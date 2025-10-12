using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.VesselAutoTransfer
{

    public class KCTransferBehaviour: ConverterBehaviour
    {
        [KSPField(isPersistant = true)]
        public ResourceFlowMode transferMode = ResourceFlowMode.ALL_VESSEL_BALANCE;

        [KSPField(isPersistant = true)]
        public int TargetColonyID = -1;
        // Constant rates per second
        public Dictionary<PartResourceDefinition, double> ToColonyResources = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> ToVesselResources = new Dictionary<PartResourceDefinition, double>();

        public Dictionary<PartResourceDefinition, double> ColonyTransferLimits = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> VesselTransferLimits = new Dictionary<PartResourceDefinition, double>();

        [KSPField(isPersistant = true)]
        private double lastUpdateTime = 0;

        public override ConverterResources GetResources(VesselState state)
        {
            ConverterResources resources = new ConverterResources();

            resources.Inputs = new List<ResourceRatio>();

            ToColonyResources.ToList().ForEach(kvp => resources.Inputs.Add(new ResourceRatio(kvp.Key.name, kvp.Value, false, transferMode)));

            return resources;
        }

        public KCTransferBehaviour()
        {

        }

        public KCTransferBehaviour(ModuleKCTransfer module)
        {
            this.transferMode = module.transferMode;

            KCColonyTransferBehaviour.KCTransferInfo info = module.transferInfo;

            if (info == null || info.Colony == null)
            {
                Configuration.writeDebug($"ERROR: TransferInfo or Colony was null in ModuleKCTransfer {module.part.name} on vessel {module.vessel.vesselName}");
                return;
            }

            this.TargetColonyID = info.Colony.uniqueID;
            this.ToColonyResources = info.ToColonyResourcesTarget;
            this.ToVesselResources = info.ToVesselResourcesTarget;
            this.ColonyTransferLimits = info.ColonyTransferLimits;
            this.VesselTransferLimits = info.VesselTransferLimits;
            this.lastUpdateTime = module.lastUpdateTime;
        }
    }
}
