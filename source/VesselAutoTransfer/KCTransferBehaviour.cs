using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Behaviour;
using System.Collections.Generic;
using System.Linq;

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

            ConverterResources resources = new ConverterResources();

            resources.Inputs = new List<ResourceRatio>();
            resources.Outputs = new List<ResourceRatio>();
            resources.Requirements = new List<ResourceConstraint>();


            //ResourceRatio ecr = new ResourceRatio("ElectricCharge", 10, false, transferMode);
            //resources.Inputs.Add(ecr);

            //ResourceConstraint req = new ResourceConstraint(ecr);
            //req.Amount = 5000;
            //req.Constraint = Constraint.AT_LEAST;

            //resources.Requirements.Add(req);

            transferInfo?.resources.ForEach(res =>
            {
                double amount;
                double requirement = transferInfo.VesselTransferLimits[res] * state.Processor.GetResourceState(res.name).maxAmount;

                if (transferInfo.ResourcesTarget[res] > 0)
                {
                    amount = transferInfo.ResourcesTarget[res];
                    ResourceRatio ratio = new ResourceRatio(res.name, amount, false);

                    ResourceConstraint constraint = new ResourceConstraint(ratio);
                    constraint.Amount = requirement;
                    constraint.Constraint = Constraint.AT_LEAST;

                    resources.Inputs.Add(ratio);
                    resources.Requirements.Add(constraint);
                }
                else
                {
                    amount = transferInfo.ResourcesTarget[res];
                    ResourceRatio ratio = new ResourceRatio(res.name, amount, false);

                    ResourceConstraint constraint = new ResourceConstraint(ratio);
                    constraint.Amount = requirement;
                    constraint.Constraint = Constraint.AT_MOST;

                    resources.Outputs.Add(ratio);
                    resources.Requirements.Add(constraint);
                }
            });

            return resources;
        }


        public override void OnRatesComputed(BackgroundResourceProcessor processor, BackgroundResourceProcessing.Core.ResourceConverter converter, RateCalculatedEvent evt)
        {
            // resource limits are enforced by the constraints
            // rates will be updated through the OnRatesComputed event
            // although it's also necessary to update the rates on every change point as e.g. solar panels will change the possible rates

            BackgroundResourceProcessing.Core.InventoryState state = processor.GetResourceState("ElectricCharge");
            Configuration.writeDebug($"KCTransferBehaviour OnRatesComputed called for vessel {processor.Vessel.vesselName}. EC remaining: {state.amount}/{state.maxAmount}, {state.rate}/s");
            
            if (converter.Inputs.Count > 0)
            {
                double ecRate = converter.Inputs.FirstOrDefault(kvp => kvp.Value.ResourceName == "ElectricCharge").Value.Ratio * converter.Rate;

                Configuration.writeDebug(ecRate.ToString());

                transferInfo.ECTransferProducer.EC = ecRate;
            }

            if (FlightGlobals.ActiveVessel != processor.Vessel)
            {
                transferInfo.Efficiency = converter.Rate;
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
            this.transferMode = module.transferMode;

            KCTransferInfo info = module.transferInfo;

            if (info == null || info.Colony == null)
            {
                Configuration.writeDebug($"ERROR: TransferInfo or Colony was null in ModuleKCTransfer {module.part.name} on vessel {module.vessel.vesselName}");
                return;
            }

            this.transferInfo = info;
            this.lastUpdateTime = module.lastUpdateTime;
            this.Priority = int.MinValue;
        }
    }
}
