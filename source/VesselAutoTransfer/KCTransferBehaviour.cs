using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Behaviour;
using KerbalColonies.Settings;
using System.Collections.Generic;
using System.Linq;

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
    public class KCTransferBehaviour : ConverterBehaviour
    {
        [KSPField(isPersistant = true)]
        public ResourceFlowMode transferMode = ResourceFlowMode.ALL_VESSEL_BALANCE;

        public KCTransferInfo transferInfo;

        [KSPField(isPersistant = true)]
        private double lastUpdateTime = 0;

        public override ConverterResources GetResources(VesselState state)
        {
            BackgroundResourceProcessing.Core.InventoryState inventoryState = state.Processor.GetResourceState("ElectricCharge");
            Configuration.writeDebug($"KCTransferBehaviour GetResources called for vessel {state.Processor.Vessel.vesselName}. EC remaining: {inventoryState.amount}/{inventoryState.maxAmount}, {inventoryState.rate}/s");

            ConverterResources resources = new()
            {
                Inputs = [],
                Outputs = [],
                Requirements = []
            };


            PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            transferInfo?.resources.ForEach(res =>
            {
                ResourceRatio ratio = new(res.name, transferInfo.ResourcesTarget[res], false);

                ResourceConstraint constraint = new(ratio);
                double requirement = transferInfo.VesselTransferLimits[res] * state.Processor.GetResourceState(res.name).maxAmount;
                constraint.Amount = requirement;

                if (transferInfo.ResourcesTarget[res] > 0)
                {
                    constraint.Constraint = Constraint.AT_LEAST;

                    resources.Inputs.Add(ratio);
                    resources.Requirements.Add(constraint);
                }
                else
                {
                    constraint.Constraint = Constraint.AT_MOST;

                    resources.Outputs.Add(ratio);
                    resources.Requirements.Add(constraint);
                }
            });

            return resources;
        }


        public override void OnRatesComputed(BackgroundResourceProcessor processor, BackgroundResourceProcessing.Core.ResourceConverter converter, RateCalculatedEvent evt)
        {
            if (converter == null || processor == null) return;

            // resource limits are enforced by the constraints
            // rates will be updated through the OnRatesComputed event
            // although it's also necessary to update the rates on every change point as e.g. solar panels will change the possible rates

            converter.Inputs.ToList().ForEach(kvp =>
            {
                double resRate = kvp.Value.Ratio * converter.Rate;
                PartResourceDefinition resDef = PartResourceLibrary.Instance.GetDefinition(kvp.Value.ResourceName);
                transferInfo.ResourceTransferHandler.ResourceRates[resDef] = resRate;
            });

            if (FlightGlobals.ActiveVessel != processor.Vessel)
            {
                transferInfo.Efficiency = converter.Rate;

                if (transferInfo.Efficiency < 1)
                {
                    if (transferInfo.Efficiency <= 0.00001)
                    {
                        transferInfo.Efficiency = 0;
                        transferInfo.resources.ForEach(res =>
                        {
                            transferInfo.ResourcesActual[res] = 0;
                            transferInfo.VesselConstrained[res] = true;
                            transferInfo.ColonyConstrained[res] = false;

                            if (transferInfo.DisableIfVesselConstrains[res])
                            {
                                Configuration.writeDebug($"Disabling transfer of {res.name} due to vessel constraint.");
                                KeyValuePair<int, ResourceRatio> resKVP = converter.Inputs.First(kvp => kvp.Value.ResourceName == res.name);
                                converter.Inputs[resKVP.Key].Ratio = 0;
                            }
                        });
                        transferInfo.resources.Clear();
                        return;
                    }

                    transferInfo.resources.ForEach(res =>
                    {
                        KeyValuePair<int, ResourceRatio> resKVP = converter.Inputs.First(kvp => kvp.Value.ResourceName == res.name);
                        ResourceRatio resRatio = resKVP.Value;

                        if (transferInfo.ColonyConstrained[res])
                        {
                            if (resRatio.Ratio > transferInfo.ResourcesActual[res])
                            {
                                resRatio.Ratio = transferInfo.ResourcesActual[res] / converter.Rate;
                            }
                        }
                        else
                        {
                            transferInfo.VesselConstrained[res] = true;
                            if (transferInfo.DisableIfVesselConstrains[res])
                            {
                                Configuration.writeDebug($"Disabling transfer of {res.name} due to vessel constraint.");
                                resRatio.Ratio = 0;
                                transferInfo.resources.Clear();
                            }
                            else
                            {
                                transferInfo.ResourcesActual[res] = resRatio.Ratio * converter.Rate;
                            }
                        }
                    });
                }
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (transferInfo != null) node.AddNode(transferInfo.Save());
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            transferInfo = KCTransferInfo.Load(node);
        }

        public KCTransferBehaviour()
        {

        }

        public KCTransferBehaviour(ModuleKCTransfer module)
        {
            transferMode = module.transferMode;

            KCTransferInfo info = module.transferInfo;

            if (info == null || info.Colony == null)
            {
                Configuration.writeDebug($"ERROR: TransferInfo or Colony was null in ModuleKCTransfer {module.part.name} on vessel {module.vessel.vesselName}");
                return;
            }

            transferInfo = info;
            lastUpdateTime = module.lastUpdateTime;
            Priority = int.MinValue;
        }
    }
}
