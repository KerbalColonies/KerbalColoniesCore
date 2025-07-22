using KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fusion;
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

        public bool ManualControl { get; set; } = false;
        public int ManualPowerLevel { get; set; }
        public double ManualThrottle { get; set; }

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
        public double PowerLevelOutput { get; protected set; }
        public double PowerLevelThrottleTarget { get; protected set; }
        public double PowerLevelChangeTime { get; protected set; } // finish time
        public bool SetPowerLevelChangeTime { get; protected set; } = false;
        public double PowerLevelLevelOffTime { get; protected set; }
        public bool SetPowerLevelLevelOffTime { get; protected set; }

        public double ThrottleTarget { get; protected set; } = -1;
        public double ThrottleChangeTime { get; protected set; }
        public bool SetThrottleChangeTime { get; protected set; }
        public bool ResetThrottleTarget { get; protected set; } = false;
        public double OldThrottleTarget { get; protected set; }
        public double NewThrottleTarget { get; protected set; }

        public Dictionary<PartResourceDefinition, double> StoredInput { get; protected set; } = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> StoredOutput { get; protected set; } = new Dictionary<PartResourceDefinition, double>();
        public double lastECPerSecond = 0.0;
        public KeyValuePair<int, double> lastPowerLevel { get; protected set; } = new KeyValuePair<int, double>(-1, 0.0);
        public int currentPowerLevel { get; protected set; } = -1;
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

            if (ChangingPowerLevel || ChangingThrottle) return;
            else
            {
                ChangingPowerLevel = true;
                PowerLevelTarget = targetLevel;
                PowerLevelThrottleTarget = targetThrottle;
                SetPowerLevelChangeTime = false;
                return;
            }
        }

        public void ChangeThrottle(double currentTime, double targetThrottle)
        {
            Configuration.writeLog($"Changing throttle: time: {currentTime}, target: {targetThrottle}");

            if (ChangingPowerLevel || ChangingThrottle || ChangingOutput) return;

            if (Math.Round(currentThrottle, 3) == Math.Round(targetThrottle, 3))
            {
                Configuration.writeDebug("Same throttle target!");
                return;
            }
            else
            {
                OldThrottleTarget = currentThrottle;
                ThrottleTarget = targetThrottle;
                ChangingThrottle = true;
                ResetThrottleTarget = false;
            }
        }

        public double ChangingPowerTarget(double lastTime, double deltaTime, double currentTime)
        {
            Configuration.writeDebug($"KCFissionreactor: ChangingPowerTarget");
            if (!ChangingPowerLevel)
            {
                lastECPerSecond = 0;
                return 0;
            }
            Configuration.writeDebug("KCFissionreactor: ChangingPowerLevel = true");

            if (currentPowerLevel == -1)
            {
                currentPowerLevel = 0;
                PowerLevelOutput = FissionInfo.ECProduction[0];
                currentThrottle = 0;
            }

            else if (!ChangingOutput)
            {
                if (currentPowerLevel == PowerLevelTarget)
                {
                    if (currentThrottle == PowerLevelThrottleTarget)
                    {
                        // Finished the change, waiting for the time
                        if (!SetPowerLevelChangeTime)
                        {
                            Configuration.writeDebug($"KCFissionreactor: ChangingPowerTarget: Setting power level change time");
                            PowerLevelChangeTime = currentTime + FissionInfo.PowerlevelChangeTime[PowerLevelTarget];
                            SetPowerLevelChangeTime = true;
                        }

                        Configuration.writeDebug($"KCFissionreactor: ChangingPowerTarget: finished the power level change, waiting for time {PowerLevelChangeTime}, current time: {currentTime}");

                        if (currentTime > PowerLevelChangeTime)
                        {
                            Configuration.writeLog($"KCFissionreactor: ChangingPowerTarget: reached target time, finished power level change.");
                            ChangingPowerLevel = false;
                            SetPowerLevelChangeTime = false;
                        }

                        lastECPerSecond = PowerLevelOutput * PowerLevelThrottleTarget;
                        currentThrottle = PowerLevelThrottleTarget;
                        lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FissionInfo.ECProduction[PowerLevelTarget]);
                        Configuration.writeDebug($"KCFissionReactor: last ec/s: {lastECPerSecond}, throttle: {currentThrottle}, ec produced: {lastECPerSecond * deltaTime}");
                        return lastECPerSecond * deltaTime;
                    }
                    else
                    {
                        lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FissionInfo.ECProduction[PowerLevelTarget]);
                        Configuration.writeDebug($"KCFissionreactor: ChangingPowerTarget: Changing to target throttle");
                        ChangePowerTarget(PowerLevelOutput * currentThrottle, PowerLevelOutput * PowerLevelThrottleTarget, currentTime);
                    }
                }
                else if (currentPowerLevel > PowerLevelTarget)
                {
                    double minThrottle = Math.Max(FissionInfo.ECProduction[PowerLevelTarget] / FissionInfo.ECProduction[currentPowerLevel], FissionInfo.MinECThrottle[currentPowerLevel]);
                    if (currentThrottle == minThrottle)
                    {
                        if (!SetPowerLevelLevelOffTime)
                        {
                            PowerLevelLevelOffTime = currentTime + FissionInfo.LevelOffTime[currentPowerLevel];
                            SetPowerLevelLevelOffTime = true;
                        }
                        
                        if (currentTime > PowerLevelLevelOffTime)
                        {
                            Configuration.writeLog($"KCFissionreactor: ChangingPowerTarget: Reached current power level minimum throttle");
                            currentPowerLevel = PowerLevelTarget;
                            lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FissionInfo.ECProduction[PowerLevelTarget]);
                            double newOutput = FissionInfo.ECProduction[currentPowerLevel];
                            ChangePowerTarget(PowerLevelOutput * currentThrottle, newOutput * PowerLevelThrottleTarget, currentTime);
                            PowerLevelOutput = newOutput;
                            ResetPowerTarget = true;
                            SetPowerLevelLevelOffTime = false;
                        }
                        else
                        {
                            lastECPerSecond = PowerLevelOutput * currentThrottle;
                            return lastECPerSecond * deltaTime;
                        }
                    }
                    else
                    {
                        Configuration.writeLog($"KCFissionreactor: ChangingPowerTarget: throttling down to minimum");
                        ChangePowerTarget(PowerLevelOutput * currentThrottle, PowerLevelOutput * minThrottle, currentTime);
                        ResetPowerTarget = true;
                    }
                }
                else if (currentPowerLevel < PowerLevelTarget)
                {
                    Configuration.writeLog($"KCFissionreactor: ChangingPowerTarget: increasing power level");
                    currentPowerLevel = PowerLevelTarget;
                    lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FissionInfo.ECProduction[PowerLevelTarget]);
                    double newOutput = FissionInfo.ECProduction[currentPowerLevel];
                    ChangePowerTarget(PowerLevelOutput * currentThrottle, newOutput * PowerLevelThrottleTarget, currentTime);
                    PowerLevelOutput = newOutput;
                }
            }
            else if (currentPowerLevel > PowerLevelTarget && !ResetPowerTarget)
            {
                StopChangingPowerTarget(currentTime);
            }
            else if (currentPowerLevel < PowerLevelTarget)
            {

            }

            return ChangingPowerProduced(lastTime, deltaTime, currentTime);
        }

        public double ChangingThrottleProduced(double lastTime, double deltaTime, double currentTime)
        {
            if (!ChangingThrottle)
            {
                lastECPerSecond = 0;
                return 0;
            }

            if (!ChangingOutput)
            {
                if (Math.Round(currentThrottle, 3) == Math.Round(ThrottleTarget, 3))
                {
                    if (!SetThrottleChangeTime)
                    {
                        Configuration.writeDebug($"KCFissionreactor: ChangingThrottleProduced: Setting throttle change time");
                        ThrottleChangeTime = currentTime + FissionInfo.LevelOffTime[currentPowerLevel];
                        SetThrottleChangeTime = true;
                    }

                    if (currentTime > ThrottleChangeTime)
                    {
                        Configuration.writeLog($"KCFissionreactor: ChangingThrottleProduced: finished throttle change");
                        ChangingThrottle = false;
                        SetThrottleChangeTime = false;
                    }

                    currentThrottle = ThrottleTarget;
                    lastECPerSecond = lastPowerLevel.Value * currentThrottle;
                    Configuration.writeDebug($"KCFissionReactor: last ec/s: {lastECPerSecond}, throttle: {currentThrottle}, ec produced: {lastECPerSecond * deltaTime}");
                    return lastECPerSecond * deltaTime;
                }
                else
                {
                    ChangePowerTarget(lastPowerLevel.Value * currentThrottle, lastPowerLevel.Value * ThrottleTarget, currentTime);
                }
            }
            else if ((OldOutput < TargetOutput && ThrottleTarget < OldThrottleTarget || OldOutput > TargetOutput && ThrottleTarget > OldThrottleTarget) && !ResetPowerTarget)
            {
                StopChangingPowerTarget(currentTime);
                ResetThrottleTarget = true;
            }

            return ChangingPowerProduced(lastTime, deltaTime, currentTime);
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
            if (!ChangingOutput || deltaTime <= 0)
            {
                lastECPerSecond = 0;
                return 0;
            }

            double x0 = lastTime - ChangeTime - ChangeDuration / 2;
            double x1 = x0 + deltaTime;

            lastECPerSecond = OldOutput + a / (1 + Math.Pow(Math.E, -b * x1)) - (b < 0 ? a : 0);

            ChangingOutput = currentTime - ChangeTime < ChangeDuration;
            if (!ChangingOutput)
            {
                ResetPowerTarget = false;
                currentThrottle = Math.Round(TargetOutput / PowerLevelOutput, 3);
                Configuration.writeLog($"KCFissionReactor: finished power change, time: {currentTime}, throttle: {currentThrottle}, last ec/s: {lastECPerSecond}, produced: {lastECPerSecond * deltaTime}");
                return lastECPerSecond * deltaTime;
            }
            else
            {
                Configuration.writeDebug($"Changing power produced: current time: {currentTime}, lastECPerSecond: {lastECPerSecond}, last ec/s: {lastECPerSecond}, produced: {lastECPerSecond * deltaTime}");
                currentThrottle = Math.Ceiling(Math.Max(0, lastECPerSecond / PowerLevelOutput) * 1000) / 1000;
                return lastECPerSecond * deltaTime;
            }
        }

        public bool CanProduceEC(double deltaTime, int powerLevel)
        {
            if (!built || !Active || Refilling) return false;

            if (powerLevel == -1) powerLevel = 0;

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

            SortedDictionary<int, double> powerLevels = PossiblePowerLevels();
            if (!CanProduceEC(deltaTime, lastPowerLevel.Key) || powerLevels.Count == 0)
            {
                Active = false;
            }

            if (Active && !ShuttingDown)
            {
                KeyValuePair<int, double> powerLevel = new KeyValuePair<int, double>(-1, 0.0);
                double throttle = 1;

                if (!KCECManager.colonyEC.ContainsKey(Colony)) return 0;

                double ecDelta = KCECManager.colonyEC[Colony].lastECDelta / KCECManager.colonyEC[Colony].deltaTime;

                bool canStoreEC = KCECStorageFacility.ColonyECCapacity(Colony) > KCECStorageFacility.ColonyEC(Colony);

                if (ManualControl)
                {
                    powerLevel = new KeyValuePair<int, double> (ManualPowerLevel, powerLevels[ManualPowerLevel]);
                    throttle = ManualThrottle;
                }
                else
                {
                    powerLevels.ToList().ForEach(kvp =>
                    {
                        double kvpDelta = kvp.Value - lastECPerSecond + ecDelta;

                        double powerLevelDelta = Math.Round(ecDelta - lastECPerSecond + powerLevel.Value * throttle, 4);

                        // ec storages not filled
                        if (canStoreEC && kvp.Value > powerLevel.Value)
                        {
                            powerLevel = kvp;
                            throttle = 1;
                        }
                        // ec Delta < 0
                        else if (powerLevelDelta < 0 || powerLevel.Key == -1)
                        {
                            // minimum throttle for the current kvp
                            if (kvpDelta > 0)
                            {
                                double minThrottle = Math.Max(fissionInfo.MinECThrottle[kvp.Key], Math.Ceiling(Math.Min(1, 1 - kvpDelta / kvp.Value) * 1000) / 1000.0);
                                // 1.23456 -> 1234.56 -> 1235 -> 1.235

                                powerLevel = kvp;
                                throttle = minThrottle;
                            }
                            else if (kvp.Value > powerLevel.Value)
                            {
                                powerLevel = kvp;
                                throttle = 1;
                            }
                        }
                    });
                }


                if (powerLevel.Key == -1)
                {
                    lastECPerSecond = 0.0;
                    return 0.0;
                }

                Configuration.writeDebug($"Fissionreactor: power: {powerLevel.Value}, throttle: {throttle}");

                if (powerLevel.Key != lastPowerLevel.Key)
                {
                    ChangePowerLevel(currentTime, powerLevel.Key, powerLevel.Value, throttle);
                }
                else if (throttle != ThrottleTarget)
                {
                    ChangeThrottle(currentTime, throttle);
                }

                if (throttle == -1) throttle = 1;

                ConsumeResources(deltaTime);

                if (ChangingPowerLevel)
                {
                    return ChangingPowerTarget(lastTime, deltaTime, currentTime);
                }
                else if (ChangingThrottle)
                {
                    return ChangingThrottleProduced(lastTime, deltaTime, currentTime);
                }
                else
                {
                    currentThrottle = throttle;
                    lastECPerSecond = lastPowerLevel.Value * currentThrottle;
                    Configuration.writeDebug($"KCFissionReactor: last ec/s: {lastECPerSecond}, throttle: {currentThrottle}, ec produced: {TargetOutput * deltaTime}");
                    return lastECPerSecond * deltaTime;
                }
            }
            else
            {

                if (ChangingPowerLevel)
                {
                    ConsumeResources(deltaTime);
                    return ChangingPowerTarget(lastTime, deltaTime, currentTime);
                }
                else if (ChangingThrottle)
                {
                    ConsumeResources(deltaTime);
                    return ChangingThrottleProduced(lastTime, deltaTime, currentTime);
                }
                else if (lastECPerSecond > fissionInfo.ECChangeThreshold[level])
                {
                    if (!ChangingOutput)
                    {
                        ChangePowerTarget(lastECPerSecond, 0, currentTime);
                        ShuttingDown = true;
                    }
                }
                else
                {
                    lastECPerSecond = 0;
                    currentThrottle = 0;

                    if (currentTime - ChangeTime > ChangeDuration + fissionInfo.LevelOffTime[level])
                    {
                        ShuttingDown = false;
                        ChangingOutput = false;
                        lastPowerLevel = new KeyValuePair<int, double>(-1, 0.0);
                        Configuration.writeLog($"Finishing shutdown of fission reactor {name} at time {currentTime}");
                    }

                    return 0;
                }

                if (lastECPerSecond <= 0) return 0;

                ConsumeResources(deltaTime);
                return ChangingPowerProduced(lastTime, deltaTime, currentTime);
            }
        }

        public void ConsumeResources(double deltaTime)
        {
            if (lastPowerLevel.Key == -1) return;

            FissionInfo.InputResources[lastPowerLevel.Key].ToList().ForEach(item =>
            {
                StoredInput[item.Key] = Math.Max(0, StoredInput[item.Key] - item.Value * currentThrottle * deltaTime);
            });
            FissionInfo.OutputResources[lastPowerLevel.Key].ToList().ForEach(item =>
            {
                StoredOutput[item.Key] = Math.Min(FissionInfo.OutputStorage[level][item.Key], StoredOutput[item.Key] + item.Value * currentThrottle * deltaTime);
            });
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

        public override string GetFacilityProductionDisplay() => $"Fission reactor production rate: {ECPerSecond()} EC/s";


        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            ConfigNode fissionNode = new ConfigNode("fissionNode");
            fissionNode.AddValue("Active", Active);
            fissionNode.AddValue("Refilling", Refilling);
            fissionNode.AddValue("RefillTime", RefillTime);
            fissionNode.AddValue("ManualControl", ManualControl);
            fissionNode.AddValue("ManualPowerLevel", ManualPowerLevel);
            fissionNode.AddValue("ManualThrottle", ManualThrottle);
            fissionNode.AddValue("ChangingOutput", ChangingOutput);
            fissionNode.AddValue("ChangingPowerLevel", ChangingPowerLevel);
            fissionNode.AddValue("ChangingThrottle", ChangingThrottle);
            fissionNode.AddValue("OldOutput", OldOutput);
            fissionNode.AddValue("TargetOutput", TargetOutput);
            fissionNode.AddValue("LastResetOutput", LastResetOutput);
            fissionNode.AddValue("b", b);
            fissionNode.AddValue("ChangeTime", ChangeTime);
            fissionNode.AddValue("ChangeDuration", ChangeDuration);
            fissionNode.AddValue("ResetPowerTarget", ResetPowerTarget);
            fissionNode.AddValue("ShuttingDown", ShuttingDown);
            fissionNode.AddValue("PowerLevelTarget", PowerLevelTarget);
            fissionNode.AddValue("PowerLevelOutput", PowerLevelOutput);
            fissionNode.AddValue("PowerLevelThrottleTarget", PowerLevelThrottleTarget);
            fissionNode.AddValue("PowerLevelChangeTime", PowerLevelChangeTime);
            fissionNode.AddValue("SetPowerLevelChangeTime", SetPowerLevelChangeTime);
            fissionNode.AddValue("PowerLevelLevelOffTime", PowerLevelLevelOffTime);
            fissionNode.AddValue("SetPowerLevelLevelOffTime", SetPowerLevelLevelOffTime);
            fissionNode.AddValue("ThrottleTarget", ThrottleTarget);
            fissionNode.AddValue("ThrottleChangeTime", ThrottleChangeTime);
            fissionNode.AddValue("SetThrottleChangeTime", SetThrottleChangeTime);
            fissionNode.AddValue("ResetThrottleTarget", ResetThrottleTarget);
            fissionNode.AddValue("OldThrottleTarget", OldThrottleTarget);
            fissionNode.AddValue("NewThrottleTarget", NewThrottleTarget);
            fissionNode.AddValue("lastECPerSecond", lastECPerSecond);
            fissionNode.AddValue("lastPowerLevel", lastPowerLevel.Key);
            fissionNode.AddValue("currentPowerLevel", currentPowerLevel);
            fissionNode.AddValue("currentThrottle", currentThrottle);

            ConfigNode storedInputNode = new ConfigNode("storedInputNode");
            StoredInput.ToList().ForEach(kvp => storedInputNode.AddValue(kvp.Key.name, kvp.Value));
            fissionNode.AddNode(storedInputNode);

            ConfigNode storedOutputNode = new ConfigNode("storedOutputNode");
            StoredOutput.ToList().ForEach(kvp => storedOutputNode.AddValue(kvp.Key.name, kvp.Value));
            fissionNode.AddNode(storedOutputNode);

            node.AddNode(fissionNode);
            return node;
        }

        public KCFissionReactor(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            window = new KCFissionWindow(this);

            ConfigNode fissionNode = node.GetNode("fissionNode");

            Active = bool.Parse(fissionNode.GetValue("Active"));
            Refilling = bool.Parse(fissionNode.GetValue("Refilling"));
            RefillTime = double.Parse(fissionNode.GetValue("RefillTime"));
            ManualControl = bool.Parse(fissionNode.GetValue("ManualControl"));
            ManualPowerLevel = int.Parse(fissionNode.GetValue("ManualPowerLevel"));
            ManualThrottle = double.Parse(fissionNode.GetValue("ManualThrottle"));
            ChangingOutput = bool.Parse(fissionNode.GetValue("ChangingOutput"));
            ChangingPowerLevel = bool.Parse(fissionNode.GetValue("ChangingPowerLevel"));
            ChangingThrottle = bool.Parse(fissionNode.GetValue("ChangingThrottle"));
            OldOutput = double.Parse(fissionNode.GetValue("OldOutput"));
            TargetOutput = double.Parse(fissionNode.GetValue("TargetOutput"));
            LastResetOutput = double.Parse(fissionNode.GetValue("LastResetOutput"));
            b = double.Parse(fissionNode.GetValue("b"));
            ChangeTime = double.Parse(fissionNode.GetValue("ChangeTime"));
            ChangeDuration = double.Parse(fissionNode.GetValue("ChangeDuration"));
            ResetPowerTarget = bool.Parse(fissionNode.GetValue("ResetPowerTarget"));
            ShuttingDown = bool.Parse(fissionNode.GetValue("ShuttingDown"));
            PowerLevelTarget = int.Parse(fissionNode.GetValue("PowerLevelTarget"));
            PowerLevelOutput = double.Parse(fissionNode.GetValue("PowerLevelOutput"));
            PowerLevelThrottleTarget = double.Parse(fissionNode.GetValue("PowerLevelThrottleTarget"));
            PowerLevelChangeTime = double.Parse(fissionNode.GetValue("PowerLevelChangeTime"));
            SetPowerLevelChangeTime = bool.Parse(fissionNode.GetValue("SetPowerLevelChangeTime"));
            PowerLevelLevelOffTime = double.Parse(fissionNode.GetValue("PowerLevelLevelOffTime"));
            SetPowerLevelLevelOffTime = bool.Parse(fissionNode.GetValue("SetPowerLevelLevelOffTime"));
            ThrottleTarget = double.Parse(fissionNode.GetValue("ThrottleTarget"));
            ThrottleChangeTime = double.Parse(fissionNode.GetValue("ThrottleChangeTime"));
            SetThrottleChangeTime = bool.Parse(fissionNode.GetValue("SetThrottleChangeTime"));
            ResetThrottleTarget = bool.Parse(fissionNode.GetValue("ResetThrottleTarget"));
            OldThrottleTarget = double.Parse(fissionNode.GetValue("OldThrottleTarget"));
            NewThrottleTarget = double.Parse(fissionNode.GetValue("NewThrottleTarget"));
            lastECPerSecond = double.Parse(fissionNode.GetValue("lastECPerSecond"));
            int lastPowerLevelKey = int.Parse(fissionNode.GetValue("lastPowerLevel"));
            lastPowerLevel = new KeyValuePair<int, double>(lastPowerLevelKey, lastPowerLevelKey != -1 ? FissionInfo.ECProduction[lastPowerLevelKey] : 0);
            currentPowerLevel = int.Parse(fissionNode.GetValue("currentPowerLevel"));
            currentThrottle = double.Parse(fissionNode.GetValue("currentThrottle"));

            ConfigNode storedInputNode = fissionNode.GetNode("storedInputNode");
            foreach (ConfigNode.Value v in storedInputNode.values)
            {
                PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                double amount = double.Parse(v.value);
                StoredInput.Add(resourceDef, amount);
            }

            ConfigNode storedOutputNode = fissionNode.GetNode("storedOutputNode");
            foreach (ConfigNode.Value v in storedOutputNode.values)
            {
                PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                double amount = double.Parse(v.value);
                StoredOutput.Add(resourceDef, amount);
            }
        }

        public KCFissionReactor(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCFissionWindow(this);
            FissionInfo.InputStorage[level].ToList().ForEach(kvp => StoredInput.Add(kvp.Key, 0));
            FissionInfo.OutputStorage[level].ToList().ForEach(kvp => StoredOutput.Add(kvp.Key, 0));

            lastPowerLevel = new KeyValuePair<int, double>(-1, 0.0);
            currentPowerLevel = -1;
        }
    }
}
