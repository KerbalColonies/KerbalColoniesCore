using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (C) 2024 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities
{
    internal class KCResearchFacilityWindow : KCWindowBase
    {
        KCResearchFacility researchFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            researchFacility.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(researchFacility, true);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Science Points: " + researchFacility.SciencePoints);
            GUILayout.Label("Max Science Points: " + researchFacility.MaxSciencePoints);
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve Science Points"))
            {
                researchFacility.RetrieveSciencePoints();
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            kerbalGUI.ksg.Close();
            kerbalGUI.transferWindow = false;
        }

        public KCResearchFacilityWindow(KCResearchFacility researchFacility) : base(Configuration.createWindowID(researchFacility), "Researchfacility")
        {
            this.researchFacility = researchFacility;
            toolRect = new Rect(100, 100, 400, 800);
            this.kerbalGUI = null;
        }
    }


    internal class KCResearchFacility : KCKerbalFacilityBase
    {
        private KCResearchFacilityWindow researchFacilityWindow;

        private float sciencePoints;

        public float MaxSciencePoints { get { return maxSciencePointList[level]; } }
        public float SciencePoints { get { return sciencePoints; } }

        private List<float> maxSciencePointList = new List<float> { 50, 100, 200, 400 };
        private List<float> researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
        private List<int> maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Scientist").ToList();
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            sciencePoints = Math.Min(maxSciencePointList[level], sciencePoints + (float)((researchpointsPerDayperResearcher[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
        }

        public override void OnBuildingClicked()
        {
            if (researchFacilityWindow.IsOpen())
            {
                researchFacilityWindow.Close();
                researchFacilityWindow.kerbalGUI.ksg.Close();
                researchFacilityWindow.kerbalGUI.transferWindow = false;
            }
            else
            {
                researchFacilityWindow.Open();
            }
        }

        public bool RetrieveSciencePoints()
        {
            if (sciencePoints > 0)
            {
                if (ResearchAndDevelopment.Instance == null)
                {
                    sciencePoints = 0;
                    return false;
                }
                ResearchAndDevelopment.Instance.AddScience(sciencePoints, TransactionReasons.Cheating);
                sciencePoints = 0;
                return true;
            }
            return false;
        }

        public override bool UpgradeFacility(int level)
        {
            base.UpgradeFacility(level);
            maxKerbals = maxKerbalsPerLevel[level];
            return true;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("sciencePoints", sciencePoints);

            return node;
        }

        public override string GetBaseGroupName(int level)
        {
            return "KC_CAB";
        }

        public KCResearchFacility(colonyClass colony, ConfigNode facilityConfig, ConfigNode node) : base(colony, facilityConfig, node)
        {
            sciencePoints = float.Parse(node.GetValue("sciencePoints"));
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);

            maxSciencePointList = new List<float> { 50, 100, 200, 400 };
            researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
            maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

            this.maxKerbals = maxKerbalsPerLevel[level];
            this.upgradeType = UpgradeType.withoutGroupChange;
        }

        public KCResearchFacility(colonyClass colony, ConfigNode facilityConfig, bool enabled) : base(colony, facilityConfig, enabled, 8, 0, 3)
        {
            sciencePoints = 0;

            this.researchFacilityWindow = new KCResearchFacilityWindow(this);

            maxSciencePointList = new List<float> { 50, 100, 200, 400 };
            researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
            maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

            this.maxKerbals = maxKerbalsPerLevel[level];
            this.upgradeType = UpgradeType.withoutGroupChange;
        }
    }
}
