using KerbalColonies.ResourceManagment;
using Smooth.Collections;
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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.FuelCell
{
    public class KCFuelCellFacility : KCFacilityBase, IKCResourceProducer, IKCResourceConsumer
    {
        public KCFuelCellInfo fuelCellInfo => (KCFuelCellInfo)facilityInfo;
        public float Throttle { get; set; } = 1.0f;

        public int ResourceConsumptionPriority { get; set; } = 0;

        public bool CanProduce { get; protected set; }

        protected KCFuelCellWindow window;

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }
        public override void OnRemoteClicked()
        {
            window.Toggle();
        }

        public override string GetFacilityProductionDisplay() => $"Fuel cell production rate: {string.Join(", ", ResourcesPerSecond().Select(kvp => $"{kvp.Key.displayName}: {kvp.Value * Throttle:f2}"))}";

        public Dictionary<PartResourceDefinition, double> ResourceProduction(double lastTime, double deltaTime, double currentTime) => CanProduce && enabled ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value * deltaTime * Throttle) : [];

        public Dictionary<PartResourceDefinition, double> ResourcesPerSecond() => CanProduce && enabled ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value * Throttle) : [];
        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => enabled ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime * Throttle) : [];
        public void ConsumeResources(double lastTime, double deltaTime, double currentTime) => CanProduce = true;

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            CanProduce = false;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * Throttle);
        
        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("Throttle", Throttle);
            node.AddValue("ECConsumptionPriority", ResourceConsumptionPriority);
            return node;
        }

        public KCFuelCellFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (float.TryParse(node.GetValue("Throttle"), out float throttle)) Throttle = throttle;
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ResourceConsumptionPriority = ecPriority;

            window = new KCFuelCellWindow(this);
        }

        public KCFuelCellFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCFuelCellWindow(this);
        }
    }
}
