using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage;
using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.VesselAutoTransfer
{
    public class ModuleKCTransfer : PartModule
    {
        [KSPField(isPersistant = true)]
        public ResourceFlowMode transferMode = ResourceFlowMode.ALL_VESSEL_BALANCE;

        public KCTransferInfo transferInfo;

        [KSPField(isPersistant = true)]
        public double lastUpdateTime = 0;

        private VesselColonyTransferchangewindow ColonyChangeWindow;
        private VesselResourceRatesChangewindow ResourceRatesChangeWindow;

        private Dictionary<PartResourceDefinition, double> lastResourceValue = new Dictionary<PartResourceDefinition, double> { };

        [KSPEvent(guiName = "Change target colony", active = true, advancedTweakable = false, category = "Colonytransfer", externalToEVAOnly = false, groupDisplayName = null, groupName = null, groupStartCollapsed = false, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void ChangeColonyTarget()
        {
            if (!ColonyChangeWindow.IsOpen()) ColonyChangeWindow.Open();
        }

        [KSPEvent(guiName = "Change resource rates", active = true, advancedTweakable = false, category = "Colonytransfer", externalToEVAOnly = false, groupDisplayName = null, groupName = null, groupStartCollapsed = false, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void ChangeResourceRates()
        {
            if (transferInfo == null)
            {
                ChangeColonyTarget();
                return;
            }

            if (!ResourceRatesChangeWindow.IsOpen()) ResourceRatesChangeWindow.Open();
        }

        public override void OnUpdate()
        {
            if (lastUpdateTime == 0)
            {
                Configuration.writeDebug($"ERROR: lastUpdateTime was 0, resetting to current time");
                lastUpdateTime = Planetarium.GetUniversalTime();
                return;
            }
            double currentTime = Planetarium.GetUniversalTime();
            double deltaTime = currentTime - lastUpdateTime;

            if (deltaTime == 0) return;

            lastUpdateTime = currentTime;

            if (vessel.srfSpeed > 0.5 || !vessel.LandedOrSplashed)
            {
                ColonyChangeWindow.Close();
                ResourceRatesChangeWindow.Close();
                transferInfo?.Delete();
                transferInfo = null;
                return;
            }
            if (transferInfo == null)
            {
                return;
            }

            Dictionary<PartResourceDefinition, double> VesselResources = new Dictionary<PartResourceDefinition, double>();

            PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            colonyClass colony = transferInfo.Colony;
            List<KCECStorageFacility> ecStorages = KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony);

            this.part.GetConnectedResourceTotals(EC.id, transferMode, out double vesselAmount, out double vesselMaxAmount);
            VesselResources.Add(EC, vesselAmount);

            foreach (PartResourceDefinition res in transferInfo.resources.ToList())
            {
                lastResourceValue.TryAdd(res, 0);
                double lastAmount = lastResourceValue[res];

                if (res == EC)
                {
                    part.GetConnectedResourceTotals(EC.id, transferMode, out double currentEC, out double maxEC);
                    lastResourceValue[res] = currentEC;
                    double ECRatio = currentEC / maxEC;
                    double ECDelta = (lastAmount - currentEC) / deltaTime - transferInfo.ResourcesActual[res];

                    double transferAmount = 0;

                    if (transferInfo.ResourcesTarget[res] > 0)
                    {
                        if (ECRatio > transferInfo.VesselTransferLimits[res])
                        {
                            transferAmount = transferInfo.ResourcesTarget[res] * transferInfo.Efficiency;

                            double newRatio = (currentEC - transferAmount - ECDelta) / maxEC;

                            if (newRatio < transferInfo.VesselTransferLimits[res])
                            {
                                double limitPercent = Math.Abs(transferInfo.VesselTransferLimits[res] - ECRatio);
                                transferAmount = Math.Min(Math.Max((transferInfo.VesselTransferLimits[res] - ECRatio) * maxEC, 0) + (ECDelta < 0 && limitPercent < 0.01 ? -ECDelta : 0), transferInfo.ResourcesTarget[res]);
                            }
                        }

                        if (transferInfo.ColonyConstrained[res])
                        {
                            if (transferInfo.ResourcesActual[res] > transferAmount)
                            {
                                transferInfo.VesselConstrained[res] = true;
                                transferInfo.ColonyConstrained[res] = false;

                                if (transferInfo.DisableIfVesselConstrains[res])
                                {
                                    transferAmount = 0;
                                    transferInfo.CleanResources();
                                }

                                transferInfo.ResourcesActual[res] = transferAmount;
                            }
                            else
                            {
                                transferAmount = transferInfo.ResourcesActual[res];
                            }
                        }
                        else if (transferAmount != -transferInfo.ResourcesActual[res])
                        {
                            if (transferAmount != transferInfo.ResourcesTarget[res])
                            {
                                transferInfo.ColonyConstrained[res] = false;
                                transferInfo.VesselConstrained[res] = true;

                                if (transferInfo.DisableIfVesselConstrains[res])
                                {
                                    transferAmount = 0;
                                    transferInfo.CleanResources();
                                }
                            }
                            else
                            {
                                transferInfo.VesselConstrained[res] = false;
                            }

                            transferInfo.ResourcesActual[res] = transferAmount;
                        }
                    }
                    else if (transferInfo.ResourcesTarget[res] < 0)
                    {
                        if (ECRatio < transferInfo.VesselTransferLimits[res])
                        {
                            transferAmount = transferInfo.ResourcesTarget[res] * transferInfo.Efficiency;

                            double newRatio = (currentEC - transferAmount - ECDelta) / maxEC;

                            if (newRatio > transferInfo.VesselTransferLimits[res])
                            {
                                double limitPercent = Math.Abs(transferInfo.VesselTransferLimits[res] - ECRatio);
                                transferAmount = Math.Max(Math.Min((transferInfo.VesselTransferLimits[res] - ECRatio) * maxEC, 0) + (ECDelta < 0 && limitPercent < 0.01 ? -ECDelta : 0), transferInfo.ResourcesTarget[res]);
                            }
                        }

                        if (transferInfo.ColonyConstrained[res])
                        {
                            if (transferInfo.ResourcesActual[res] < transferAmount)
                            {
                                transferInfo.VesselConstrained[res] = true;
                                transferInfo.ColonyConstrained[res] = false;

                                if (transferInfo.DisableIfVesselConstrains[res])
                                {
                                    transferAmount = 0;
                                }

                                transferInfo.ResourcesActual[res] = transferAmount;
                            }
                            else
                            {
                                transferAmount = transferInfo.ResourcesActual[res];
                            }
                        }
                        else if (transferAmount != transferInfo.ResourcesActual[res])
                        {
                            if (transferAmount != transferInfo.ResourcesTarget[res])
                            {
                                transferInfo.VesselConstrained[res] = true;
                                transferInfo.ColonyConstrained[res] = false;

                                if (transferInfo.DisableIfVesselConstrains[res])
                                {
                                    transferAmount = 0;
                                    transferInfo.CleanResources();
                                }
                            }
                            else
                            {
                                transferInfo.VesselConstrained[res] = false;
                            }

                            transferInfo.ResourcesActual[res] = transferAmount;
                        }
                        else if (transferAmount == transferInfo.ResourcesTarget[res])
                        {
                            transferInfo.VesselConstrained[res] = false;

                            transferInfo.ResourcesActual[res] = transferAmount;
                        }
                    }
                    else
                    {
                        transferInfo.ResourcesActual[res] = transferAmount;
                        transferInfo.ECTransferProducer.EC = 0;
                    }

                    Configuration.writeDebug($"ModuleKCTransfer: Requesting {transferAmount} (dt: {deltaTime}) from {part.name.ToString()} in vessel {part.vessel.name}");
                    part.RequestResource(res.id, transferAmount * deltaTime, transferMode);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (transferInfo != null) node.AddNode(transferInfo.Save());
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            transferInfo = KCTransferInfo.Load(node);
        }

        public ModuleKCTransfer()
        {
            ColonyChangeWindow = new VesselColonyTransferchangewindow(this);
            ResourceRatesChangeWindow = new VesselResourceRatesChangewindow(this);
        }
    }
}
