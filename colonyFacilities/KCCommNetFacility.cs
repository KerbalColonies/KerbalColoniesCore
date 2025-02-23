using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities
{
    public class KCCommNetCost : KCFacilityCostClass
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

        public KCCommNetCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, float>> {
                { 0, new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500f } } },
            };
        }
    }

    public class KCCommNetFacility : KCKerbalFacilityBase
    {
        public string groundstationUUID;


        public override void OnGroupPlaced()
        {
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);

            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(baseGroupName, "Kerbin").Where(s => s.facilityType == KerbalKonstructs.Modules.KKFacilityType.GroundStation).First();
            string uuid = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).First();

            KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(uuid);

            targetInstance.facilityType = KKFacilityType.GroundStation;
            targetInstance.myFacilities.Add(targetInstance.gameObject.AddComponent<GroundStation>().ParseConfig(new ConfigNode()));

            GroundStation baseStation = baseInstance.myFacilities[0] as GroundStation;
            GroundStation targetStation = targetInstance.myFacilities[0] as GroundStation;

            targetStation.TrackingShort = baseStation.TrackingShort;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.baseGroupName = "KC_CommNet";
        }


        public KCCommNetFacility(bool enabled) : base("KCCommNetFacility", enabled, 4, 0, 0)
        {

        }
    }
}
