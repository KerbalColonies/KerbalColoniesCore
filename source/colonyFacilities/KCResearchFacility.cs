using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

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
            researchFacility.Colony.UpdateColony();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(researchFacility, true);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Science Points: {researchFacility.SciencePoints:f2}");
            GUILayout.Label($"Max Science Points: {researchFacility.MaxSciencePoints:f2}");
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUI.enabled = facility.enabled;
            if (GUILayout.Button("Retrieve Science Points"))
                researchFacility.RetrieveSciencePoints();

            if (facility.facilityInfo.ECperSecond[facility.level] > 0)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"EC Consumption Priority: {researchFacility.ECConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) researchFacility.ECConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) researchFacility.ECConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
            }
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


    public class KCResearchFacility : KCKerbalFacilityBase, KCECConsumer
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
            enabled = built && kerbals.Count > 0 && !outOfEC;
            if (enabled)
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

        public override string GetFacilityProductionDisplay() => $"Science Points: {sciencePoints:f2} / {MaxSciencePoints:f2}\nDaily rate: {(researchFacilityInfo.sciencePointsPerDayperResearcher[level] * kerbals.Count):f2}{(facilityInfo.ECperSecond[level] > 0 ? $"\n{facilityInfo.ECperSecond[level]:f2} EC/s" : "")}";

        public bool RetrieveSciencePoints()
        {
            if (sciencePoints > 0 && enabled)
            {
                if (ResearchAndDevelopment.Instance == null)
                {
                    sciencePoints = 0;
                    return false;
                }
                ResearchAndDevelopment.Instance.AddScience((float)sciencePoints, TransactionReasons.Cheating);
                sciencePoints = 0;
                return true;
            }
            return false;
        }

        public int ECConsumptionPriority { get; set; } = 0;
        public bool outOfEC { get; set; } = false;
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) =>
            (kerbals.Count is int count && count > 0) ? facilityInfo.ECperSecond[level] * deltaTime * count : 0;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => outOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => outOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("sciencePoints", sciencePoints);
            node.AddValue("ECConsumptionPriority", ECConsumptionPriority);

            return node;
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            sciencePoints = double.Parse(node.GetValue("sciencePoints"));
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ECConsumptionPriority = ecPriority;
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            sciencePoints = 0;
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
        }
    }
}
