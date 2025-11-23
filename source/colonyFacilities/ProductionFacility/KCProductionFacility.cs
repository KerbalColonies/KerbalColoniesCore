using KerbalColonies.colonyFacilities.CabFacility;
using KerbalColonies.ResourceManagment;
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
        public static Dictionary<colonyClass, Dictionary<KCFacilityBase, double>> ConstructingFacilities { get; protected set; } = new Dictionary<colonyClass, Dictionary<KCFacilityBase, double>>();
        public static Dictionary<colonyClass, List<KCFacilityBase>> ConstructedFacilities { get; protected set; } = new Dictionary<colonyClass, List<KCFacilityBase>>();
        public static Dictionary<colonyClass, Dictionary<KCFacilityBase, double>> UpgradingFacilities { get; protected set; } = new Dictionary<colonyClass, Dictionary<KCFacilityBase, double>>();
        public static Dictionary<colonyClass, List<KCFacilityBase>> UpgradedFacilities { get; protected set; } = new Dictionary<colonyClass, List<KCFacilityBase>>();

        public static void AddConstructingFacility(KCFacilityBase facility, double time)
        {
            ConstructingFacilities[facility.Colony].TryAdd(facility, time);
        }

        public static void AddConstructedFacility(KCFacilityBase facility)
        {
            ConstructingFacilities[facility.Colony].Remove(facility);
            ConstructedFacilities[facility.Colony].Add(facility);
        }

        public static void AddUpgradingFacility(KCFacilityBase facility, double time)
        {
            UpgradingFacilities[facility.Colony].TryAdd(facility, time);
        }

        public static void AddUpgradedFacility(KCFacilityBase facility)
        {
            UpgradingFacilities[facility.Colony].Remove(facility);
            UpgradedFacilities[facility.Colony].Add(facility);
        }

        public bool FacilityQueue => ConstructingFacilities.ContainsKey(Colony) ? ConstructingFacilities[Colony].Count > 0 || UpgradingFacilities[Colony].Count > 0 : false;
        public bool VesselQueue => KCHangarFacility.GetConstructingVessels(Colony).Count > 0;

        private static double getDeltaTime(colonyClass colony)
        {
            ConfigNode timeNode = colony.sharedColonyNodes.FirstOrDefault(node => node.name == "KCProductionFacilityTime");
            if (timeNode == null)
            {
                ConfigNode node = new ConfigNode("KCProductionFacilityTime");
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
            if (ConstructingFacilities.TryAdd(colony, new Dictionary<KCFacilityBase, double>()))
            {
                ConstructedFacilities.Add(colony, new List<KCFacilityBase>());
                UpgradingFacilities.Add(colony, new Dictionary<KCFacilityBase, double>());
                UpgradedFacilities.Add(colony, new List<KCFacilityBase>());

                ConfigNode production = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "production");
                if (production != null)
                {
                    foreach (ConfigNode facilityNode in production.GetNode("constructingFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) AddConstructingFacility(facility, double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in production.GetNode("constructedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) AddConstructedFacility(facility);
                    }
                    foreach (ConfigNode facilityNode in production.GetNode("upgradingFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) AddUpgradingFacility(facility, double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in production.GetNode("upgradedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) AddUpgradedFacility(facility);
                    }
                }
            }


            double dt = getDeltaTime(colony);
            if (dt == 0) return;

            KCProductionFacility.DailyProductions(colony, out double dailyProduction, out double dailyVesselProduction);

            dailyProduction = (((dailyProduction * dt) / 6) / 60) / 60; // convert from Kerbin days (6 hours) to seconds
            dailyVesselProduction = (((dailyVesselProduction * dt) / 6) / 60) / 60;

            List<StoredVessel> constructingVessel = KCHangarFacility.GetConstructingVessels(colony);

            if (constructingVessel.Count > 0)
            {
                while (dailyVesselProduction > 0 && constructingVessel.Count > 0)
                {
                    if (constructingVessel[0].vesselBuildTime > dailyVesselProduction)
                    {
                        double buildingVesselMass = (double)(constructingVessel[0].vesselDryMass * (dailyVesselProduction / constructingVessel[0].entireVesselBuildTime));
                        if (!KCHangarFacility.CanBuildVessel(buildingVesselMass, colony)) break;

                        KCHangarFacility.BuildVessel(buildingVesselMass, colony);
                        constructingVessel[0].vesselBuildTime -= dailyVesselProduction;
                        if (Math.Round((double)constructingVessel[0].vesselBuildTime, 2) <= 0)
                        {
                            constructingVessel[0].vesselBuildTime = null;
                            constructingVessel[0].entireVesselBuildTime = null;
                            ScreenMessages.PostScreenMessage($"KC: Vessel {constructingVessel[0].vesselName} was fully built on colony {colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                        }
                        dailyVesselProduction = 0;
                        break;
                    }
                    else
                    {
                        if (constructingVessel[0].vesselBuildTime == null) { constructingVessel.RemoveAt(0); continue; }
                        double buildingVesselMass = (double)(constructingVessel[0].vesselDryMass * (constructingVessel[0].vesselBuildTime / constructingVessel[0].entireVesselBuildTime));
                        if (!KCHangarFacility.CanBuildVessel(buildingVesselMass, colony)) break;

                        KCHangarFacility.BuildVessel(buildingVesselMass, colony);
                        dailyVesselProduction -= (double)constructingVessel[0].vesselBuildTime;
                        constructingVessel[0].vesselBuildTime = null;
                        constructingVessel[0].entireVesselBuildTime = null;
                        ScreenMessages.PostScreenMessage($"KC: Vessel {constructingVessel[0].vesselName} was fully built on colony {colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    }
                }
            }

            dailyProduction += dailyVesselProduction;

            if (UpgradingFacilities[colony].Count > 0 || ConstructingFacilities[colony].Count > 0)
            {
                while (dailyProduction > 0)
                {
                    if (UpgradingFacilities[colony].Count > 0)
                    {
                        if (UpgradingFacilities[colony].ElementAt(0).Value > dailyProduction)
                        {
                            UpgradingFacilities[colony][UpgradingFacilities[colony].ElementAt(0).Key] -= dailyProduction;
                            dailyProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = UpgradingFacilities[colony].ElementAt(0).Key;
                            dailyProduction -= UpgradingFacilities[colony].ElementAt(0).Value;
                            UpgradingFacilities[colony].Remove(facility);

                            ScreenMessages.PostScreenMessage($"KC: Facility {facility.DisplayName} was fully upgraded on colony {colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);

                            switch (facility.facilityInfo.UpgradeTypes[facility.level + 1])
                            {
                                case UpgradeType.withGroupChange:
                                    KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                                    break;
                                case UpgradeType.withoutGroupChange:
                                    KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                                    break;
                                case UpgradeType.withAdditionalGroup:
                                    KCProductionFacility.AddUpgradedFacility(facility);
                                    break;
                            }
                        }
                    }
                    else if (ConstructingFacilities[colony].Count > 0)
                    {
                        if (ConstructingFacilities[colony].First().Value > dailyProduction)
                        {
                            ConstructingFacilities[colony][ConstructingFacilities[colony].First().Key] -= dailyProduction;
                            dailyProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = ConstructingFacilities[colony].ElementAt(0).Key;
                            dailyProduction -= ConstructingFacilities[colony].ElementAt(0).Value;
                            ConstructingFacilities[colony].Remove(facility);
                            KCProductionFacility.AddConstructedFacility(facility);
                            ScreenMessages.PostScreenMessage($"KC: Facility {facility.DisplayName} was fully built on colony {colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
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
                GUILayout.Label($"<b>Production:</b>");
                DailyProductions(colony, out double dailyProduction, out double dailyVesselProduction);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth / 2 - 10));
                    {
                        GUILayout.Label($"Daily production: {dailyProduction:f2}");
                        GUILayout.Label($"Facilities building/upgrading: {ConstructingFacilities[colony].Count + UpgradingFacilities[colony].Count}");
                        GUILayout.Label($"Facilities built/upgraded: {ConstructedFacilities[colony].Count + UpgradedFacilities[colony].Count}");
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth / 2 - 10));
                    {
                        GUILayout.Label($"Daily vessel production: {dailyVesselProduction:f2}");
                        GUILayout.Label($"Vessels building: {KCHangarFacility.GetConstructingVessels(colony).Count}");
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
                production += (info.baseProduction[level] + info.experienceMultiplier[level] * (pcm.experienceLevel - 1));
            }
            production *= 1 + info.facilityLevelMultiplier[level] * level;
            lastProduction = production;
            return production;
        }

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            enabled = !OutOfResources && built && (FacilityQueue || VesselQueue && KCProductionInfo.CanBuildVessels(level));
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


        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => lastProduction > 0 ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime) : new Dictionary<PartResourceDefinition, double>();

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

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => lastProduction > 0 ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value) : new Dictionary<PartResourceDefinition, double>();

        public override string GetFacilityProductionDisplay() => $"{kerbals.Count} kerbals assigned\ndaily production: {dailyProduction():f2}\n{(KCProductionInfo.CanBuildVessels(level) ? "Can build vessels" : "Can't build vessels")}";

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

            ConfigNode constructingFacilities = new ConfigNode("constructingFacilities");
            ConstructingFacilities[colony].ToList().ForEach(pair =>
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");
                facilityNode.AddValue("facilityID", pair.Key.id);
                facilityNode.AddValue("remainingTime", pair.Value);
                constructingFacilities.AddNode(facilityNode);
            });
            production.AddNode(constructingFacilities);

            ConfigNode constructedFacilities = new ConfigNode("constructedFacilities");
            ConstructedFacilities[colony].ForEach(facility =>
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");
                facilityNode.AddValue("facilityID", facility.id);
                constructedFacilities.AddNode(facilityNode);
            });
            production.AddNode(constructedFacilities);

            ConfigNode upgradingFacilities = new ConfigNode("upgradingFacilities");
            UpgradingFacilities[colony].ToList().ForEach(pair =>
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");
                facilityNode.AddValue("facilityID", pair.Key.id);
                facilityNode.AddValue("remainingTime", pair.Value);
                upgradingFacilities.AddNode(facilityNode);
            });
            production.AddNode(upgradingFacilities);

            ConfigNode upgradedFacilities = new ConfigNode("upgradedFacilities");
            UpgradedFacilities[colony].ForEach(facility =>
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");
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
                    ConfigNode vesselBuildInfo = new ConfigNode("vesselBuildInfo");
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
