using KerbalColonies.UI;
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
    public class KCProductionWindow : KCFacilityWindowBase
    {
        KCProductionFacility productionFacility;
        public KerbalGUI kerbalGUI;
        string selectedType = null;
        Vector2 scrollPosTypeOverview = new Vector2();
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosUnfinishedFacilities = new Vector2();
        Vector2 scrollPosVesselCost = new Vector2();
        Vector2 resourceUsageScrollPos = new Vector2();

        private int cabLevel;
        private SortedDictionary<string, List<KCFacilityInfoClass>> sortedTypes = new SortedDictionary<string, List<KCFacilityInfoClass>>() { };
        public SortedDictionary<string, List<KCFacilityInfoClass>> SortedTypes => sortedTypes;

        public void addType(KCFacilityInfoClass info)
        {
            if (info.hidden) return;
            if (!KCTechTreeHandler.CanBuild(info, 0)) return;

            if (sortedTypes == null) sortedTypes = new SortedDictionary<string, List<KCFacilityInfoClass>>() { { info.category, new List<KCFacilityInfoClass> { info } } };
            else if (!sortedTypes.ContainsKey(info.category)) sortedTypes.Add(info.category, new List<KCFacilityInfoClass> { info });
            else if (!sortedTypes[info.category].Contains(info)) sortedTypes[info.category].Add(info);
        }

        public void addAllTypes() => Configuration.BuildableFacilities.ForEach(info => addType(info));

        protected override void OnOpen()
        {
            selectedType = null;
            toolRect = new Rect(100, 100, 620, 700);
        }

        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(productionFacility, true);
            }

            if (sortedTypes.Count == 0) addAllTypes();

            cabLevel = facility.Colony.CAB.level;

            GUILayout.BeginHorizontal(GUILayout.Width(600));
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(300));
                    {
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
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    if (facility.facilityInfo.ResourceUsage[facility.level].Count > 0)
                    {
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"Resource Consumption Priority: {productionFacility.ResourceConsumptionPriority}", GUILayout.Height(18));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) productionFacility.ResourceConsumptionPriority--;
                            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) productionFacility.ResourceConsumptionPriority++;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Label("Resource usage:");
                        resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(120));
                        {
                            productionFacility.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                                GUILayout.Label($"- {kvp.Key.displayName}: {kvp.Value}/s")
                            );
                        }
                        GUILayout.EndScrollView();
                    }

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
                                double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1] * Configuration.FacilityTimeMultiplier;
                                GUILayout.Label($"{pair.Key.DisplayName}: {max - pair.Value:f2}/{max:f2}");
                                GUILayout.EndHorizontal();
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            });

                            GUILayout.Space(10);

                            KCProductionFacility.ConstructingFacilities[facility.Colony].ToList().ForEach(pair =>
                            {
                                GUILayout.BeginHorizontal();
                                double max = pair.Key.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier;
                                GUILayout.Label($"{pair.Key.DisplayName}: {(max - pair.Value):f2}/{max:f2}");
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
                                bool cabLevelPass = t.MinCABLevel[0] <= cabLevel;

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
                                if (!cabLevelPass) GUILayout.Label($"Minimum CAB Level: {t.MinCABLevel[0]}");
                                GUILayout.EndVertical();

                                GUILayout.EndHorizontal();

                                GUILayout.Space(10);

                                if (!t.checkResources(0, productionFacility.Colony) || !cabLevelPass) { GUI.enabled = false; }

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
}
