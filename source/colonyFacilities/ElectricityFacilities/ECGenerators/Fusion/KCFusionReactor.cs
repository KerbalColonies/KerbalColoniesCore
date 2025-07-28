using KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage;
using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fusion
{
    public class KCFusionReactor : KCKerbalFacilityBase, KCECProducer, KCECConsumer
    {
        public KCFusionInfo FusionInfo => (KCFusionInfo)facilityInfo;

        public bool Active { get; set; } = false;

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

        public double lastECPerSecond = 0.0;
        public double ActualLastECPerSecond { get; protected set; } = 0.0; // the last EC per second produced, used for the GUI. Also includes the base load, unlike the lastECPerSecond
        public KeyValuePair<int, double> lastPowerLevel { get; protected set; } = new KeyValuePair<int, double>(-1, 0.0);
        public int currentPowerLevel { get; protected set; } = -1;
        public double currentThrottle { get; protected set; } = -1;

        public bool OutOfEC { get; protected set; } = false; // if the reactor is out of EC, it will not produce any power

        public KCFusionWindow window { get; protected set; } = null;

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals) => base.filterKerbals(kerbals).Where(k => k.experienceLevel >= FusionInfo.MinKerbalLevel[level]).ToList();

        public SortedDictionary<int, double> AvailablePowerLevels() => new SortedDictionary<int, double>(FusionInfo.ECProduction.Where(kvp => kvp.Key <= level).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        public SortedDictionary<int, double> PossiblePowerLevels() => new SortedDictionary<int, double>(AvailablePowerLevels().Where(kvp =>
        {
            if (FusionInfo.MinKerbals[kvp.Key] > kerbals.Count) return false;
            foreach (KeyValuePair<string, int> traitKVP in FusionInfo.RequiredTraits[kvp.Key])
            {
                if (kerbals.Count(k => k.Key.trait.ToLower() == traitKVP.Key.ToLower()) < traitKVP.Value) return false;
            }
            return true;
        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

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
            Configuration.writeDebug($"KCFusionReactor: ChangingPowerTarget");
            if (!ChangingPowerLevel)
            {
                lastECPerSecond = 0;
                return 0;
            }
            Configuration.writeDebug("KCFusionReactor: ChangingPowerLevel = true");

            if (currentPowerLevel == -1)
            {
                currentPowerLevel = 0;
                PowerLevelOutput = FusionInfo.ECProduction[0];
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
                            Configuration.writeDebug($"KCFusionReactor: ChangingPowerTarget: Setting power level change time");
                            PowerLevelChangeTime = currentTime + FusionInfo.PowerlevelChangeTime[PowerLevelTarget];
                            SetPowerLevelChangeTime = true;
                        }

                        Configuration.writeDebug($"KCFusionReactor: ChangingPowerTarget: finished the power level change, waiting for time {PowerLevelChangeTime}, current time: {currentTime}");

                        if (currentTime > PowerLevelChangeTime)
                        {
                            Configuration.writeLog($"KCFusionReactor: ChangingPowerTarget: reached target time, finished power level change.");
                            ChangingPowerLevel = false;
                            SetPowerLevelChangeTime = false;
                        }

                        lastECPerSecond = PowerLevelOutput * PowerLevelThrottleTarget;
                        currentThrottle = PowerLevelThrottleTarget;
                        lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FusionInfo.ECProduction[PowerLevelTarget]);
                        Configuration.writeDebug($"KCFusionReactor: last ec/s: {lastECPerSecond}, throttle: {currentThrottle}, ec produced: {lastECPerSecond * deltaTime}");
                        return lastECPerSecond * deltaTime;
                    }
                    else
                    {
                        lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FusionInfo.ECProduction[PowerLevelTarget]);
                        Configuration.writeDebug($"KCFusionReactor: ChangingPowerTarget: Changing to target throttle");
                        ChangePowerTarget(PowerLevelOutput * currentThrottle, PowerLevelOutput * PowerLevelThrottleTarget, currentTime);
                    }
                }
                else if (currentPowerLevel > PowerLevelTarget)
                {

                    double minThrottle = Math.Max(FusionInfo.ECProduction[PowerLevelTarget] / FusionInfo.ECProduction[currentPowerLevel], FusionInfo.MinECThrottle[currentPowerLevel]);
                    if (currentThrottle == minThrottle)
                    {
                        if (!SetPowerLevelLevelOffTime)
                        {
                            PowerLevelLevelOffTime = currentTime + FusionInfo.LevelOffTime[currentPowerLevel];
                            SetPowerLevelLevelOffTime = true;
                        }

                        if (currentTime > PowerLevelLevelOffTime)
                        {
                            Configuration.writeLog($"KCFusionReactor: ChangingPowerTarget: Reached current power level minimum throttle");
                            currentPowerLevel = PowerLevelTarget;
                            lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FusionInfo.ECProduction[PowerLevelTarget]);
                            double newOutput = FusionInfo.ECProduction[currentPowerLevel];
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
                        Configuration.writeLog($"KCFusionReactor: ChangingPowerTarget: throttling down to minimum");
                        ChangePowerTarget(PowerLevelOutput * currentThrottle, PowerLevelOutput * minThrottle, currentTime);
                        ResetPowerTarget = true;
                    }
                }
                else if (currentPowerLevel < PowerLevelTarget)
                {
                    Configuration.writeLog($"KCFusionReactor: ChangingPowerTarget: increasing power level");
                    currentPowerLevel = PowerLevelTarget;
                    lastPowerLevel = new KeyValuePair<int, double>(PowerLevelTarget, FusionInfo.ECProduction[PowerLevelTarget]);
                    double newOutput = FusionInfo.ECProduction[currentPowerLevel];
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
                        Configuration.writeDebug($"KCFusionReactor: ChangingThrottleProduced: Setting throttle change time");
                        ThrottleChangeTime = currentTime + FusionInfo.LevelOffTime[currentPowerLevel];
                        SetThrottleChangeTime = true;
                    }

                    if (currentTime > ThrottleChangeTime)
                    {
                        Configuration.writeLog($"KCFusionReactor: ChangingThrottleProduced: finished throttle change");
                        ChangingThrottle = false;
                        SetThrottleChangeTime = false;
                    }

                    currentThrottle = ThrottleTarget;
                    lastECPerSecond = lastPowerLevel.Value * currentThrottle;
                    Configuration.writeDebug($"KCFusionReactor: last ec/s: {lastECPerSecond}, throttle: {currentThrottle}, ec produced: {lastECPerSecond * deltaTime}");
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

            b = 4 * FusionInfo.MaxECChangeRate[level] / a * Math.Sign(targetOutput - oldOutput);

            ChangeDuration = Math.Abs((-1 / b) * Math.Log((a / (a - FusionInfo.ECChangeThreshold[level])) - 1)) * 2;

            while (ChangeDuration < FusionInfo.MinECRateChangeTime[level])
            {
                b *= 0.99;
                ChangeDuration = Math.Abs((-1 / b) * Math.Log((a / (a - FusionInfo.ECChangeThreshold[level])) - 1)) * 2;
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

            b = 4 * FusionInfo.MaxECChangeRate[level] / a * Math.Sign(TargetOutput - OldOutput);

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
                Configuration.writeLog($"KCFusionReactor: finished power change, time: {currentTime}, throttle: {currentThrottle}, last ec/s: {lastECPerSecond}, produced: {lastECPerSecond * deltaTime}");
                return lastECPerSecond * deltaTime;
            }
            else
            {
                Configuration.writeDebug($"KCFusionReactor: Changing power produced: current time: {currentTime}, lastECPerSecond: {lastECPerSecond}, last ec/s: {lastECPerSecond}, produced: {lastECPerSecond * deltaTime}");
                currentThrottle = Math.Ceiling(Math.Max(0, lastECPerSecond / PowerLevelOutput) * 1000) / 1000;
                return lastECPerSecond * deltaTime;
            }
        }

        public bool CanProduceEC(double deltaTime, int powerLevel)
        {
            if (!built || !Active) return false;

            if (powerLevel == -1) powerLevel = 0;

            foreach (KeyValuePair<PartResourceDefinition, double> item in FusionInfo.InputResources[powerLevel])
            {
                if (KCStorageFacility.colonyResources(item.Key, Colony) < item.Value * deltaTime)
                {
                    Configuration.writeDebug($"KCFusionReactor ({name}): Not enough {item.Key.name} to produce EC");
                    return false;
                }
            }
            foreach (KeyValuePair<PartResourceDefinition, double> item in FusionInfo.OutputResources[powerLevel])
            {
                if (KCStorageFacility.colonyResourceSpace(item.Key, Colony) < item.Value * deltaTime)
                {
                    Configuration.writeDebug($"KCFusionReactor ({name}): Unable to store {item.Key.name}");
                    return false;
                }
            }

            return true;
        }

        public double ProduceEC(double lastTime, double deltaTime, double currentTime)
        {
            KCFusionInfo fusionInfo = FusionInfo;

            SortedDictionary<int, double> powerLevels = PossiblePowerLevels();
            if (!CanProduceEC(deltaTime, lastPowerLevel.Key) || OutOfEC || powerLevels.Count == 0)
            {
                Active = false;
                Configuration.writeDebug($"KCFusionReactor: can't produce EC, OutOfEC: {OutOfEC}, powerlevels count: {powerLevels.Count}");
            }
            if (!Active && lastECPerSecond == 0)
            {
                Configuration.writeDebug($"KCFusionReactor: attempted start while being unable to produce power");
                return 0.0;
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
                    powerLevel = new KeyValuePair<int, double>(ManualPowerLevel, powerLevels[ManualPowerLevel]);
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
                                double minThrottle = Math.Max(fusionInfo.MinECThrottle[kvp.Key], Math.Ceiling(Math.Min(1, 1 - kvpDelta / kvp.Value) * 1000) / 1000.0);
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

                Configuration.writeDebug($"KCFusionReactor: power: {powerLevel.Value}, throttle: {throttle}");

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
                    Configuration.writeDebug($"KCFusionReactor: last ec/s: {lastECPerSecond}, throttle: {currentThrottle}, ec produced: {TargetOutput * deltaTime}");
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
                else if (lastECPerSecond > fusionInfo.ECChangeThreshold[level])
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

                    if (currentTime - ChangeTime > ChangeDuration + fusionInfo.LevelOffTime[level])
                    {
                        ShuttingDown = false;
                        ChangingOutput = false;
                        lastPowerLevel = new KeyValuePair<int, double>(-1, 0.0);
                        Configuration.writeLog($"KCFusionReactor: Finishing shutdown of fusion reactor {name} at time {currentTime}");
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

            FusionInfo.InputResources[lastPowerLevel.Key].ToList().ForEach(item =>
            {
                KCStorageFacility.addResourceToColony(item.Key, -item.Value * currentThrottle * deltaTime, Colony);
            });
            FusionInfo.OutputResources[lastPowerLevel.Key].ToList().ForEach(item =>
            {
                KCStorageFacility.addResourceToColony(item.Key, item.Value * currentThrottle * deltaTime, Colony);
            });
        }

        private double producedEC = 0.0;
        public void ExecuteProduction(double lastTime, double deltaTime, double currentTime)
        {
            if (lastECPerSecond <= 0 && !Active && !ShuttingDown)
            {
                producedEC = 0;
                ActualLastECPerSecond = 0;
            }
            else
            {
                producedEC = ProduceEC(lastTime, deltaTime, currentTime) - FusionInfo.ECperSecond[lastPowerLevel.Key == -1 ? 0 : lastPowerLevel.Key] * deltaTime;
                ActualLastECPerSecond = lastECPerSecond - FusionInfo.ECperSecond[lastPowerLevel.Key == -1 ? 0 : lastPowerLevel.Key];
            }
        }
        public double ECProduction(double lastTime, double deltaTime, double currentTime)
        {
            ExecuteProduction(lastTime, deltaTime, currentTime);
            return Math.Max(producedEC, 0);
        }

        public double ECPerSecond() => ActualLastECPerSecond;

        public int ECConsumptionPriority { get; set; } = int.MinValue;
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => Math.Max(-producedEC, 0);

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => OutOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => OutOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;


        public override void OnBuildingClicked()
        {
            window.Toggle();
        }
        public override void OnRemoteClicked()
        {
            window.Toggle();
        }

        public override string GetFacilityProductionDisplay() => $"Fusion reactor production rate: {ECPerSecond():f2} EC/s";

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            ConfigNode fusionNode = new ConfigNode("fusionNode");
            fusionNode.AddValue("Active", Active);
            fusionNode.AddValue("ManualControl", ManualControl);
            fusionNode.AddValue("ManualPowerLevel", ManualPowerLevel);
            fusionNode.AddValue("ManualThrottle", ManualThrottle);
            fusionNode.AddValue("ChangingOutput", ChangingOutput);
            fusionNode.AddValue("ChangingPowerLevel", ChangingPowerLevel);
            fusionNode.AddValue("ChangingThrottle", ChangingThrottle);
            fusionNode.AddValue("OldOutput", OldOutput);
            fusionNode.AddValue("TargetOutput", TargetOutput);
            fusionNode.AddValue("LastResetOutput", LastResetOutput);
            fusionNode.AddValue("b", b);
            fusionNode.AddValue("ChangeTime", ChangeTime);
            fusionNode.AddValue("ChangeDuration", ChangeDuration);
            fusionNode.AddValue("ResetPowerTarget", ResetPowerTarget);
            fusionNode.AddValue("ShuttingDown", ShuttingDown);
            fusionNode.AddValue("PowerLevelTarget", PowerLevelTarget);
            fusionNode.AddValue("PowerLevelOutput", PowerLevelOutput);
            fusionNode.AddValue("PowerLevelThrottleTarget", PowerLevelThrottleTarget);
            fusionNode.AddValue("PowerLevelChangeTime", PowerLevelChangeTime);
            fusionNode.AddValue("SetPowerLevelChangeTime", SetPowerLevelChangeTime);
            fusionNode.AddValue("PowerLevelLevelOffTime", PowerLevelLevelOffTime);
            fusionNode.AddValue("SetPowerLevelLevelOffTime", SetPowerLevelLevelOffTime);
            fusionNode.AddValue("ThrottleTarget", ThrottleTarget);
            fusionNode.AddValue("ThrottleChangeTime", ThrottleChangeTime);
            fusionNode.AddValue("SetThrottleChangeTime", SetThrottleChangeTime);
            fusionNode.AddValue("ResetThrottleTarget", ResetThrottleTarget);
            fusionNode.AddValue("OldThrottleTarget", OldThrottleTarget);
            fusionNode.AddValue("NewThrottleTarget", NewThrottleTarget);
            fusionNode.AddValue("lastECPerSecond", lastECPerSecond);
            fusionNode.AddValue("lastPowerLevel", lastPowerLevel.Key);
            fusionNode.AddValue("currentPowerLevel", currentPowerLevel);
            fusionNode.AddValue("currentThrottle", currentThrottle);

            node.AddNode(fusionNode);
            return node;
        }

        public KCFusionReactor(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            window = new KCFusionWindow(this);

            ConfigNode fusionNode = node.GetNode("fusionNode");

            Active = bool.Parse(fusionNode.GetValue("Active"));
            ManualControl = bool.Parse(fusionNode.GetValue("ManualControl"));
            ManualPowerLevel = int.Parse(fusionNode.GetValue("ManualPowerLevel"));
            ManualThrottle = double.Parse(fusionNode.GetValue("ManualThrottle"));
            ChangingOutput = bool.Parse(fusionNode.GetValue("ChangingOutput"));
            ChangingPowerLevel = bool.Parse(fusionNode.GetValue("ChangingPowerLevel"));
            ChangingThrottle = bool.Parse(fusionNode.GetValue("ChangingThrottle"));
            OldOutput = double.Parse(fusionNode.GetValue("OldOutput"));
            TargetOutput = double.Parse(fusionNode.GetValue("TargetOutput"));
            LastResetOutput = double.Parse(fusionNode.GetValue("LastResetOutput"));
            b = double.Parse(fusionNode.GetValue("b"));
            ChangeTime = double.Parse(fusionNode.GetValue("ChangeTime"));
            ChangeDuration = double.Parse(fusionNode.GetValue("ChangeDuration"));
            ResetPowerTarget = bool.Parse(fusionNode.GetValue("ResetPowerTarget"));
            ShuttingDown = bool.Parse(fusionNode.GetValue("ShuttingDown"));
            PowerLevelTarget = int.Parse(fusionNode.GetValue("PowerLevelTarget"));
            PowerLevelOutput = double.Parse(fusionNode.GetValue("PowerLevelOutput"));
            PowerLevelThrottleTarget = double.Parse(fusionNode.GetValue("PowerLevelThrottleTarget"));
            PowerLevelChangeTime = double.Parse(fusionNode.GetValue("PowerLevelChangeTime"));
            SetPowerLevelChangeTime = bool.Parse(fusionNode.GetValue("SetPowerLevelChangeTime"));
            PowerLevelLevelOffTime = double.Parse(fusionNode.GetValue("PowerLevelLevelOffTime"));
            SetPowerLevelLevelOffTime = bool.Parse(fusionNode.GetValue("SetPowerLevelLevelOffTime"));
            ThrottleTarget = double.Parse(fusionNode.GetValue("ThrottleTarget"));
            ThrottleChangeTime = double.Parse(fusionNode.GetValue("ThrottleChangeTime"));
            SetThrottleChangeTime = bool.Parse(fusionNode.GetValue("SetThrottleChangeTime"));
            ResetThrottleTarget = bool.Parse(fusionNode.GetValue("ResetThrottleTarget"));
            OldThrottleTarget = double.Parse(fusionNode.GetValue("OldThrottleTarget"));
            NewThrottleTarget = double.Parse(fusionNode.GetValue("NewThrottleTarget"));
            lastECPerSecond = double.Parse(fusionNode.GetValue("lastECPerSecond"));
            int lastPowerLevelKey = int.Parse(fusionNode.GetValue("lastPowerLevel"));
            lastPowerLevel = new KeyValuePair<int, double>(lastPowerLevelKey, lastPowerLevelKey != -1 ? FusionInfo.ECProduction[lastPowerLevelKey] : 0);
            currentPowerLevel = int.Parse(fusionNode.GetValue("currentPowerLevel"));
            currentThrottle = double.Parse(fusionNode.GetValue("currentThrottle"));
        }

        public KCFusionReactor(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCFusionWindow(this);
            lastPowerLevel = new KeyValuePair<int, double>(-1, 0.0);
            currentPowerLevel = -1;
        }
    }

}
