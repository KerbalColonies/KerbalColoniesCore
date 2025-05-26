using KerbalColonies.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static KSP.UI.Screens.SpaceCenter.BuildingPicker;

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
    public class KCResearchFacilityInfoClass : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, double> maxSciencePoints = new SortedDictionary<int, double>();
        public SortedDictionary<int, double> sciencePointsPerDayperResearcher = new SortedDictionary<int, double>();

        public KCResearchFacilityInfoClass(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                if (n.Value.HasValue("scienceRate")) sciencePointsPerDayperResearcher[n.Key] = double.Parse(n.Value.GetValue("scienceRate"));
                else if (n.Key > 0) sciencePointsPerDayperResearcher[n.Key] = sciencePointsPerDayperResearcher[n.Key - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no scienceRate (at least for level 0).");

                if (n.Value.HasValue("maxScience")) maxSciencePoints[n.Key] = double.Parse(n.Value.GetValue("maxScience"));
                else if (n.Key > 0) maxSciencePoints[n.Key] = maxSciencePoints[n.Key - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no maxScience (at least for level 0).");
            });
        }
    }

    public class KCResearchFacilityWindow : KCFacilityWindowBase
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

        public KCResearchFacilityWindow(KCResearchFacility researchFacility) : base(researchFacility, Configuration.createWindowID())
        {
            this.researchFacility = researchFacility;
            toolRect = new Rect(100, 100, 400, 800);
            this.kerbalGUI = null;
        }
    }


    public class KCResearchFacility : KCKerbalFacilityBase
    {
        protected KCResearchFacilityWindow researchFacilityWindow;

        public KCResearchFacilityInfoClass researchFacilityInfo { get { return (KCResearchFacilityInfoClass)facilityInfo; } }

        protected double sciencePoints;

        public double MaxSciencePoints { get { return researchFacilityInfo.maxSciencePoints[level]; } }
        public double SciencePoints { get { return sciencePoints; } }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            sciencePoints = Math.Min(researchFacilityInfo.maxSciencePoints[level], sciencePoints + ((researchFacilityInfo.sciencePointsPerDayperResearcher[level] / 6 / 60 / 60) * deltaTime) * kerbals.Count);
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
                ResearchAndDevelopment.Instance.AddScience((float) sciencePoints, TransactionReasons.Cheating);
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

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            sciencePoints = double.Parse(node.GetValue("sciencePoints"));
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            sciencePoints = 0;
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
        }
    }
}
