using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage;
using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.VesselAutoTransfer
{
    public class KCColonyTransferBehaviour
    {
        public class KCTransferInfo
        {
            private colonyClass colony;

            public colonyClass Colony => colony;

            public List<PartResourceDefinition> resources = new List<PartResourceDefinition>();

            public Dictionary<PartResourceDefinition, double> ToColonyResourcesTarget = new Dictionary<PartResourceDefinition, double>();
            public Dictionary<PartResourceDefinition, double> ToVesselResourcesTarget = new Dictionary<PartResourceDefinition, double>();

            public Dictionary<PartResourceDefinition, double> ToColonyResourcesActual = new Dictionary<PartResourceDefinition, double>();
            public Dictionary<PartResourceDefinition, double> ToVesselResourcesActual = new Dictionary<PartResourceDefinition, double>();

            public Dictionary<PartResourceDefinition, double> ColonyTransferLimits = new Dictionary<PartResourceDefinition, double>();
            public Dictionary<PartResourceDefinition, double> VesselTransferLimits = new Dictionary<PartResourceDefinition, double>();

            public Dictionary<PartResourceDefinition, bool> DisableIfVesselConstrains = new Dictionary<PartResourceDefinition, bool>();
            public Dictionary<PartResourceDefinition, bool> DisableIfColonyConstrains = new Dictionary<PartResourceDefinition, bool>();

            public Dictionary<PartResourceDefinition, bool> VesselConstrained = new Dictionary<PartResourceDefinition, bool>();
            public Dictionary<PartResourceDefinition, bool> ColonyConstrained = new Dictionary<PartResourceDefinition, bool>();

            public KCColonyTransferECHandler ECTransferProducer = null;

            public uint partModuleID;
            public uint vesselID;

            public void AddResource(PartResourceDefinition resource)
            {
                if (!resources.Contains(resource))
                {
                    resources.Add(resource);

                    ToColonyResourcesTarget[resource] = 0;
                    ToVesselResourcesTarget[resource] = 0;
                    ToColonyResourcesActual[resource] = 0;
                    ToVesselResourcesActual[resource] = 0;
                    ColonyTransferLimits[resource] = 1;
                    VesselTransferLimits[resource] = 1;
                    DisableIfVesselConstrains[resource] = false;
                    DisableIfColonyConstrains[resource] = false;
                    VesselConstrained[resource] = false;
                    ColonyConstrained[resource] = false;
                }
            }

            public void CleanResources()
            {
                PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

                resources.ToList().ForEach(r =>
                {
                    if (ToColonyResourcesTarget[r] == 0 && ToVesselResourcesTarget[r] == 0)
                    {
                        if (r == EC) ECTransferProducer.EC = 0;

                        resources.Remove(r);
                        ToColonyResourcesTarget.Remove(r);
                        ToVesselResourcesTarget.Remove(r);
                        ToColonyResourcesActual.Remove(r);
                        ToVesselResourcesActual.Remove(r);
                        ColonyTransferLimits.Remove(r);
                        VesselTransferLimits.Remove(r);
                        DisableIfVesselConstrains.Remove(r);
                        DisableIfColonyConstrains.Remove(r);
                        VesselConstrained.Remove(r);
                        ColonyConstrained.Remove(r);
                    }
                });
            }

            public void Delete()
            {
                ActiveTransfers.Remove(this);
                if (ECTransferProducer != null)
                {
                    KCECManager.otherProducers[colony].Remove(ECTransferProducer);
                }
            }

            public ConfigNode Save()
            {
                CleanResources();

                ConfigNode node = new ConfigNode("KCTransferInfo");

                node.AddValue("colonyID", colony.uniqueID);
                node.AddValue("partModuleID", partModuleID);
                node.AddValue("vesselID", vesselID);

                ConfigNode resourceNode = new ConfigNode("TransferResources");

                resources.ForEach(r =>
                {
                    string value = $"{ToColonyResourcesTarget[r]},{ToVesselResourcesTarget[r]},{ColonyTransferLimits[r]},{VesselTransferLimits[r]},{DisableIfVesselConstrains[r]},{DisableIfColonyConstrains[r]}";
                    resourceNode.AddValue(r.name, value);
                });

                node.AddNode(resourceNode);

                return node;
            }

            public static KCTransferInfo Load(ConfigNode node)
            {
                node = node.GetNode("KCTransferInfo");

                if (node == null) return null;

                colonyClass colony = Configuration.GetColonyByID(int.Parse(node.GetValue("colonyID")));
                uint partModuleID = uint.Parse(node.GetValue("partModuleID"));
                uint vesselID = uint.Parse(node.GetValue("vesselID"));

                KCTransferInfo info = new KCTransferInfo(colony, partModuleID, vesselID);

                ConfigNode resourceNode = node.GetNode("TransferResources");
                foreach (ConfigNode.Value v in resourceNode.values)
                {
                    PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(v.name);
                    info.AddResource(resource);
                    string[] values = v.value.Split(',');
                    info.ToColonyResourcesTarget[resource] = double.Parse(values[0]);
                    info.ToVesselResourcesTarget[resource] = double.Parse(values[1]);
                    info.ColonyTransferLimits[resource] = double.Parse(values[2]);
                    info.VesselTransferLimits[resource] = double.Parse(values[3]);
                    info.DisableIfVesselConstrains[resource] = bool.Parse(values[4]);
                    info.DisableIfColonyConstrains[resource] = bool.Parse(values[5]);
                }

                return info;
            }

            public KCTransferInfo(colonyClass colony, uint partModuleID, uint vesselID)
            {
                this.colony = colony;
                this.partModuleID = partModuleID;
                this.vesselID = vesselID;

                ECTransferProducer = new KCColonyTransferECHandler(colony);
                ActiveTransfers.Add(this);
            }
        }

        public static List<KCTransferInfo> ActiveTransfers { get; protected set; } = new List<KCTransferInfo>();

        public class KCColonyTransferECHandler : KCECProducer, KCECConsumer
        {
            public double EC = 0;

            public int ECConsumptionPriority => 0;

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

            public KCColonyTransferECHandler(colonyClass colony)
            {
                if (!KCECManager.otherProducers.ContainsKey(colony)) KCECManager.otherProducers[colony] = new List<KCECProducer>();
                if (!KCECManager.otherConsumers.ContainsKey(colony)) KCECManager.otherConsumers[colony] = new SortedDictionary<int, List<KCECConsumer>>();
                if (!KCECManager.otherConsumers[colony].ContainsKey(0)) KCECManager.otherConsumers[colony][0] = new List<KCECConsumer>();

                KCECManager.otherProducers[colony].Add(this);
                KCECManager.otherConsumers[colony][0].Add(this);
            }
        }

        public static void ColonyUpdateTransferAction(colonyClass colony)
        {
            PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            ActiveTransfers.Where(t => t.Colony == colony).ToList().ForEach(t =>
            {
                if (t.ToColonyResourcesTarget.ContainsKey(EC))
                {
                    List<KCECStorageFacility> colonyECStorages = KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony);
                    double maxColonyEC = colonyECStorages.Sum(f => f.ECCapacity);
                    double currentColonyEC = colonyECStorages.Sum(f => f.ECStored);
                    double colonyECRatio = currentColonyEC / maxColonyEC;

                    KCColonyECData colonyECData = KCECManager.colonyEC[colony];

                    double transferAmount = 0;

                    if (t.ToColonyResourcesTarget[EC] > 0)
                    {
                        if (colonyECRatio <= t.ColonyTransferLimits[EC])
                        {
                            transferAmount = t.ToColonyResourcesTarget[EC];

                            double newRatio = (currentColonyEC + transferAmount + colonyECData.lastECDelta) / maxColonyEC;

                            if (newRatio > t.ColonyTransferLimits[EC])
                            {
                                transferAmount = (t.ColonyTransferLimits[EC] - colonyECRatio) * maxColonyEC + (colonyECData.lastECDelta < 0 ? colonyECData.lastECDelta : 0);
                            }
                        }

                        t.ECTransferProducer.EC = transferAmount;

                        if (transferAmount != t.ToColonyResourcesActual[EC])
                        {
                            t.ToColonyResourcesActual[EC] = transferAmount;

                            if (transferAmount != t.ToColonyResourcesTarget[EC])
                            {
                                t.ColonyConstrained[EC] = true;

                                if (t.DisableIfColonyConstrains[EC])
                                {
                                    transferAmount = 0;
                                }
                            }
                            else
                            {
                                t.ColonyConstrained[EC] = false;
                            }
                        }
                    }
                    else if (t.ToVesselResourcesTarget[EC] > 0)
                    {
                        if (colonyECRatio > t.ColonyTransferLimits[EC])
                        {
                            transferAmount = t.ToVesselResourcesTarget[EC];

                            double newRatio = (currentColonyEC - transferAmount + colonyECData.lastECDelta) / maxColonyEC;

                            if (newRatio < t.ColonyTransferLimits[EC])
                            {
                                transferAmount = (t.ColonyTransferLimits[EC] - colonyECRatio) * maxColonyEC + (colonyECData.lastECDelta > 0 ? colonyECData.lastECDelta : 0);
                            }
                        }

                        t.ECTransferProducer.EC = -transferAmount;

                        if (transferAmount != t.ToVesselResourcesActual[EC])
                        {
                            t.ToVesselResourcesActual[EC] = transferAmount;

                            if (transferAmount != t.ToVesselResourcesTarget[EC])
                            {
                                t.ColonyConstrained[EC] = true;

                                if (t.DisableIfColonyConstrains[EC])
                                {
                                    transferAmount = 0;
                                }
                            }
                            else
                            {
                                t.ColonyConstrained[EC] = false;
                            }
                        }
                    }
                    else
                    {
                        t.ECTransferProducer.EC = 0;
                    }
                }
            });
        }
    }
}
