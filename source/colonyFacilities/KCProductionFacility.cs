using KerbalColonies.Electricity;
using KerbalColonies.UI;
using KerbalKonstructs.Modules;
using KSP.UI.Screens.Mapview;
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
        string selectedType = null;
        Vector2 scrollPosTypeOverview = new Vector2();
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosUnfinishedFacilities = new Vector2();
        Vector2 scrollPosVesselCost = new Vector2();

        private static SortedDictionary<string, List<KCFacilityInfoClass>> sortedTypes = new SortedDictionary<string, List<KCFacilityInfoClass>>() { };
        public static SortedDictionary<string, List<KCFacilityInfoClass>> SortedTypes => sortedTypes;

        public static void addType(KCFacilityInfoClass info)
        {
            if (info.hidden) return;

            if (sortedTypes == null) sortedTypes = new SortedDictionary<string, List<KCFacilityInfoClass>>() { { info.category, new List<KCFacilityInfoClass> { info } } };
            else if (!sortedTypes.ContainsKey(info.category)) sortedTypes.Add(info.category, new List<KCFacilityInfoClass> { info });
            else if (!sortedTypes[info.category].Contains(info)) sortedTypes[info.category].Add(info);
        }

        public static void addAllTypes() => Configuration.BuildableFacilities.ForEach(info => addType(info));

        protected override void OnOpen() => selectedType = null;

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
                            GUILayout.Space(10);
                            GUILayout.Label($"Daily production: {productionFacility.dailyProduction():f2}");
                        }
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical(GUILayout.Width(300));
                        {
                            scrollPosTypeOverview = GUILayout.BeginScrollView(scrollPosTypeOverview);
                            {
                                SortedTypes.ToList().ForEach(kvp =>
                                {
                                    if (GUILayout.Button($"{kvp.Key} ({kvp.Value.Count})"))
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
                            if (facility.facilityInfo.ECperSecond[facility.level] > 0)
                            {
                                GUILayout.Space(10);
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label($"EC Consumption Priority: {productionFacility.ECConsumptionPriority}", GUILayout.Height(18));
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) productionFacility.ECConsumptionPriority--;
                                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) productionFacility.ECConsumptionPriority++;
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);
                    GUILayout.Label("Unfinished facilities");

                    scrollPosUnfinishedFacilities = GUILayout.BeginScrollView(scrollPosUnfinishedFacilities);
                    {
                        GUILayout.Label("Facilities under construction:");
                        GUILayout.BeginVertical();
                        {
                            GUILayout.Label("Upgrading Facilities:");
                            KCProductionFacility.UpgradingFacilities[facility.Colony].ToList().ForEach(pair =>
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(pair.Key.DisplayName);
                                double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1] * Configuration.FacilityTimeMultiplier;
                                GUILayout.Label($"{max - pair.Value:f2}/{max:f2}");
                                GUILayout.EndHorizontal();
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            });

                            GUILayout.Space(10);

                            KCProductionFacility.ConstructingFacilities[facility.Colony].ToList().ForEach(pair =>
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
                        GUILayout.Label($"{selectedType} facilities");
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
                                        GUILayout.Label($"{t.resourceCost[0].ElementAt(i).Key.displayName}: {(t.resourceCost[0].ElementAt(i).Value * Configuration.FacilityCostMultiplier):f2}");
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                GUILayout.Label($"Funds: {((t.Funds.Count > 0 ? t.Funds[0] : 0) * Configuration.FacilityCostMultiplier):f2}");
                                //GUILayout.Label($"ECperSecond: {t.ECperSecond}");
                                GUILayout.Label($"Time: {(t.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier):f2}");
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

        public bool FacilityQueue => ConstructingFacilities[Colony].Count > 0 || UpgradingFacilities[Colony].Count > 0;
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
            enabled = !outOfEC && built && (FacilityQueue || VesselQueue && KCProductionInfo.CanBuildVessels(level));
        }

        public override void OnBuildingClicked()
        {
            prdWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            prdWindow.Toggle();
        }

        public int ECConsumptionPriority { get; set; } = 0;
        // Check if facility has daily production
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => lastProduction > 0 ? facilityInfo.ECperSecond[level] * deltaTime : 0;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => outOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => outOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;


        public override string GetFacilityProductionDisplay() => $"{kerbals.Count} kerbals assigned\ndaily production: {dailyProduction():f2}\n{(KCProductionInfo.CanBuildVessels(level) ? "Can build vessels" : "Can't build vessels")}";

        public override ConfigNode getConfigNode()
        {
            UpdateSharedNode(Colony);

            ConfigNode node = base.getConfigNode();
            node.AddValue("ECConsumptionPriority", ECConsumptionPriority);
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

            if (ConstructingFacilities.TryAdd(Colony, new Dictionary<KCFacilityBase, double>()))
            {
                ConstructedFacilities.Add(Colony, new List<KCFacilityBase>());
                UpgradingFacilities.Add(Colony, new Dictionary<KCFacilityBase, double>());
                UpgradedFacilities.Add(Colony, new List<KCFacilityBase>());

                ConfigNode production = Colony.sharedColonyNodes.FirstOrDefault(n => n.name == "production");
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

        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader();
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int priority)) ECConsumptionPriority = priority;
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader();
        }
    }
}
