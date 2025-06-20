using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class KCProductionInfo : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, Dictionary<PartResourceDefinition, double>> vesselResourceCost { get; private set; } = new SortedDictionary<int, Dictionary<PartResourceDefinition, double>> { };

        public SortedDictionary<int, double> baseProduction { get; private set; } = new SortedDictionary<int, double> { };
        public SortedDictionary<int, double> experienceMultiplier { get; private set; } = new SortedDictionary<int, double> { };
        public SortedDictionary<int, double> facilityLevelMultiplier { get; private set; } = new SortedDictionary<int, double> { };

        public bool CanBuildVessels(int level) => vesselResourceCost.ContainsKey(level);

        public bool HasSameRecipe(int level, KCProductionFacility otherFacility)
        {
            if (!CanBuildVessels(level)) return false;
            Dictionary<PartResourceDefinition, double> vesselCost = vesselResourceCost[level];
            KCProductionInfo otherInfo = (KCProductionInfo)otherFacility.facilityInfo;
            if (!otherInfo.CanBuildVessels(otherFacility.level)) return false;
            return vesselCost.All(vc => otherInfo.vesselResourceCost[otherFacility.level].ContainsKey(vc.Key) ? otherInfo.vesselResourceCost[otherFacility.level][vc.Key] == vc.Value : false);
        }

        public KCProductionInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                ConfigNode iLevel = n.Value;
                if (iLevel.HasValue("baseProduction")) baseProduction.Add(n.Key, double.Parse(iLevel.GetValue("baseProduction")));
                else if (n.Key > 0) baseProduction.Add(n.Key, baseProduction[n.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no baseProduction (at least for level 0).");

                if (iLevel.HasValue("experienceMultiplier")) experienceMultiplier.Add(n.Key, double.Parse(iLevel.GetValue("experienceMultiplier")));
                else if (n.Key > 0) experienceMultiplier.Add(n.Key, experienceMultiplier[n.Key - 1]);
                else experienceMultiplier.Add(0, 0);

                if (iLevel.HasValue("facilityLevelMultiplier")) facilityLevelMultiplier.Add(n.Key, double.Parse(iLevel.GetValue("facilityLevelMultiplier")));
                else if (n.Key > 0) facilityLevelMultiplier.Add(n.Key, facilityLevelMultiplier[n.Key - 1]);
                else facilityLevelMultiplier.Add(0, 0);

                if (n.Value.HasNode("vesselResourceCost"))
                {
                    ConfigNode craftResourceNode = n.Value.GetNode("vesselResourceCost");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in craftResourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    vesselResourceCost.Add(n.Key, resourceList);
                }
                else if (n.Key > 0 && vesselResourceCost.ContainsKey(n.Key - 1)) vesselResourceCost.Add(n.Key, vesselResourceCost[n.Key - 1]);
            });
        }
    }

    public class KCProductionWindow : KCFacilityWindowBase
    {
        KCProductionFacility productionFacility;
        public KerbalGUI kerbalGUI;
        Type selectedType = null;
        Vector2 scrollPosTypeOverview = new Vector2();
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosUnfinishedFacilities = new Vector2();
        Vector2 scrollPosVesselCost = new Vector2();

        private static SortedDictionary<Type, List<KCFacilityInfoClass>> sortedTypes = new SortedDictionary<Type, List<KCFacilityInfoClass>>(Comparer<Type>.Create((x, y) => string.Compare(x.FullName, y.FullName))) { };
        public static SortedDictionary<Type, List<KCFacilityInfoClass>> SortedTypes => sortedTypes;

        public static void addType(KCFacilityInfoClass info)
        {
            if (sortedTypes == null) sortedTypes = new SortedDictionary<Type, List<KCFacilityInfoClass>>(Comparer<Type>.Create((x, y) => string.Compare(x.FullName, y.FullName))) { { info.type, new List<KCFacilityInfoClass> { info } } };
            else if (!sortedTypes.ContainsKey(info.type)) sortedTypes.Add(info.type, new List<KCFacilityInfoClass> { info });
            else if (!sortedTypes[info.type].Contains(info)) sortedTypes[info.type].Add(info);
        }

        public static void addAllTypes() => Configuration.BuildableFacilities.ForEach(info => addType(info));


        protected override void CustomWindow()
        {
            KCProductionFacility.ExecuteProduction(productionFacility.Colony);

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(productionFacility, true);
            }

            if (sortedTypes.Count == 0) addAllTypes();

            GUILayout.BeginHorizontal(GUILayout.Width(600));{
                GUILayout.BeginVertical();{
                    GUILayout.BeginHorizontal(GUILayout.Height(300));{
                        GUILayout.BeginVertical(GUILayout.Width(300));
                        {
                            kerbalGUI.StaffingInterface();
                        }
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical(GUILayout.Width(300));
                        scrollPosTypeOverview = GUILayout.BeginScrollView(scrollPosTypeOverview);
                        {
                            SortedTypes.ToList().ForEach(kvp =>
                            {
                                if (GUILayout.Button($"{kvp.Key.Name} ({kvp.Value.Count})"))
                                {
                                    if (selectedType == kvp.Key)
                                    {
                                        selectedType = null;
                                        toolRect = new Rect(toolRect.x, toolRect.y, 620, 700);
                                    }
                                    else
                                    {
                                        selectedType = kvp.Key;
                                        toolRect = new Rect(toolRect.x, toolRect.y, 1110, 700);
                                    }
                                }
                            });
                        }
                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);
                    GUILayout.Label($"Daily production: {productionFacility.dailyProduction():f2}");
                    GUILayout.Space(10);
                    GUILayout.Label("Unfinished facilities");

                    scrollPosUnfinishedFacilities = GUILayout.BeginScrollView(scrollPosUnfinishedFacilities);
                    {
                        GUILayout.Label("Facilities under construction:");
                        GUILayout.BeginVertical();
                        {
                            GUILayout.Label("Upgrading Facilities:");
                            productionFacility.Colony.CAB.UpgradingFacilities.ToList().ForEach(pair =>
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(pair.Key.DisplayName);
                                double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1] * Configuration.FacilityTimeMultiplier;
                                GUILayout.Label($"{max - pair.Value:f2}/{max:f2}");
                                GUILayout.EndHorizontal();
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            });

                            GUILayout.Space(10);

                            productionFacility.Colony.CAB.ConstructingFacilities.ToList().ForEach(pair =>
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(pair.Key.DisplayName);
                                double max = pair.Key.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier;
                                GUILayout.Label($"{(max - pair.Value):f2}/{max:f2}");
                                GUILayout.EndHorizontal();
                                GUILayout.Space(10);
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                                GUILayout.Space(10);
                            });
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndScrollView();

                    if (((KCProductionInfo)productionFacility.facilityInfo).CanBuildVessels(productionFacility.level))
                    {
                        GUILayout.Space(10);
                        GUILayout.Label("This facility can build vessels.");
                        GUILayout.Label("Costs per ton of the vessel:");
                        scrollPosVesselCost = GUILayout.BeginScrollView(scrollPosVesselCost, GUIStyle.none);
                        {
                            KCProductionInfo kCProductionInfo = (KCProductionInfo)productionFacility.facilityInfo;
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.BeginVertical(GUILayout.Width(340));
                                {
                                    for (int i = 0; i < kCProductionInfo.vesselResourceCost[productionFacility.level].Count / 2; i++)
                                    {
                                        KeyValuePair<PartResourceDefinition, double> resource = kCProductionInfo.vesselResourceCost[productionFacility.level].ElementAt(i);
                                        GUILayout.Label($"{resource.Key.displayName}: {resource.Value * Configuration.VesselCostMultiplier}");
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.BeginVertical(GUILayout.Width(340));
                                {
                                    for (int i = kCProductionInfo.vesselResourceCost[productionFacility.level].Count / 2; i < kCProductionInfo.vesselResourceCost[productionFacility.level].Count; i++)
                                    {
                                        KeyValuePair<PartResourceDefinition, double> resource = kCProductionInfo.vesselResourceCost[productionFacility.level].ElementAt(i);
                                        GUILayout.Label($"{resource.Key.displayName}: {resource.Value * Configuration.VesselCostMultiplier}");
                                    }
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();

                        ConfigNode colonyNode = productionFacility.Colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
                        if (colonyNode != null)
                        {
                            KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(colonyNode.GetValue("facilityConfig"));
                            if (info != null)
                            {
                                if (info.HasSameRecipe(int.Parse(colonyNode.GetValue("facilityLevel")), productionFacility)) GUI.enabled = false;
                                if (GUILayout.Button("Use this facility type to build vessels"))
                                {
                                    Configuration.writeDebug($"Facility {productionFacility.name} is now used to build vessels.");
                                    colonyNode.SetValue("facilityConfig", productionFacility.name);
                                    colonyNode.SetValue("facilityLevel", productionFacility.level);
                                }
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Use this facility type to build vessels"))
                            {
                                Configuration.writeDebug($"Facility {productionFacility.name} is now used to build vessels.");
                                ConfigNode vesselBuildInfo = new ConfigNode("vesselBuildInfo");
                                vesselBuildInfo.AddValue("facilityConfig", productionFacility.name);
                                vesselBuildInfo.AddValue("facilityLevel", productionFacility.level);
                                productionFacility.Colony.sharedColonyNodes.Add(vesselBuildInfo);
                            }
                        }
                        GUI.enabled = true;
                    }
                }
                GUILayout.EndVertical();

                if (selectedType != null)
                {
                    GUILayout.BeginVertical(GUILayout.Width(480));
                    {
                        GUILayout.Label($"{selectedType.Name} facilities");
                        scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes);
                        {
                            foreach (KCFacilityInfoClass t in SortedTypes[selectedType])
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label($"{t.displayName}\t");
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                {
                                    for (int i = 0; i < t.resourceCost[0].Count; i++)
                                    {
                                        GUILayout.Label($"{t.resourceCost[0].ElementAt(i).Key.displayName}: {t.resourceCost[0].ElementAt(i).Value * Configuration.FacilityCostMultiplier}");
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                GUILayout.Label($"Funds: {(t.Funds.Count > 0 ? t.Funds[0] : 0) * Configuration.FacilityCostMultiplier}");
                                //GUILayout.Label($"ECperSecond: {t.ECperSecond}");
                                GUILayout.Label($"Time: {t.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier}");
                                GUILayout.EndVertical();

                                GUILayout.EndHorizontal();

                                GUILayout.Space(10);

                                if (!t.checkResources(0, productionFacility.Colony)) { GUI.enabled = false; }

                                if (GUILayout.Button("Build"))
                                {
                                    Configuration.writeLog($"Building facility {t.displayName} in colony {productionFacility.Colony.Name}");

                                    t.removeResources(0, productionFacility.Colony);
                                    KCFacilityBase KCFac = Configuration.CreateInstance(t, productionFacility.Colony, false);

                                    productionFacility.Colony.CAB.AddconstructingFacility(KCFac);
                                }
                                GUI.enabled = true;
                                GUILayout.Space(10);
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                                GUILayout.Space(10);
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }
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

        public KCProductionWindow(KCProductionFacility facility) : base(facility, Configuration.createWindowID())
        {
            this.productionFacility = facility;
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 620, 700);
        }
    }

    public class KCProductionFacility : KCKerbalFacilityBase, KCECConsumer
    {
        public static bool facilityQueue = false;
        public static bool vesselQueue = false;

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
            double dt = getDeltaTime(colony);

            KCProductionFacility.DailyProductions(colony, out double dailyProduction, out double dailyVesselProduction);

            dailyProduction = (((dailyProduction * dt) / 6) / 60) / 60; // convert from Kerbin days (6 hours) to seconds
            dailyVesselProduction = (((dailyVesselProduction * dt) / 6) / 60) / 60;

            List<StoredVessel> constructingVessel = KCHangarFacility.GetConstructingVessels(colony);

            if (constructingVessel.Count > 0)
            {
                vesselQueue = true;
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
            else vesselQueue = false;

            dailyProduction += dailyVesselProduction;

            if (colony.CAB.UpgradingFacilities.Count > 0 || colony.CAB.ConstructingFacilities.Count > 0)
            {
                facilityQueue = true;
                while (dailyProduction > 0)
                {
                    if (colony.CAB.UpgradingFacilities.Count > 0)
                    {
                        if (colony.CAB.UpgradingFacilities.ElementAt(0).Value > dailyProduction)
                        {
                            colony.CAB.UpgradingFacilities[colony.CAB.UpgradingFacilities.ElementAt(0).Key] -= dailyProduction;
                            dailyProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = colony.CAB.UpgradingFacilities.ElementAt(0).Key;
                            dailyProduction -= colony.CAB.UpgradingFacilities.ElementAt(0).Value;
                            colony.CAB.UpgradingFacilities.Remove(facility);

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
                                    colony.CAB.addUpgradedFacility(facility);
                                    break;
                            }
                        }
                    }
                    else if (colony.CAB.ConstructingFacilities.Count > 0)
                    {
                        if (colony.CAB.ConstructingFacilities.ElementAt(0).Value > dailyProduction)
                        {
                            colony.CAB.ConstructingFacilities[colony.CAB.ConstructingFacilities.ElementAt(0).Key] -= dailyProduction;
                            dailyProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = colony.CAB.ConstructingFacilities.ElementAt(0).Key;
                            dailyProduction -= colony.CAB.ConstructingFacilities.ElementAt(0).Value;
                            colony.CAB.ConstructingFacilities.Remove(facility);
                            colony.CAB.addConstructedFacility(facility);
                            ScreenMessages.PostScreenMessage($"KC: Facility {facility.DisplayName} was fully built on colony {colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else facilityQueue = false;
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

        protected KCProductionWindow prdWindow;
        public KCProductionInfo KCProductionInfo => (KCProductionInfo)facilityInfo;

        public bool outOfEC { get; protected set; } = false;
        public double lastProduction { get; protected set; } = 0;

        public double dailyProduction()
        {
            if (outOfEC || !enabled) return 0;
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
            enabled = !outOfEC && built && (facilityQueue || vesselQueue && KCProductionInfo.CanBuildVessels(level));
        }

        public override void OnBuildingClicked()
        {
            prdWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            prdWindow.Toggle();
        }

        public int ECConsumptionPriority => 0;
        // Check if facility has daily production
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => lastProduction > 0 ? facilityInfo.ECperSecond[level] * deltaTime : 0;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => outOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => outOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;


        public override string GetFacilityProductionDisplay() => $"{kerbals.Count} kerbals assigned\ndaily production: {dailyProduction():f2}\n{(KCProductionInfo.CanBuildVessels(level) ? "Can build vessels" : "Can't build vessels")}";

        private void configNodeLoader()
        {
            prdWindow = new KCProductionWindow(this);

            foreach (KeyValuePair<int, ConfigNode> levelNode in facilityInfo.levelNodes)
            {

            }

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
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader();
        }
    }
}
