using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities
{
    public class KCBuildingProductionFacilityCost : KCFacilityCostClass
    {
        public override bool VesselHasRessources(Vessel vessel, int level)
        {
            for (int i = 0; i < resourceCost[level].Count; i++)
            {
                vessel.GetConnectedResourceTotals(resourceCost[level].ElementAt(i).Key.id, false, out double amount, out double maxAmount);
                if (amount < resourceCost[level].ElementAt(i).Value)
                {
                    return false;
                }
            }
            return true;
        }
        public override bool RemoveVesselRessources(Vessel vessel, int level)
        {
            if (VesselHasRessources(vessel, 0))
            {
                for (int i = 0; i < resourceCost[level].Count; i++)
                {
                    vessel.RequestResource(vessel.rootPart, resourceCost[level].ElementAt(i).Key.id, resourceCost[level].ElementAt(i).Value, true);
                }
                return true;
            }
            return false;
        }
        public KCBuildingProductionFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, float>> {
                { 0, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500f } } },
                { 1, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1000f } } },
                {2, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1500f } } },
            };
        }
    }

    /// <summary>
    /// TODO: Finish this class
    /// </summary>
    [System.Serializable]
    public class KCBuildingProductionFacility : KCKerbalFacilityBase
    {
        public double dailyProduction()
        {
            double production = 0;

            foreach (ProtoCrewMember pcm in kerbals.Keys)
            {
                production += (100 + 5 * (pcm.experienceLevel - 1)) * (1 + 0.05 * this.level);
            }
            return 100;
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            baseGroupName = "KC_CAB";
            upgradeType = UpgradeType.withAdditionalGroup;
        }

        public KCBuildingProductionFacility(bool enabled, string facilityData = "") : base("KCBuildingProductionFacility", enabled, 4, facilityData, 0, 2)
        {

        }
    }
}
