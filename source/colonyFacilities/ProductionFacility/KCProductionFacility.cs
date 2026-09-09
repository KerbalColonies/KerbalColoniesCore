using KerbalColonies.colonyFacilities.CabFacility;
using KerbalColonies.colonyFacilities.HangarFacility;
using KerbalColonies.ResourceManagment;
using KerbalColonies.Settings;
using KSP.Localization;
using Smooth.Collections;
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

namespace KerbalColonies.colonyFacilities.ProductionFacility
{
    public class KCProductionFacility : KCKerbalFacilityBase, IKCResourceConsumer
    {
        public static Dictionary<colonyClass, List<KCProductionQueueItem>> ProductionQueues { get; protected set; } = [];
        public static Dictionary<colonyClass, List<KCFacilityBase>> ConstructedFacilities { get; protected set; } = [];
        public static Dictionary<colonyClass, List<KCFacilityBase>> UpgradedFacilities { get; protected set; } = [];

        public static void AddConstructedFacility(KCFacilityBase facility)
        {
            ConstructedFacilities.TryAdd(facility.Colony, []);
            ConstructedFacilities[facility.Colony].Add(facility);
        }

        public static void AddUpgradedFacility(KCFacilityBase facility)
        {
            UpgradedFacilities.TryAdd(facility.Colony, []);
            UpgradedFacilities[facility.Colony].Add(facility);
        }

        public static List<KCProductionQueueItem> GetQueue(colonyClass colony)
        {
            ProductionQueues.TryAdd(colony, []);
            return ProductionQueues[colony];
        }

        public static void Enqueue(colonyClass colony, KCProductionQueueItem item)
        {
            item.RecalculateCosts(colony);
            GetQueue(colony).Add(item);
        }

        public static void RecalculateAllCosts()
        {
            foreach (KeyValuePair<colonyClass, List<KCProductionQueueItem>> queue in ProductionQueues)
            {
                queue.Value.ForEach(item => item.RecalculateCosts(queue.Key));
            }
        }

        public static bool MoveQueueItem(colonyClass colony, KCProductionQueueItem item, int direction)
        {
            List<KCProductionQueueItem> queue = GetQueue(colony);
            int index = queue.IndexOf(item);
            int targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || targetIndex >= queue.Count) return false;
            queue.RemoveAt(index);
            queue.Insert(targetIndex, item);
            return true;
        }

        private static void LoadQueue(colonyClass colony, ConfigNode production)
        {
            if (production.HasNode("queue"))
            {
                foreach (ConfigNode node in production.GetNode("queue").GetNodes("queueItem"))
                {
                    KCProductionQueueItem item = node.GetValue("variant") == KCProductionQueueItem.VesselVariant
                        ? new KCVesselProductionQueueItem(node)
                        : new KCFacilityProductionQueueItem(node);
                    if (item.IsAvailable(colony)) Enqueue(colony, item);
                }
                return;
            }

            foreach (StoredVessel vessel in KCHangarFacility.GetConstructingVessels(colony))
            {
                KCHangarFacility hangar = KCHangarFacility.GetHangarsInColony(colony).FirstOrDefault(h => h.storedVessels.Contains(vessel));
                ConfigNode recipeNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
                if (hangar == null || recipeNode == null) continue;
                double totalTime = vessel.entireVesselBuildTime ?? vessel.vesselBuildTime ?? 0;
                double progress = totalTime == 0 ? 0 : 1 - ((vessel.vesselBuildTime ?? totalTime) / totalTime);
                Enqueue(colony, new KCVesselProductionQueueItem(hangar, vessel, vessel.vesselDryMass ?? 0, vessel.vesselNode.GetNodes("PART").Length, progress));
            }

            foreach (ConfigNode facilityNode in production.GetNode("upgradingFacilities")?.GetNodes("facilityNode") ?? [])
            {
                KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                if (facility == null) continue;
                double totalTime = facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier;
                double remainingTime = double.Parse(facilityNode.GetValue("remainingTime"));
                Enqueue(colony, new KCFacilityProductionQueueItem(facility, facility.level + 1, true, totalTime == 0 ? 0 : 1 - remainingTime / totalTime, true));
            }
            foreach (ConfigNode facilityNode in production.GetNode("constructingFacilities")?.GetNodes("facilityNode") ?? [])
            {
                KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                if (facility == null) continue;
                double totalTime = facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier;
                double remainingTime = double.Parse(facilityNode.GetValue("remainingTime"));
                Enqueue(colony, new KCFacilityProductionQueueItem(facility, 0, false, totalTime == 0 ? 0 : 1 - remainingTime / totalTime, true));
            }
        }

