using KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.SolarPanel;
using KerbalColonies.ResourceManagment;
using KerbalColonies.Settings;
using KerbalColonies.SunMath;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Solarpanel
{
    public class KCSolarpanelFacility : KCFacilityBase, IKCResourceProducer, IKCResourceConsumer
    {
        public KCSolarpanelInfo solarpanelInfo => (KCSolarpanelInfo)facilityInfo;

        public int ResourceConsumptionPriority { get; set; } = 0;

        private double cachedReceivedRadiance = 0.0; // relative to home-body radiance

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();

            // Solar panels are always on when built.
            enabled = built;
        }

        private void UpdateReceivedRadianceCache()
        {
            CelestialBody body = FlightGlobals.GetBodyByName(Colony.BodyName);

            if (solarpanelInfo.SunTracking)
            {
                cachedReceivedRadiance = RadianceSolver.GetReceivedRadiance(body);

            }
            else if (KKgroups != null && KKgroups.Count > 0)
            {
                KerbalKonstructs.Core.GroupCenter group = KerbalKonstructs.API.GetGroupCenter(KKgroups.First(), Colony.BodyName);

                cachedReceivedRadiance = RadianceSolver.ReceivedRadianceAngle(
                    body,
                    group.RefLatitude,
                    group.RefLongitude,
                    solarpanelInfo.PanelAngle,
                    group.heading
                );
            }
        }

        private double ProductionScale()
        {
            if (!built) return 0.0;
            UpdateReceivedRadianceCache(); // Update the cache each production cycle to account for orbital changes

            return Math.Min(solarpanelInfo.MaxEfficiency, cachedReceivedRadiance);
        }

        public Dictionary<PartResourceDefinition, double> ResourceProduction(double lastTime, double deltaTime, double currentTime)
        {
            if (!enabled) return [];

            double scale = ProductionScale();
            if (scale <= 0.0) return [];

            return facilityInfo.ResourceUsage[level]
                .Where(kvp => kvp.Value > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * scale * deltaTime);
        }

        public Dictionary<PartResourceDefinition, double> ResourcesPerSecond()
        {
            if (!enabled) return [];

            double scale = ProductionScale();
            if (scale <= 0.0) return [];

            return facilityInfo.ResourceUsage[level]
                .Where(kvp => kvp.Value > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value * scale);
        }

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime)
        {
            if (!enabled) return [];

            double scale = ProductionScale();
            if (scale <= 0.0) return [];

            return facilityInfo.ResourceUsage[level]
                .Where(kvp => kvp.Value < 0)
                .ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * scale * deltaTime);
        }

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime)
        {
            // Intentionally empty: facility remains enabled while built.
        }

        public Dictionary<PartResourceDefinition, double> InsufficientResources(
            double lastTime,
            double deltaTime,
            double currentTime,
            Dictionary<PartResourceDefinition, double> sufficientResources,
            Dictionary<PartResourceDefinition, double> limitingResources)
        {
            // Return all already-approved resources as unused, preserving resource manager behavior.
            foreach (KeyValuePair<PartResourceDefinition, double> kvp in sufficientResources)
            {
                if (limitingResources.ContainsKey(kvp.Key)) limitingResources[kvp.Key] += kvp.Value;
                else limitingResources.Add(kvp.Key, kvp.Value);
            }

            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond()
        {
            if (!enabled) return [];

            double scale = ProductionScale();
            if (scale <= 0.0) return [];

            return facilityInfo.ResourceUsage[level]
                .Where(kvp => kvp.Value < 0)
                .ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * scale);
        }

        public override string GetFacilityProductionDisplay()
        {
            double radiance = built ? cachedReceivedRadiance : 0.0;
            string resources = string.Join(", ", ResourcesPerSecond().Select(kvp => $"{kvp.Key.displayName}: {kvp.Value:f2}/s"));
            return $"Solar panel radiance: {radiance:f3}; production: {resources}";
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("ECConsumptionPriority", ResourceConsumptionPriority);
            return node;
        }

        public KCSolarpanelFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ResourceConsumptionPriority = ecPriority;
        }

        public KCSolarpanelFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
        }
    }
}
