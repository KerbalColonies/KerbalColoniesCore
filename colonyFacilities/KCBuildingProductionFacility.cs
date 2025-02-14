using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public class KCBuildingProductionWindow : KCWindowBase
    {
        KCBuildingProductionFacility facility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            facility.Update();

            if (kerbalGUI == null)
            {
                KCFacilityBase.GetInformationByFacilty(facility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
                kerbalGUI = new KerbalGUI(facility, saveGame, bodyIndex, colonyName);
            }

            GUILayout.BeginVertical();
            GUILayout.Label($"Daily production: {Math.Round(facility.dailyProduction(), 2)}");

            kerbalGUI.StaffingInterface();
            GUILayout.EndVertical();
        }

        public KCBuildingProductionWindow(KCBuildingProductionFacility facility) : base(Configuration.createWindowID(facility), "Production Facility")
        {
            this.facility = facility;
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 400, 800);

        }
    }

    [System.Serializable]
    public class KCBuildingProductionFacility : KCKerbalFacilityBase
    {
        KCBuildingProductionWindow prdWindow;

        private List<int> maxKerbalsPerLevel = new List<int> { 8, 12, 16 };

        public double dailyProduction()
        {
            double production = 0;

            foreach (ProtoCrewMember pcm in kerbals.Keys)
            {
                production += (100 + 5 * (pcm.experienceLevel - 1)) * (1 + 0.05 * this.level);
            }
            return production;
        }

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Engineer").ToList();
        }

        public override int GetUpgradeTime(int level)
        {
            // 1 Kerbin day = 0.25 days
            // 100 per day * 5 engineers = 500 per day
            // 500 per day * 4 kerbin days = 500

            // 1 Kerbin day = 0.25 days
            // 100 per day * 5 engineers = 500 per day
            // 500 per day * 2 kerbin days = 250
            int[] buildTimes = { 500, 250, 250 };
            return buildTimes[level];
        }

        public override void OnBuildingClicked()
        {
            prdWindow.Toggle();
        }

        public override void UpdateBaseGroupName()
        {
            baseGroupName = "KC_CAB";
        }

        public override void Initialize()
        {
            base.Initialize();
            baseGroupName = "KC_CAB";
            upgradeType = UpgradeType.withGroupChange;
            maxKerbalsPerLevel = new List<int> { 8, 12, 16 };
            prdWindow = new KCBuildingProductionWindow(this);
        }

        public KCBuildingProductionFacility(bool enabled) : base("KCBuildingProductionFacility", enabled, 4, 0, 2)
        {

        }
    }
}
