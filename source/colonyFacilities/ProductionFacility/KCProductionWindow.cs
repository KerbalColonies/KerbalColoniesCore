using KerbalColonies.Settings;
using KerbalColonies.UI;
using KSP.Localization;
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
        private KCProductionFacility productionFacility;
        public KerbalGUI kerbalGUI;
        private string selectedType = null;
        private Vector2 scrollPosTypeOverview = new();
        private Vector2 scrollPosTypes = new();
        private Vector2 scrollPosUnfinishedFacilities = new();
        private Vector2 scrollPosVesselCost = new();
        private Vector2 resourceUsageScrollPos = new();
        private bool editQueue;

        private int cabLevel;
        private SortedDictionary<string, List<KCFacilityInfoClass>> sortedTypes = [];
        public SortedDictionary<string, List<KCFacilityInfoClass>> SortedTypes => sortedTypes;

        public void addType(KCFacilityInfoClass info)
        {
            if (info.hidden) return;
            if (!KCTechTreeHandler.CanBuild(info, 0)) return;

            if (sortedTypes == null) sortedTypes = new SortedDictionary<string, List<KCFacilityInfoClass>>() { { info.category, new List<KCFacilityInfoClass> { info } } };
            else if (!sortedTypes.ContainsKey(info.category)) sortedTypes.Add(info.category, [info]);
            else if (!sortedTypes[info.category].Contains(info)) sortedTypes[info.category].Add(info);
        }

        public void addAllTypes() => Configuration.BuildableFacilities.ForEach(info => addType(info));

        protected override void OnOpen()
        {
            selectedType = null;
            editQueue = false;
            toolRect = new Rect(100, 100, 920, 700);
        }

        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();

            kerbalGUI ??= new KerbalGUI(productionFacility, true);

            if (sortedTypes.Count == 0) addAllTypes();

            cabLevel = facility.Colony.CAB.level;

            GUILayout.BeginHorizontal(GUILayout.Width(900));
            {
                GUILayout.BeginVertical(GUILayout.Width(450));
                {
                    DrawProductionQueue();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.Width(600));
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(300));
                    {
                        GUILayout.BeginVertical(GUILayout.Width(300));
                        {
                            kerbalGUI.StaffingInterface();
                            GUILayout.Space(10);
                            GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_DAILY", productionFacility.dailyProduction().ToString("f2")));
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
                                            toolRect = new Rect(toolRect.x, toolRect.y, 920, 700);
                                        }
                                        else
                                        {
                                            selectedType = kvp.Key;
                                            toolRect = new Rect(toolRect.x, toolRect.y, 1410, 700);
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
                            GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_PRIORITY", productionFacility.ResourceConsumptionPriority), GUILayout.Height(18));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) productionFacility.ResourceConsumptionPriority--;
                            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) productionFacility.ResourceConsumptionPriority++;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_USAGE"));
                        resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(40));
                        {
                            productionFacility.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                                GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_RATE_ITEM", kvp.Key.displayName, kvp.Value))
                            );
                        }
                        GUILayout.EndScrollView();
                    }

                    if (((KCProductionInfo)productionFacility.facilityInfo).CanBuildVessels(productionFacility.level))
                    {
                        GUILayout.Space(10);
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_CAN_BUILD_VESSELS"));
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_VESSEL_COSTS"));
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
                                        GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_LABEL_VALUE", resource.Key.displayName, resource.Value * Configuration.VesselCostMultiplier));
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.BeginVertical(GUILayout.Width(340));
                                {
                                    for (int i = kCProductionInfo.vesselResourceCost[productionFacility.level].Count / 2; i < kCProductionInfo.vesselResourceCost[productionFacility.level].Count; i++)
                                    {
                                        KeyValuePair<PartResourceDefinition, double> resource = kCProductionInfo.vesselResourceCost[productionFacility.level].ElementAt(i);
                                        GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_LABEL_VALUE", resource.Key.displayName, resource.Value * Configuration.VesselCostMultiplier));
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
                                if (GUILayout.Button(Localizer.Format("#LOC_KC_PRODUCTION_USE_FOR_VESSELS")))
                                {
                                    Configuration.writeDebug($"Facility {productionFacility.name} is now used to build vessels.");
                                    colonyNode.SetValue("facilityConfig", productionFacility.name);
                                    colonyNode.SetValue("facilityLevel", productionFacility.level);
                                }
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(Localizer.Format("#LOC_KC_PRODUCTION_USE_FOR_VESSELS")))
                            {
                                Configuration.writeDebug($"Facility {productionFacility.name} is now used to build vessels.");
                                ConfigNode vesselBuildInfo = new("vesselBuildInfo");
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
                        GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_FACILITY_TYPE", selectedType));
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
                                        GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_LABEL_VALUE", t.resourceCost[0].ElementAt(i).Key.displayName, (t.resourceCost[0].ElementAt(i).Value * Configuration.FacilityCostMultiplier).ToString("f2")));
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_FUNDS", ((t.Funds.Count > 0 ? t.Funds[0] : 0) * Configuration.FacilityCostMultiplier).ToString("f2")));
                                //GUILayout.Label($"ECperSecond: {t.ECperSecond}");
                                GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_TIME", (t.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier).ToString("f2")));
                                if (!cabLevelPass) GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_MIN_CAB", t.MinCABLevel[0]));
                                GUILayout.EndVertical();

                                GUILayout.EndHorizontal();

                                GUILayout.Space(10);

                                if (!cabLevelPass) { GUI.enabled = false; }

                                if (GUILayout.Button(Localizer.Format("#LOC_KC_COMMON_BUILD")))
                                {
                                    Configuration.writeLog($"Building facility {t.displayName} in colony {productionFacility.Colony.Name}");

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

        private void DrawProductionQueue()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_QUEUE"));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format(editQueue ? "#LOC_KC_PRODUCTION_DONE_CANCELING" : "#LOC_KC_PRODUCTION_CANCEL_ITEMS"), GUILayout.Width(100))) editQueue = !editQueue;
            GUILayout.EndHorizontal();

            scrollPosUnfinishedFacilities = GUILayout.BeginScrollView(scrollPosUnfinishedFacilities);
            List<KCProductionQueueItem> queue = KCProductionFacility.GetQueue(facility.Colony);
            for (int i = 0; i < queue.Count; i++)
            {
                KCProductionQueueItem item = queue[i];
                bool compatible = facility.Colony.Facilities.OfType<KCProductionFacility>().Any(producer => item.Matches(producer));
                bool affordable = item.GetAffordableProgress(facility.Colony, item.RemainingProgress) > 0;
                double requiredProgress = item.GetBuildTime(facility.Colony);

                GUILayout.Label($"{item.GetDisplayName(facility.Colony)}{(!compatible ? Localizer.Format("#LOC_KC_PRODUCTION_NO_PRODUCER") : !affordable ? Localizer.Format("#LOC_KC_PRODUCTION_WAITING_COSTS") : "")}");
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KC_PRODUCTION_PROGRESS", (item.Progress * requiredProgress).ToString("f2"), requiredProgress.ToString("f2"), (item.Progress * 100).ToString("f1")), GUILayout.Width(160));
                GUILayout.FlexibleSpace();
                GUI.enabled = i > 0;
                if (GUILayout.Button(Localizer.Format("#LOC_KC_PRODUCTION_MOVE_UP"), GUILayout.Width(25))) KCProductionFacility.MoveQueueItem(facility.Colony, item, -1);
                GUI.enabled = i < queue.Count - 1;
                if (GUILayout.Button(Localizer.Format("#LOC_KC_PRODUCTION_MOVE_DOWN"), GUILayout.Width(25))) KCProductionFacility.MoveQueueItem(facility.Colony, item, 1);
                GUI.enabled = true;
                if (editQueue && GUILayout.Button(Localizer.Format("#LOC_KC_COMMON_CANCEL"), UIConfig.ButtonRed, GUILayout.Width(55)))
                {
                    KCProductionFacility.CancelQueueItem(facility.Colony, item);
                    break;
                }
                GUILayout.EndHorizontal();
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            }
            GUILayout.EndScrollView();
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
            productionFacility = facility;
            kerbalGUI = null;
            toolRect = new Rect(100, 100, 920, 800);
        }
    }
}
