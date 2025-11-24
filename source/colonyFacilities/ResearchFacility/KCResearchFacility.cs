using KerbalColonies.ResourceManagment;
using Smooth.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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

namespace KerbalColonies.colonyFacilities.ResearchFacility
{
    public class KCResearchFacility : KCKerbalFacilityBase, IKCResourceConsumer
    {
        protected KCResearchFacilityWindow researchFacilityWindow;

        public KCResearchFacilityInfo researchFacilityInfo { get { return (KCResearchFacilityInfo)facilityInfo; } }

        protected double sciencePoints;

        public double MaxSciencePoints { get { return researchFacilityInfo.maxSciencePoints[level]; } }
        public double SciencePoints { get { return sciencePoints; } }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            enabled = built && kerbals.Count > 0 && !OutOfResources;
            if (enabled)
                sciencePoints = Math.Min(researchFacilityInfo.maxSciencePoints[level], sciencePoints + (researchFacilityInfo.sciencePointsPerDayperResearcher[level] / 6 / 60 / 60 * deltaTime * kerbals.Count));
        }

        public override void OnBuildingClicked()
        {
            researchFacilityWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            researchFacilityWindow.Toggle();
        }

        public override string GetFacilityProductionDisplay() => $"Science Points: {sciencePoints:f2} / {MaxSciencePoints:f2}\nDaily rate: {researchFacilityInfo.sciencePointsPerDayperResearcher[level] * kerbals.Count:f2}";

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

        public int ResourceConsumptionPriority { get; set; } = 0;
        public bool OutOfResources { get; set; } = false;

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => enabled || OutOfResources ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * kerbals.Count * deltaTime) : [];

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime) => OutOfResources = false;

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            OutOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * kerbals.Count);

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("sciencePoints", sciencePoints);
            node.AddValue("ECConsumptionPriority", ResourceConsumptionPriority);

            return node;
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            sciencePoints = double.Parse(node.GetValue("sciencePoints"));
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ResourceConsumptionPriority = ecPriority;
            researchFacilityWindow = new KCResearchFacilityWindow(this);
        }

        public KCResearchFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            sciencePoints = 0;
            researchFacilityWindow = new KCResearchFacilityWindow(this);
        }
    }
}
