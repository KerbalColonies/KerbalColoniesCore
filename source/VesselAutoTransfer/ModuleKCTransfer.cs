using System;
using System.Collections.Generic;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

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

        private Dictionary<PartResourceDefinition, double> lastResourceValue = [];

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

            colonyClass colony = transferInfo.Colony;

            transferInfo.resources.ForEach(res =>
            {
                lastResourceValue.TryAdd(res, 0);
                double lastAmount = lastResourceValue[res];

                part.GetConnectedResourceTotals(res.id, transferMode, out double vesselAmount, out double vesselMaxAmount);
                lastResourceValue[res] = vesselAmount;
                double ResRatio = vesselAmount / vesselMaxAmount;
                double ResDelta = ((lastAmount - vesselAmount) / deltaTime) - transferInfo.ResourcesActual[res];

                double transferAmount = 0;

                if (transferInfo.ResourcesTarget[res] > 0)
                {
                    if (ResRatio > transferInfo.VesselTransferLimits[res])
                    {
                        transferAmount = transferInfo.ResourcesTarget[res] * transferInfo.Efficiency;

                        double newRatio = (vesselAmount - transferAmount - ResDelta) / vesselMaxAmount;

                        if (newRatio < transferInfo.VesselTransferLimits[res])
                        {
                            double limitPercent = Math.Abs(transferInfo.VesselTransferLimits[res] - ResRatio);
                            transferAmount = Math.Min(Math.Max((transferInfo.VesselTransferLimits[res] - ResRatio) * vesselMaxAmount, 0) + (ResDelta < 0 && limitPercent < 0.01 ? -ResDelta : 0), transferInfo.ResourcesTarget[res]);
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
                    if (ResRatio < transferInfo.VesselTransferLimits[res])
                    {
                        transferAmount = transferInfo.ResourcesTarget[res] * transferInfo.Efficiency;

                        double newRatio = (vesselAmount - transferAmount - ResDelta) / vesselMaxAmount;

                        if (newRatio > transferInfo.VesselTransferLimits[res])
                        {
                            double limitPercent = Math.Abs(transferInfo.VesselTransferLimits[res] - ResRatio);
                            transferAmount = Math.Max(Math.Min((transferInfo.VesselTransferLimits[res] - ResRatio) * vesselMaxAmount, 0) + (ResDelta < 0 && limitPercent < 0.01 ? -ResDelta : 0), transferInfo.ResourcesTarget[res]);
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
                    transferInfo.ResourceTransferHandler.ResourceRates[res] = 0;
                }

                Configuration.writeDebug($"ModuleKCTransfer: Requesting {transferAmount} (dt: {deltaTime}) from {part.name} in vessel {part.vessel.name}");
                part.RequestResource(res.id, transferAmount * deltaTime, transferMode);

            });
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
