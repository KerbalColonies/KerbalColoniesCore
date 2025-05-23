using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static KSP.UI.Screens.SpaceCenter.BuildingPicker;

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

    public class KCProductionWindow : KCWindowBase
    {
        KCProductionFacility facility;
        public KerbalGUI kerbalGUI;
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosUnfinishedFacilities = new Vector2();
        Vector2 scrollPosVesselCost = new Vector2();

        protected override void CustomWindow()
        {
            facility.Colony.CAB.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(facility, true);
            }

            GUILayout.BeginVertical();
            {

                GUILayout.BeginHorizontal(GUILayout.Height(300));
                {
                    GUILayout.BeginVertical(GUILayout.Width(300));
                    {
                        kerbalGUI.StaffingInterface();
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width(480));
                    {
                        GUILayout.Label($"Facility types");
                        scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes);
                        {
                            foreach (KCFacilityInfoClass t in Configuration.BuildableFacilities)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label($"{t.displayName}\t");
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                {
                                    for (int i = 0; i < t.resourceCost[0].Count; i++)
                                    {
                                        GUILayout.Label($"{t.resourceCost[0].ElementAt(i).Key.displayName}: {t.resourceCost[0].ElementAt(i).Value}");
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                GUILayout.Label($"Funds: {(t.Funds.Count > 0 ? t.Funds[0] : 0)}");
                                //GUILayout.Label($"Electricity: {t.Electricity}");
                                GUILayout.Label($"Time: {t.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier}");
                                GUILayout.EndVertical();

                                GUILayout.EndHorizontal();

                                GUILayout.Space(10);

                                if (!t.checkResources(0, facility.Colony)) { GUI.enabled = false; }

                                if (GUILayout.Button("Build"))
                                {
                                    t.removeResources(0, facility.Colony);
                                    KCFacilityBase KCFac = Configuration.CreateInstance(t, facility.Colony, false);

                                    facility.Colony.CAB.AddconstructingFacility(KCFac);
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
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label($"Daily production: {Math.Round(facility.dailyProduction(), 2)}");
                GUILayout.Space(10);
                GUILayout.Label("Unfinished facilities");

                scrollPosUnfinishedFacilities = GUILayout.BeginScrollView(scrollPosUnfinishedFacilities);
                {
                    GUILayout.Label("Facilities under construction:");
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Upgrading Facilities:");
                        facility.Colony.CAB.UpgradingFacilities.ToList().ForEach(pair =>
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(pair.Key.displayName);
                            double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1] * Configuration.FacilityTimeMultiplier;
                            GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                            GUILayout.EndHorizontal();
                            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        });

                        GUILayout.Space(10);

                        facility.Colony.CAB.ConstructingFacilities.ToList().ForEach(pair =>
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(pair.Key.displayName);
                            double max = pair.Key.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier;
                            GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                            GUILayout.EndHorizontal();
                            GUILayout.Space(10);
                            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            GUILayout.Space(10);
                        });
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();

                if (((KCProductionInfo)facility.facilityInfo).CanBuildVessels(facility.level))
                {
                    GUILayout.Space(10);
                    GUILayout.Label("This facility can build vessels.");
                    GUILayout.Label("Costs per ton of the vessel:");
                    scrollPosVesselCost = GUILayout.BeginScrollView(scrollPosVesselCost, GUIStyle.none);
                    {
                        KCProductionInfo kCProductionInfo = (KCProductionInfo)facility.facilityInfo;
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical(GUILayout.Width(340));
                            {
                                for (int i = 0; i < kCProductionInfo.vesselResourceCost[facility.level].Count / 2; i++)
                                {
                                    KeyValuePair<PartResourceDefinition, double> resource = kCProductionInfo.vesselResourceCost[facility.level].ElementAt(i);
                                    GUILayout.Label($"{resource.Key.displayName}: {resource.Value}");
                                }
                            }
                            GUILayout.EndVertical();
                            GUILayout.BeginVertical(GUILayout.Width(340));
                            {
                                for (int i = kCProductionInfo.vesselResourceCost[facility.level].Count / 2; i < kCProductionInfo.vesselResourceCost[facility.level].Count; i++)
                                {
                                    KeyValuePair<PartResourceDefinition, double> resource = kCProductionInfo.vesselResourceCost[facility.level].ElementAt(i);
                                    GUILayout.Label($"{resource.Key.displayName}: {resource.Value}");
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                    ConfigNode colonyNode = facility.Colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
                    if (colonyNode != null)
                    {
                        KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(colonyNode.GetValue("facilityConfig"));
                        if (info != null)
                        {
                            if (info.HasSameRecipe(int.Parse(colonyNode.GetValue("facilityLevel")), facility)) GUI.enabled = false;
                            if (GUILayout.Button("Use this facility type to build vessels"))
                            {
                                Configuration.writeDebug($"Facility {facility.name} is now used to build vessels.");
                                colonyNode.SetValue("facilityConfig", facility.name);
                                colonyNode.SetValue("facilityLevel", facility.level);
                            }
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Use this facility type to build vessels"))
                        {
                            Configuration.writeDebug($"Facility {facility.name} is now used to build vessels.");
                            ConfigNode vesselBuildInfo = new ConfigNode("vesselBuildInfo");
                            vesselBuildInfo.AddValue("facilityConfig", facility.name);
                            vesselBuildInfo.AddValue("facilityLevel", facility.level);
                            facility.Colony.sharedColonyNodes.Add(vesselBuildInfo);
                        }
                    }
                    GUI.enabled = true;
                }
            }
            GUILayout.EndVertical();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCProductionWindow(KCProductionFacility facility) : base(Configuration.createWindowID(), "Production Facility")
        {
            this.facility = facility;
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 800, 700);

        }
    }

    public class KCProductionFacility : KCKerbalFacilityBase
    {
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

        KCProductionWindow prdWindow;
        KCProductionInfo KCProductionInfo => (KCProductionInfo)facilityInfo;

        public double dailyProduction()
        {
            double production = 0;

            KCProductionInfo info = KCProductionInfo;

            foreach (ProtoCrewMember pcm in kerbals.Keys)
            {
                production += (info.baseProduction[level] + info.experienceMultiplier[level] * (pcm.experienceLevel - 1));
            }
            production *= 1 + info.facilityLevelMultiplier[level] * level;
            return production;
        }

        public override void OnBuildingClicked()
        {
            prdWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            prdWindow.Toggle();
        }

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
