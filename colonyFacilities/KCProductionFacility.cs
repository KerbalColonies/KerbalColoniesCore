using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalColonies.UI;

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

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCProductionWindow(KCProductionFacility facility) : base(Configuration.createWindowID(), "Production Facility")
        {
            this.facility = facility;
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 400, 600);

        }
    }

    public class KCProductionFacility : KCKerbalFacilityBase
    {
        KCProductionWindow prdWindow;

        public List<float> baseProduction { get; private set; } = new List<float> { };
        public List<float> experienceMultiplier { get; private set; } = new List<float> { };
        public List<float> facilityLevelMultiplier { get; private set; } = new List<float> { };

        public double dailyProduction()
        {
            double production = 0;

            foreach (ProtoCrewMember pcm in kerbals.Keys)
            {
                production += (baseProduction[level] + experienceMultiplier[level] * (pcm.experienceLevel - 1)) * (1 + facilityLevelMultiplier[level] * this.level);
            }
            return production;
        }

        public override void OnBuildingClicked()
        {
            prdWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            prdWindow.Toggle();
        }

        private void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());
                if (iLevel.HasValue("baseProduction")) baseProduction.Add(float.Parse(iLevel.GetValue("baseProduction")));
                else if (i > 0) baseProduction.Add(baseProduction[i - 1]);
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no baseProduction (at least for level 0).");

                if (iLevel.HasValue("experienceMultiplier")) experienceMultiplier.Add(float.Parse(iLevel.GetValue("experienceMultiplier")));
                else if (i > 0) experienceMultiplier.Add(experienceMultiplier[i - 1]);
                else experienceMultiplier.Add(1);

                if (iLevel.HasValue("facilityLevelMultiplier")) facilityLevelMultiplier.Add(float.Parse(iLevel.GetValue("facilityLevelMultiplier")));
                else if (i > 0) facilityLevelMultiplier.Add(facilityLevelMultiplier[i - 1]);
                else facilityLevelMultiplier.Add(1);
            }
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            prdWindow = new KCProductionWindow(this);
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            prdWindow = new KCProductionWindow(this);
        }
    }
}
