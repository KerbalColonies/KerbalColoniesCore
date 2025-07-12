using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fission
{
    public class KCFissionReactor : KCKerbalFacilityBase, KCECProducer
    {
        public KCFissionInfo FissionInfo => (KCFissionInfo)facilityInfo;

        public bool Active { get; set; } = false;
        public bool Refilling { get; protected set; } = false;
        public double RefillTime = 0.0;

        public bool ChangingOutput { get; protected set; } = false;
        public bool ChangingPowerLevel { get; protected set; } = false;
        public bool ChangingThrottle { get; protected set; } = false;
        public double OldOutput { get; protected set; } = 0.0;
        public double TargetOutput { get; protected set; } = 0.0;
        public double LastResetOutput { get; protected set; } = 0.0;
        public double a => Math.Abs(TargetOutput - OldOutput); // Output delta
        public double b { get; protected set; }
        public double ChangeTime { get; protected set; } = 0.0; // the start time of the change
        public double ChangeDuration { get; protected set; } = 0.0;
        public bool ResetPowerTarget { get; protected set; } = false;
        public bool ShuttingDown { get; protected set; } = false;
        public int PowerLevelTarget { get; protected set; } = -1;
        public double ThrottleTarget { get; protected set; } = -1;

        public Dictionary<PartResourceDefinition, double> StoredInput { get; protected set; } = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> StoredOutput { get; protected set; } = new Dictionary<PartResourceDefinition, double>();
        public double lastECPerSecond = 0.0;
        public KeyValuePair<int, double> lastPowerLevel { get; protected set; } = new KeyValuePair<int, double>(-1, 0.0);
        public KeyValuePair<int, double> lastThrottle { get; protected set; } = new KeyValuePair<int, double>(-1, 0.0);
        public double currentPowerLevel { get; protected set; } = -1;
        public double currentThrottle { get; protected set; } = -1;

        public KCFissionWindow window { get; protected set; } = null;

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals) => base.filterKerbals(kerbals).Where(k => k.experienceLevel >= FissionInfo.MinKerbalLevel[level]).ToList();

        public SortedDictionary<int, double> AvailablePowerLevels() => new SortedDictionary<int, double>(FissionInfo.ECProduction.Where(kvp => kvp.Key <= level).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        public SortedDictionary<int, double> PossiblePowerLevels() => new SortedDictionary<int, double>(AvailablePowerLevels().Where(kvp => FissionInfo.MinKerbals[kvp.Key] <= kerbals.Count).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        public void Refill()
        {
            if (Active) return;

            Refilling = true;
            RefillTime = 0.0;
        }


        public void ChangePowerLevel(double currentTime, int targetLevel, double levelOutput, double targetThrottle)
        {
            Configuration.writeLog($"Changing power level: time: {currentTime}, target: {targetLevel}, levelOutput: {levelOutput}, throttle: {targetThrottle}");

            if (ChangingOutput) return;

            if (ChangingPowerLevel)
            {
                if (targetLevel != PowerLevelTarget) return;

                ChangeThrottle(currentTime, lastThrottle.Key, targetThrottle);
                if (!ChangingThrottle && currentTime - ChangeTime > ChangeDuration / 2 + FissionInfo.PowerlevelChangeTime[level])
                {
                    ChangingPowerLevel = false;
                    lastPowerLevel = new KeyValuePair<int, double>(targetLevel, levelOutput);
                    lastThrottle = new KeyValuePair<int, double>(targetLevel, targetThrottle);
                }
            }
            else if (lastThrottle.Key == -1)
            {
                lastPowerLevel = new KeyValuePair<int, double>(targetLevel, levelOutput);
                lastThrottle = new KeyValuePair<int, double>(targetLevel, targetThrottle);
                ChangePowerTarget(0, levelOutput * targetThrottle, currentTime);
            }
            else
            {
                if (targetLevel > lastPowerLevel.Key)
                {
                    if (currentThrottle < 1) ChangeThrottle(currentTime, lastThrottle.Key, 1);
                    else
                    {
                        lastPowerLevel = new KeyValuePair<int, double>(targetLevel, levelOutput);
                        lastThrottle = new KeyValuePair<int, double>(targetLevel, targetThrottle);
                        ChangeThrottle(currentTime, targetLevel, targetThrottle);
                    }
                }
                else
                {
                    if (currentThrottle > FissionInfo.MinECThrottle[lastThrottle.Key])
                    {
                        double target = Math.Max(FissionInfo.MinECThrottle[lastThrottle.Key], levelOutput / lastPowerLevel.Value);
                        ChangeThrottle(currentTime, targetLevel, target);
                        ChangingPowerLevel = true;

                    }
                    else
                    {
                        lastPowerLevel = new KeyValuePair<int, double>(targetLevel, levelOutput);
                        lastThrottle = new KeyValuePair<int, double>(targetLevel, targetThrottle);
                        ChangeThrottle(currentTime, targetLevel, targetThrottle);
                    }
                }
            }
            PowerLevelTarget = targetLevel;
            ThrottleTarget = targetThrottle;
        }

        public void ChangeThrottle(double currentTime, int throttleKey, double targetThrottle)
        {
            Configuration.writeLog($"Changing throttle: time: {currentTime}, key: {throttleKey}, target: {targetThrottle}");

            if (Math.Round(currentThrottle, 3) == Math.Round(targetThrottle, 3))
            {
                Configuration.writeDebug("Same throttle target!");
                ChangingThrottle = false;
                //currentThrottle = Math.Round(currentThrottle, 3);
                //lastThrottle = new KeyValuePair<int, double>(throttleKey, targetThrottle);
                return;
            }

            if (ChangingOutput && !ChangingThrottle)
            {
                if (TargetOutput > OldOutput && targetThrottle > lastThrottle.Value)
                {
                    if (currentTime - ChangeTime > ChangeDuration / 2)
                    {
                        TargetOutput = lastPowerLevel.Value * targetThrottle;
                        b = 4 * FissionInfo.MaxECChangeRate[level] / a * Math.Sign(TargetOutput - OldOutput);
                        ChangeDuration = Math.Abs((-1 / b) * Math.Log((a / (a - FissionInfo.ECChangeThreshold[level])) - 1)) * 2;

                        Configuration.writeLog($"Changing throttle target for {name} from {OldOutput} to {TargetOutput} (a: {a}, b: {b}, duration: {ChangeDuration})");
                        Configuration.writeDebug($"Starttime: {currentTime}");

                        ChangingThrottle = true;
                        lastThrottle = new KeyValuePair<int, double>(throttleKey, targetThrottle);
                    }
                }
                else if (!ResetPowerTarget)
                {
                    StopChangingPowerTarget(currentTime);
                }
            }
            else if (!ChangingThrottle)
            {
                if (currentTime - ChangeTime > ChangeDuration + FissionInfo.LevelOffTime[level] / 2)
                {
                    Configuration.writeDebug("Throttle isn't changed, starting change");
                    ChangePowerTarget(lastECPerSecond, lastPowerLevel.Value * targetThrottle, currentTime);
                    lastThrottle = new KeyValuePair<int, double>(throttleKey, targetThrottle);
                    ChangingThrottle = true;
                }
            }
            else
            {
                Configuration.writeDebug("Finished throttle change");
                ChangingThrottle = false;
            }
        }

        public void ChangePowerTarget(double oldOutput, double targetOutput, double currentTime)
        {
            if (Math.Round(oldOutput, 3) == Math.Round(targetOutput, 3) || ChangingOutput) return;

            OldOutput = oldOutput;
            TargetOutput = targetOutput;

            b = 4 * FissionInfo.MaxECChangeRate[level] / a * Math.Sign(targetOutput - oldOutput);

            ChangeDuration = Math.Abs((-1 / b) * Math.Log((a / (a - FissionInfo.ECChangeThreshold[level])) - 1)) * 2;

            while (ChangeDuration < FissionInfo.MinECRateChangeTime[level])
            {
                b *= 0.99;
                ChangeDuration = Math.Abs((-1 / b) * Math.Log((a / (a - FissionInfo.ECChangeThreshold[level])) - 1)) * 2;
            }

            ChangeTime = currentTime;
            ChangingOutput = true;

            Configuration.writeLog($"Changing power target for {name} from {oldOutput} to {targetOutput} (a: {a}, b: {b}, duration: {ChangeDuration})");
            Configuration.writeDebug($"Starttime: {currentTime}");
        }

        public void StopChangingPowerTarget(double currentTime)
        {
            if (!ChangingOutput || ResetPowerTarget) return;

            if (currentTime - ChangeTime > ChangeDuration / 2) return;

            double x = currentTime - ChangeTime - ChangeDuration / 2;
            LastResetOutput = TargetOutput;

            b = 4 * FissionInfo.MaxECChangeRate[level] / a * Math.Sign(TargetOutput - OldOutput);

            if (TargetOutput > OldOutput)
                TargetOutput = OldOutput + a / (1 + Math.Pow(Math.E, -b * x)) * 2;
            else
                TargetOutput = OldOutput - (a - a / (1 + Math.Pow(Math.E, -b * x))) * 2;

            ChangeDuration = (currentTime - ChangeTime) * 2;


            ChangingOutput = true;
            ResetPowerTarget = true;

            Configuration.writeLog($"Reseting power target for {name} from {OldOutput} to {TargetOutput} (a: {a}, b: {b}, duration: {ChangeDuration})");
            Configuration.writeDebug($"Starttime: {currentTime}");
        }

        protected double ChangingPowerProduced(double lastTime, double deltaTime, double currentTime)
        {
            if (!ChangingOutput || deltaTime <= 0) return 0.0;

            double x0 = lastTime - ChangeTime - ChangeDuration / 2;
            double x1 = x0 + deltaTime;

            lastECPerSecond = OldOutput + a / (1 + Math.Pow(Math.E, -b * x1)) - (b < 0 ? a : 0);

            ChangingOutput = currentTime - ChangeTime < ChangeDuration;
            ResetPowerTarget = ResetPowerTarget && ChangingOutput;

            Configuration.writeLog($"Changing power produced: current time: {currentTime}, lastECPerSecond: {lastECPerSecond}");

            currentThrottle = Math.Max(0, lastECPerSecond / TargetOutput);

            return lastECPerSecond * deltaTime;
        }

        public bool CanProduceEC(double deltaTime, int powerLevel)
        {
            if (!built || !Active || Refilling) return false;

            foreach (KeyValuePair<PartResourceDefinition, double> item in FissionInfo.InputResources[powerLevel])
            {
                if (!StoredInput.ContainsKey(item.Key) || StoredInput[item.Key] < item.Value * deltaTime)
                {
                    Configuration.writeDebug($"KCFissionReactor ({name}): Not enough {item.Key.name} to produce EC");
                    return false;
                }
            }
            foreach (KeyValuePair<PartResourceDefinition, double> item in FissionInfo.OutputResources[powerLevel])
            {
                if (!StoredOutput.ContainsKey(item.Key) || StoredOutput[item.Key] + item.Value * deltaTime > FissionInfo.OutputStorage[level][item.Key])
                {
                    Configuration.writeDebug($"KCFissionReactor ({name}): Unable to store {item.Key.name}");
                    return false;
                }
            }

            return true;
        }

        public double ProduceEC(double lastTime, double deltaTime, double currentTime)
        {
            KCFissionInfo fissionInfo = FissionInfo;

            if (Refilling && lastECPerSecond <= 0)
            {
                Active = false;

                RefillTime += deltaTime * kerbals.Count;
                if (RefillTime >= FissionInfo.RefillTime[level])
                {
                    Refilling = false;
                    StoredInput.ToList().ForEach(kvp =>
                    {
                        double requestedAmount = fissionInfo.InputStorage[level][kvp.Key] - kvp.Value;
                        double missingAmount = KCStorageFacility.addResourceToColony(kvp.Key, -requestedAmount, Colony);
                        StoredInput[kvp.Key] += requestedAmount + missingAmount;
                    });
                    StoredOutput.ToList().ForEach(kvp =>
                    {
                        double requestedAmount = kvp.Value;
                        double missingAmount = KCStorageFacility.addResourceToColony(kvp.Key, requestedAmount, Colony);
                        StoredOutput[kvp.Key] = missingAmount;
                    });
                }
                else
                {
                    lastECPerSecond = 0.0;
                    return 0.0;
                }
            }

            if (Active && !ShuttingDown)
            {
                SortedDictionary<int, double> powerLevels = PossiblePowerLevels();
                KeyValuePair<int, double> powerLevel = new KeyValuePair<int, double>(-1, 0.0);
                KeyValuePair<int, double> throttle = new KeyValuePair<int, double>(-1, 0.0);
                double ecDelta = KCECManager.colonyEC[Colony].lastECDelta / KCECManager.colonyEC[Colony].deltaTime;

                bool canStoreEC = KCECStorageFacility.ColonyECCapacity(Colony) > KCECStorageFacility.ColonyEC(Colony);

                powerLevels.ToList().ForEach(kvp =>
                {
                    double kvpDelta = kvp.Value - lastECPerSecond + ecDelta;

                    double powerLevelDelta = Math.Round(ecDelta - lastECPerSecond + powerLevel.Value * throttle.Value, 4);

                    // ec storages not filled
                    if (canStoreEC && kvp.Value > powerLevel.Value)
                    {
                        powerLevel = kvp;
                        throttle = new KeyValuePair<int, double>(kvp.Key, 1);
                    }
                    // ec Delta < 0
                    else if (powerLevelDelta < 0 || powerLevel.Key == -1)
                    {
                        // minimum throttle for the current kvp
                        if (kvpDelta > 0)
                        {
                            double minThrottle = Math.Max(fissionInfo.MinECThrottle[kvp.Key], Math.Min(1, 1 - kvpDelta / kvp.Value));

                            powerLevel = kvp;
                            throttle = new KeyValuePair<int, double>(kvp.Key, minThrottle);
                        }
                        else if (kvp.Value > powerLevel.Value)
                        {
                            powerLevel = kvp;
                            throttle = new KeyValuePair<int, double>(kvp.Key, 1);
                        }
                    }
                });

                if (powerLevel.Key == -1 || throttle.Key == -1 || !CanProduceEC(deltaTime, powerLevel.Key))
                {
                    lastECPerSecond = 0.0;
                    return 0.0;
                }

                Configuration.writeDebug($"Fissionreactor: power: {powerLevel.Value}, throttle: {throttle.Value}");

                if (powerLevel.Key != lastPowerLevel.Key || ChangingPowerLevel)
                {
                    //ChangePowerTarget(lastECPerSecond, powerLevel.Value, currentTime); // hard change in output is unwanted

                    ChangePowerLevel(currentTime, powerLevel.Key, powerLevel.Value, throttle.Value);

                    /*
                    if (ChangingOutput && !ResetPowerTarget)
                    {
                        StopChangingPowerTarget(currentTime);
                    }
                    else if (currentTime - ChangeTime > ChangeDuration + fissionInfo.LevelOffTime[level])
                    {
                        ChangePowerTarget(lastECPerSecond, powerLevel.Value, currentTime);
                        lastPowerLevel = powerLevel;
                    }*/
                }
                else if (throttle.Value != lastThrottle.Value || ChangingThrottle)
                {
                    ChangeThrottle(currentTime, throttle.Key, throttle.Value);
                }

                FissionInfo.InputResources[lastPowerLevel.Key].ToList().ForEach(item =>
                {
                    StoredInput[item.Key] -= item.Value * deltaTime;
                });
                FissionInfo.OutputResources[lastPowerLevel.Key].ToList().ForEach(item =>
                {
                    StoredOutput[item.Key] += item.Value * deltaTime;
                });

                if (ChangingOutput)
                {
                    return ChangingPowerProduced(lastTime, deltaTime, currentTime);
                }
                else if (currentTime - ChangeTime <= ChangeDuration + fissionInfo.LevelOffTime[level])
                {
                    currentThrottle = lastThrottle.Value;
                    lastECPerSecond = TargetOutput;
                    return TargetOutput * deltaTime;
                }
                else
                {
                    currentThrottle = lastThrottle.Value;
                    lastECPerSecond = lastPowerLevel.Value * lastThrottle.Value;
                    return lastECPerSecond * deltaTime;
                }
            }
            else
            {
                if (lastECPerSecond <= 0) return 0;

                if (ChangingOutput)
                {
                    if (!ShuttingDown)
                    {
                        if (TargetOutput < OldOutput)
                        {
                            TargetOutput = 0;
                            b = -4 * FissionInfo.MaxECChangeRate[level] / a;
                            ChangeDuration = Math.Abs((-1 / b) * Math.Log((a / (a - FissionInfo.ECChangeThreshold[level])) - 1)) * 2;

                            Configuration.writeLog($"Disabling fission reactor {name}, oldoutput: {OldOutput}, b: {b}, duration: {ChangeDuration}");
                            ShuttingDown = true;
                        }
                        else if (!ResetPowerTarget)
                        {
                            StopChangingPowerTarget(currentTime);
                        }
                    }
                }
                else if (lastECPerSecond > fissionInfo.ECChangeThreshold[level])
                {
                    if (currentTime - ChangeTime > ChangeDuration + fissionInfo.LevelOffTime[level])
                    {
                        ChangePowerTarget(lastECPerSecond, 0, currentTime);
                        ShuttingDown = true;
                    }
                }
                else
                {
                    if (currentTime - ChangeTime > ChangeDuration + fissionInfo.LevelOffTime[level])
                    {
                        ShuttingDown = false;
                        lastECPerSecond = 0;
                        ChangingOutput = false;
                        lastPowerLevel = new KeyValuePair<int, double>(-1, 0.0);
                        Configuration.writeLog($"Finishing shutdown of fission reactor {name} at time {currentTime}");
                    }

                    return 0;
                }

                return ChangingPowerProduced(lastTime, deltaTime, currentTime);
            }
        }

        public double ECProduction(double lastTime, double deltaTime, double currentTime) => ProduceEC(lastTime, deltaTime, currentTime);

        public double ECPerSecond() => lastECPerSecond;

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }
        public override void OnRemoteClicked()
        {
            window.Toggle();
        }

        public KCFissionReactor(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            window = new KCFissionWindow(this);

            StoredInput = new Dictionary<PartResourceDefinition, double>();
            FissionInfo.InputStorage[level].ToList().ForEach(kvp => StoredInput.Add(kvp.Key, 0));
            FissionInfo.OutputStorage[level].ToList().ForEach(kvp => StoredOutput.Add(kvp.Key, 0));
        }

        public KCFissionReactor(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCFissionWindow(this);
            FissionInfo.InputStorage[level].ToList().ForEach(kvp => StoredInput.Add(kvp.Key, 0));
            FissionInfo.OutputStorage[level].ToList().ForEach(kvp => StoredOutput.Add(kvp.Key, 0));
        }
    }
}
