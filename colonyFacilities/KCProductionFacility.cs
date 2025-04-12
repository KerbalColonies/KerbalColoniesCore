using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    public class KCProductionWindow : KCWindowBase
    {
        KCProductionFacility facility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            facility.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(facility, true);
            }

            GUILayout.BeginVertical();
            GUILayout.Label($"Daily production: {Math.Round(facility.dailyProduction(), 2)}");

            kerbalGUI.StaffingInterface();
            GUILayout.EndVertical();
        }

        public KCProductionWindow(KCProductionFacility facility) : base(Configuration.createWindowID(facility), "Production Facility")
        {
            this.facility = facility;
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 400, 800);

        }
    }

    public class KCProductionFacility : KCKerbalFacilityBase
    {
        KCProductionWindow prdWindow;

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

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            prdWindow = new KCProductionWindow(this);
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            maxKerbalsPerLevel = new List<int> { 8, 12, 16 };
            prdWindow = new KCProductionWindow(this);
        }
    }
}