        public static bool CancelQueueItem(colonyClass colony, KCProductionQueueItem item)
        {
            if (!GetQueue(colony).Remove(item)) return false;
            item.Cancel(colony);
            return true;
        }

        public bool FacilityQueue => GetQueue(Colony).OfType<KCFacilityProductionQueueItem>().Any();
        public bool VesselQueue => GetQueue(Colony).OfType<KCVesselProductionQueueItem>().Any();

        private static double getDeltaTime(colonyClass colony)
        {
            ConfigNode timeNode = colony.sharedColonyNodes.FirstOrDefault(node => node.name == "KCProductionFacilityTime");
            if (timeNode == null)
            {
                ConfigNode node = new("KCProductionFacilityTime");
                node.AddValue("lastTime", Planetarium.GetUniversalTime().ToString());
                colony.sharedColonyNodes.Add(node);
                return 0;
            }
            double lastTime = double.Parse(timeNode.GetValue("lastTime"));
            double deltaTime = Planetarium.GetUniversalTime() - lastTime;
            timeNode.SetValue("lastTime", Planetarium.GetUniversalTime().ToString());
            return deltaTime;
        }

        public static void ExecuteProduction(colonyClass colony)
        {
            ProductionQueues.TryAdd(colony, []);
            if (ConstructedFacilities.TryAdd(colony, []) | UpgradedFacilities.TryAdd(colony, []))
            {
                ConfigNode production = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "production");
                if (production != null)
                {
                    if (!production.HasNode("constructedFacilities")) production.AddNode(new ConfigNode("constructedFacilities"));
                    if (!production.HasNode("upgradedFacilities")) production.AddNode(new ConfigNode("upgradedFacilities"));

                    foreach (ConfigNode facilityNode in production.GetNode("constructedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) AddConstructedFacility(facility);
                    }
                    foreach (ConfigNode facilityNode in production.GetNode("upgradedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) AddUpgradedFacility(facility);
                    }
                    LoadQueue(colony, production);
                }
            }


            double dt = getDeltaTime(colony);
            if (dt == 0)
            {
                Configuration.writeDebug($"ExecuteProduction early return dt=0 for colony={colony.DisplayName}");
                return;
            }

            foreach (KCProductionFacility producer in colony.Facilities.OfType<KCProductionFacility>())
            {
                if (!producer.enabled || producer.OutOfResources) continue;

                double availableProduction = producer.dailyProduction() * dt / 6 / 60 / 60;
                while (availableProduction > 0)
                {
                    KCProductionQueueItem item = GetQueue(colony).FirstOrDefault(queueItem =>
                    {
                        if (!queueItem.IsAvailable(colony) || !queueItem.Matches(producer)) return false;
                        double buildTime = queueItem.GetBuildTime(colony);
                        return buildTime > 0 && queueItem.GetAffordableProgress(colony, queueItem.RemainingProgress) > 0;
                    });
                    if (item == null) break;

                    double requestedProgress = Math.Min(item.RemainingProgress, availableProduction / item.GetBuildTime(colony));
                    double appliedProgress = item.GetAffordableProgress(colony, requestedProgress);
                    if (!item.ApplyProgress(colony, appliedProgress)) continue;
                    availableProduction -= appliedProgress * item.GetBuildTime(colony);

                    if (item.IsComplete)
                    {
                        item.Complete(colony);
                        GetQueue(colony).Remove(item);
                    }
                }
            }

            return;
        }

        public static void DailyProductions(colonyClass colony, out double dailyProduction, out double dailyVesselProduction)
        {
            dailyProduction = 0;
            dailyVesselProduction = 0;

            ConfigNode vesselBuildInfoNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
            KCProductionInfo info = null;
            int level = 0;

            if (vesselBuildInfoNode != null)
            {
                info = (KCProductionInfo)Configuration.GetInfoClass(vesselBuildInfoNode.GetValue("facilityConfig"));
                level = int.Parse(vesselBuildInfoNode.GetValue("facilityLevel"));
            }

            foreach (KCProductionFacility f in colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f))
            {
                if (info != null && info.HasSameRecipe(level, f)) dailyVesselProduction += f.dailyProduction();
                else dailyProduction += f.dailyProduction();
            }
        }

