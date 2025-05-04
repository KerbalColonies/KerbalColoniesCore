using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

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
            GUILayout.Label($"Science Points: {researchFacility.SciencePoints:f2}");
            GUILayout.Label($"Max Science Points: {researchFacility.MaxSciencePoints:f2}");
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
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCResearchFacilityWindow(KCResearchFacility researchFacility) : base(Configuration.createWindowID(), "Researchfacility")
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

        private List<float> maxSciencePointList = new List<float> { };
        private List<float> researchpointsPerDayperResearcher = new List<float> { };
        private List<int> maxKerbalsPerLevel = new List<int> { };

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            sciencePoints = Math.Min(maxSciencePointList[level], sciencePoints + (float)((researchpointsPerDayperResearcher[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
        }

        public override void OnBuildingClicked()
        {
            researchFacilityWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            researchFacilityWindow.Toggle();
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

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("sciencePoints", sciencePoints);

            return node;
        }

        public void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = node.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());

                if (iLevel.HasValue("scienceRate")) researchpointsPerDayperResearcher[i] = float.Parse(iLevel.GetValue("scienceRate"));
                else if (i > 0) researchpointsPerDayperResearcher[i] = researchpointsPerDayperResearcher[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no scienceRate (at least for level 0).");

                if (iLevel.HasValue("maxScience")) maxSciencePointList[i] = float.Parse(iLevel.GetValue("maxScience"));
                else if (i > 0) maxSciencePointList[i] = maxSciencePointList[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxScience (at least for level 0).");
            }
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            sciencePoints = float.Parse(node.GetValue("sciencePoints"));
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            sciencePoints = 0;
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);

        }
    }
}
