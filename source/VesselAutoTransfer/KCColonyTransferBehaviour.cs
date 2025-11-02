using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.VesselAutoTransfer
{
    public class KCColonyTransferBehaviour
    {
        public static Dictionary<uint, KCTransferInfo> ActiveTransfers { get; protected set; } = new Dictionary<uint, KCTransferInfo> { };

        public class KCColonyTransferECHandler : KCECProducer, KCECConsumer
        {
            private double ec;
            public double EC
            {
                get => ec; set
                {
                    Configuration.writeDebug($"Setting EC transfer handler EC to {value}");
                    ec = value;
                }
            }
            public int ECConsumptionPriority => 0;
            public uint partModuleID = 0;

            public double ECProduction(double lastTime, double deltaTime, double currentTime) => EC > 0 ? EC * deltaTime : 0;

            public double ECPerSecond() => EC > 0 ? EC : 0;

            public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => EC < 0 ? -EC * deltaTime : 0;

            public void ConsumeEC(double lastTime, double deltaTime, double currentTime)
            {
            }

            public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC)
            {
                // TODO
            }

            public double DailyECConsumption() => EC < 0 ? -EC * 60 * 60 * 6 : 0;

            public KCColonyTransferECHandler(colonyClass colony, uint partModuleID)
            {
                Configuration.writeDebug($"Creating EC transfer handler with partModuleID {partModuleID}");

                if (!KCECManager.otherProducers.ContainsKey(colony)) KCECManager.otherProducers.Add(colony, new List<KCECProducer>());
                if (!KCECManager.otherConsumers.ContainsKey(colony)) KCECManager.otherConsumers.Add(colony, new SortedDictionary<int, List<KCECConsumer>>());
                if (!KCECManager.otherConsumers[colony].ContainsKey(0)) KCECManager.otherConsumers[colony][0] = new List<KCECConsumer>();

                Configuration.writeDebug($"Existing EC transfer handlers count: {KCECManager.otherProducers[colony].OfType<KCColonyTransferECHandler>().Count()}");
                //foreach (KCColonyTransferECHandler item in KCECManager.otherProducers[colony].ToList().OfType<KCColonyTransferECHandler>())
                //{
                //    Configuration.writeDebug($"Found EC transfer handler with partModuleID {item.partModuleID}");
                //    if (item.partModuleID == partModuleID)
                //    {
                //        Configuration.writeDebug($"EC transfer handler for partModuleID {partModuleID} already exists in otherProducers, removing it first");
                //        KCECManager.otherProducers[colony].Remove(item);
                //    }
                //}
                //Configuration.writeDebug($"New EC transfer handlers count: {KCECManager.otherProducers[colony].OfType<KCColonyTransferECHandler>().Count()}");


                KCECManager.otherProducers[colony].OfType<KCColonyTransferECHandler>().Where(prod => prod.partModuleID == partModuleID).ToList().ForEach(prod => KCECManager.otherProducers[colony].Remove(prod));

                KCECManager.otherProducers[colony].Add(this);
                KCECManager.otherConsumers[colony][0].Add(this);

                this.partModuleID = partModuleID;
            }
        }

        public static void ColonyLoadAction(colonyClass colony)
        {
            ActiveTransfers.Values.ToList().ForEach(t =>
            {
                if (t.Colony.uniqueID == colony.uniqueID)
                {
                    t.Delete();
                }
            });

            if (KCECManager.otherProducers.ContainsKey(colony))
            {
                KCECManager.otherProducers[colony] = new List<KCECProducer>();
            }
            else
            {
                KCECManager.otherProducers.Add(colony, new List<KCECProducer>());
            }
        }


        public static void ColonyUpdateTransferAction(colonyClass colony)
        {
            PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            ActiveTransfers.Values.Where(t => t.Colony == colony).ToList().ForEach(t =>
            {
                t.resources.ForEach(res =>
                {
                    if (res == EC)
                    {
                        List<KCECStorage> colonyresStorages = colony.Facilities.OfType<KCECStorage>().ToList();
                        double maxColonyRes = colonyresStorages.Sum(f => f.ECCapacity);
                        double currentColonyRes = colonyresStorages.Sum(f => f.ECStored);
                        double colonyResRatio = currentColonyRes / maxColonyRes;

                        KCColonyECData colonyresData = KCECManager.colonyEC[colony];

                        double transferAmount = 0;

                        if (t.ResourcesTarget[res] > 0)
                        {
                            if (colonyResRatio < t.ColonyTransferLimits[res])
                            {
                                transferAmount = t.ResourcesTarget[res] * t.Efficiency / t.EfficiencyBalancer[res];

                                double newRatio = (currentColonyRes + transferAmount + colonyresData.lastECDelta) / maxColonyRes;

                                if (newRatio > t.ColonyTransferLimits[res])
                                {
                                    double limitPercent = Math.Abs(t.ColonyTransferLimits[res] - colonyResRatio);
                                    transferAmount = Math.Min(Math.Max((t.ColonyTransferLimits[res] - colonyResRatio) * maxColonyRes, 0) + (colonyresData.lastECDelta > 0 && limitPercent < 0.01 ? colonyresData.lastECDelta : 0), t.ResourcesTarget[res]);
                                }
                            }

                            if (t.VesselConstrained[res])
                            {
                                if (t.ResourcesActual[res] > transferAmount)
                                {
                                    t.VesselConstrained[res] = false;
                                    t.ColonyConstrained[res] = true;

                                    if (t.DisableIfColonyConstrains[res])
                                    {
                                        transferAmount = 0;
                                        t.CleanResources();
                                    }

                                    t.ResourcesActual[res] = transferAmount;
                                    t.ECTransferProducer.EC = transferAmount;
                                }
                                else
                                {
                                    t.ECTransferProducer.EC = t.ResourcesActual[res];
                                }
                            }
                            else if (transferAmount != t.ResourcesActual[res])
                            {
                                if (transferAmount != t.ResourcesTarget[res])
                                {
                                    t.VesselConstrained[res] = false;
                                    t.ColonyConstrained[res] = true;

                                    if (t.DisableIfColonyConstrains[res])
                                    {
                                        transferAmount = 0;
                                        t.CleanResources();
                                    }
                                }
                                else
                                {
                                    t.ColonyConstrained[res] = false;
                                }

                                t.ResourcesActual[res] = transferAmount;
                                t.ECTransferProducer.EC = transferAmount;
                            }
                            else
                            {
                                t.ResourcesActual[res] = transferAmount;
                                t.ECTransferProducer.EC = transferAmount;
                            }
                        }
                        else if (t.ResourcesTarget[res] < 0)
                        {
                            if (colonyResRatio > t.ColonyTransferLimits[res])
                            {
                                transferAmount = t.ResourcesTarget[res] * t.Efficiency / t.EfficiencyBalancer[res];

                                double newRatio = (currentColonyRes + transferAmount + colonyresData.lastECDelta) / maxColonyRes;

                                if (newRatio < t.ColonyTransferLimits[res])
                                {
                                    double limitPercent = Math.Abs(t.ColonyTransferLimits[res] - colonyResRatio);
                                    transferAmount = Math.Max(Math.Min((t.ColonyTransferLimits[res] - colonyResRatio) * maxColonyRes, 0) + (colonyresData.lastECDelta < 0 && limitPercent < 0.01 ? colonyresData.lastECDelta : 0), t.ResourcesTarget[res]);
                                }
                            }

                            if (t.VesselConstrained[res])
                            {
                                if (t.ResourcesActual[res] < transferAmount)
                                {
                                    t.VesselConstrained[res] = false;
                                    t.ColonyConstrained[res] = true;

                                    if (t.DisableIfColonyConstrains[res])
                                    {
                                        transferAmount = 0;
                                        t.CleanResources();
                                    }

                                    t.ResourcesActual[res] = transferAmount;
                                    t.ECTransferProducer.EC = transferAmount;
                                }
                                else
                                {
                                    t.ECTransferProducer.EC = t.ResourcesActual[res];
                                }
                            }
                            else if (transferAmount != t.ResourcesActual[res])
                            {
                                if (transferAmount != t.ResourcesTarget[res])
                                {
                                    t.VesselConstrained[res] = false;
                                    t.ColonyConstrained[res] = true;

                                    if (t.DisableIfColonyConstrains[res])
                                    {
                                        transferAmount = 0;
                                        t.CleanResources();
                                    }
                                }
                                else
                                {
                                    t.ColonyConstrained[res] = false;
                                }

                                t.ResourcesActual[res] = transferAmount;
                                t.ECTransferProducer.EC = transferAmount;
                            }
                            else if (transferAmount == t.ResourcesTarget[res])
                            {
                                t.ColonyConstrained[res] = false;

                                t.ResourcesActual[res] = transferAmount;
                                t.ECTransferProducer.EC = transferAmount;
                            }
                            else
                            {
                                t.ResourcesActual[res] = transferAmount;
                                t.ECTransferProducer.EC = transferAmount;
                            }
                        }
                        else
                        {
                            t.ResourcesActual[res] = 0;
                            t.ECTransferProducer.EC = 0;
                        }
                    }
                });
            });
        }
    }
}