        public static void CABDisplay(colonyClass colony)
        {
            GUILayout.Space(10);
            GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth), GUILayout.Height(80));
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_TITLE"));
                DailyProductions(colony, out double dailyProduction, out double dailyVesselProduction);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width((KC_CAB_Window.CABInfoWidth / 2) - 10));
                    {
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_DAILY", dailyProduction.ToString("f2")));
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_BUILDING_FACILITIES", GetQueue(colony).OfType<KCFacilityProductionQueueItem>().Count()));
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_BUILT_FACILITIES", ConstructedFacilities[colony].Count + UpgradedFacilities[colony].Count));
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width((KC_CAB_Window.CABInfoWidth / 2) - 10));
                    {
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_DAILY_VESSEL", dailyVesselProduction.ToString("f2")));
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_BUILDING_VESSELS", GetQueue(colony).OfType<KCVesselProductionQueueItem>().Count()));
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        protected KCProductionWindow prdWindow;
        public KCProductionInfo KCProductionInfo => (KCProductionInfo)facilityInfo;

        public bool OutOfResources { get; protected set; } = false;
        public double lastProduction { get; protected set; } = 0;

        public double dailyProduction()
        {
            if (OutOfResources || !enabled) return 0;
            double production = 0;

            KCProductionInfo info = KCProductionInfo;

            foreach (ProtoCrewMember pcm in kerbals.Keys)
            {
                production += info.baseProduction[level] + (info.experienceMultiplier[level] * (pcm.experienceLevel - 1));
            }
            production *= 1 + (info.facilityLevelMultiplier[level] * level);
            lastProduction = production;
            return production;
        }

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            enabled = !OutOfResources && built && (FacilityQueue || (VesselQueue && KCProductionInfo.CanBuildVessels(level)));
        }

        public override void OnBuildingClicked()
        {
            prdWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            prdWindow.Toggle();
        }

        public int ResourceConsumptionPriority { get; set; } = 0;


        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => lastProduction > 0 ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime) : [];

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime)
        {
            OutOfResources = false;
        }

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            OutOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => lastProduction > 0 ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value) : [];

        public override string GetFacilityProductionDisplay() => Localizer.Format("#LOC_KC_PRODUCTION_SUMMARY", kerbals.Count, dailyProduction().ToString("f2"), Localizer.Format(KCProductionInfo.CanBuildVessels(level) ? "#LOC_KC_PRODUCTION_CAN_BUILD" : "#LOC_KC_PRODUCTION_CANNOT_BUILD"));

        public override ConfigNode getConfigNode()
        {
            UpdateSharedNode(Colony);

            ConfigNode node = base.getConfigNode();
            node.AddValue("ECConsumptionPriority", ResourceConsumptionPriority);
            return node;
        }

        public void UpdateSharedNode(colonyClass colony)
        {
            ConfigNode production = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "production");
            if (production == null)
            {
                production = new ConfigNode("production");
                colony.sharedColonyNodes.Add(production);
            }
            else production.ClearNodes();

            ConfigNode queue = new("queue");
            GetQueue(colony).ForEach(item => queue.AddNode(item.GetConfigNode()));
            production.AddNode(queue);

            ConfigNode constructedFacilities = new("constructedFacilities");
            ConstructedFacilities.TryAdd(colony, []);
            ConstructedFacilities[colony].ForEach(facility =>
            {
                ConfigNode facilityNode = new("facilityNode");
                facilityNode.AddValue("facilityID", facility.id);
                constructedFacilities.AddNode(facilityNode);
            });
            production.AddNode(constructedFacilities);

            ConfigNode upgradedFacilities = new("upgradedFacilities");
            UpgradedFacilities.TryAdd(colony, []);
            UpgradedFacilities[colony].ForEach(facility =>
            {
                ConfigNode facilityNode = new("facilityNode");
                facilityNode.AddValue("facilityID", facility.id);
                upgradedFacilities.AddNode(facilityNode);
            });
            production.AddNode(upgradedFacilities);
        }

        private void configNodeLoader()
        {
            prdWindow = new KCProductionWindow(this);

            KCProductionInfo productionInfo = (KCProductionInfo)facilityInfo;
            if (productionInfo.CanBuildVessels(level))
            {
                if (!Colony.sharedColonyNodes.Any(n => n.name == "vesselBuildInfo"))
                {
                    Configuration.writeDebug($"Facility {name} is now used to build vessels.");
                    ConfigNode vesselBuildInfo = new("vesselBuildInfo");
                    vesselBuildInfo.AddValue("facilityConfig", name);
                    vesselBuildInfo.AddValue("facilityLevel", level);
                    Colony.sharedColonyNodes.Add(vesselBuildInfo);
                }
            }
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader();
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int priority)) ResourceConsumptionPriority = priority;
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader();
        }
    }
}
