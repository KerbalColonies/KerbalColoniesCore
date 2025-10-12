using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage;
using KerbalColonies.colonyFacilities.StorageFacility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.VesselAutoTransfer
{
    public class ModuleKCTransfer : PartModule
    {
        public ResourceBroker broker = new ResourceBroker();

        [KSPField(isPersistant = true)]
        public ResourceFlowMode transferMode = ResourceFlowMode.ALL_VESSEL_BALANCE;

        public KCColonyTransferBehaviour.KCTransferInfo transferInfo;

        [KSPField(isPersistant = true)]
        public double lastUpdateTime = 0;

        private VesselColonyTransferchangewindow ColonyChangeWindow;
        private VesselResourceRatesChangewindow ResourceRatesChangeWindow;

        public void AddToColonyResource(PartResourceDefinition resource, double ratePerSecond, double colonyTransferLimits, double vesselTransferLimits)
        {
            transferInfo.ToColonyResourcesTarget[resource] = ratePerSecond;
            transferInfo.ToColonyResourcesTarget[resource] = 0;

            transferInfo.ColonyTransferLimits[resource] = colonyTransferLimits;
            transferInfo.VesselTransferLimits[resource] = vesselTransferLimits;
        }

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
            lastUpdateTime = currentTime;

            if (vessel.srfSpeed > 0.5 || !vessel.LandedOrSplashed)
            {
                transferInfo.Delete();
                transferInfo = null;
                return;
            }
            if (transferInfo == null)
            {
                return;
            }

            Dictionary<PartResourceDefinition, double> ColonyResources = new Dictionary<PartResourceDefinition, double>();
            Dictionary<PartResourceDefinition, double> VesselResources = new Dictionary<PartResourceDefinition, double>();
            
            PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            colonyClass colony = transferInfo.Colony;
            List<KCECStorageFacility> ecStorages = KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony);

            ColonyResources.Add(EC, ecStorages.Sum(f => f.ECStored));
            this.part.GetConnectedResourceTotals(EC.id, transferMode, out double vesselAmount, out double vesselMaxAmount);
            VesselResources.Add(EC, vesselAmount);

            foreach (PartResourceDefinition res in transferInfo.resources)
            {
                if (res != EC) continue;

                double transferAmount = 0;

                if (transferInfo.ToColonyResourcesTarget[res] > 0) transferAmount = transferInfo.ToColonyResourcesActual[res];
                if (transferInfo.ToVesselResourcesTarget[res] > 0) transferAmount = -transferInfo.ToVesselResourcesActual[res];

                transferAmount *= deltaTime;

                double amount = broker.RequestResource(this.part, res.id, transferAmount, deltaTime, transferMode);

                Configuration.writeDebug($"KC Transfer: broker return: {amount} of {transferAmount} requested to colony {colony.Name}");
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

            transferInfo = KCColonyTransferBehaviour.KCTransferInfo.Load(node);
        }

        public ModuleKCTransfer()
        {
            ColonyChangeWindow = new VesselColonyTransferchangewindow(this);
            ResourceRatesChangeWindow = new VesselResourceRatesChangewindow(this);
        }
    }
}
