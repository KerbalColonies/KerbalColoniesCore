using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    public class KCProductionInfo : KCFacilityInfoClass
    {
        public Dictionary<int, Dictionary<PartResourceDefinition, double>> vesselResourceCost { get; private set; } = new Dictionary<int, Dictionary<PartResourceDefinition, double>> { };

        public bool CanBuildVessels(int level) => vesselResourceCost.ContainsKey(level);

        public bool HasSameRecipt(int level, KCProductionFacility otherFacility)
        {
            Dictionary<PartResourceDefinition, double> vesselCost = vesselResourceCost[level];
            KCProductionInfo otherInfo = (KCProductionInfo)otherFacility.facilityInfo;
            return vesselCost.All(vc => otherInfo.vesselResourceCost[otherFacility.level].ContainsKey(vc.Key) ? otherInfo.vesselResourceCost[otherFacility.level][vc.Key] == vc.Value : false);
        }

        public KCProductionInfo(ConfigNode node) : base(node)
        {
            foreach (KeyValuePair<int, ConfigNode> levelNode in levelNodes)
            {
                if (levelNode.Value.HasNode("vesselResourceCost"))
                {
                    ConfigNode craftResourceNode = levelNode.Value.GetNode("vesselResourceCost");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in craftResourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    vesselResourceCost.Add(levelNode.Key, resourceList);
                }
                else if (levelNode.Key > 0 && vesselResourceCost.ContainsKey(levelNode.Key - 1)) vesselResourceCost.Add(levelNode.Key, vesselResourceCost[levelNode.Key - 1]);
            }
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
            facility.Update();

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
                                GUILayout.Label($"Time: {t.UpgradeTimes[0]}");
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
                            double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1];
                            GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                            GUILayout.EndHorizontal();
                            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        });

                        GUILayout.Space(10);

                        facility.Colony.CAB.ConstructingFacilities.ToList().ForEach(pair =>
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(pair.Key.displayName);
                            double max = pair.Key.facilityInfo.UpgradeTimes[0];
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

                    ConfigNode colonyNode = facility.Colony.sharedColonyNodes.First(n => n.name == "vesselBuildInfo");
                    KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(colonyNode.GetValue("facilityConfig"));
                    if (info.HasSameRecipt(int.Parse(colonyNode.GetValue("facilityLevel")), facility)) GUI.enabled = false;
                    if (GUILayout.Button("Use this facility type to build vessels"))
                    {
                        Configuration.writeDebug($"Facility {facility.name} is now used to build vessels.");
                        colonyNode.SetValue("facilityConfig", facility.name);
                        colonyNode.SetValue("facilityLevel", facility.level);
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
        KCProductionWindow prdWindow;

        public List<float> baseProduction { get; private set; } = new List<float> { };
        public List<float> experienceMultiplier { get; private set; } = new List<float> { };
        public List<float> facilityLevelMultiplier { get; private set; } = new List<float> { };

        public double dailyProduction()
        {
            double production = 0;

            foreach (ProtoCrewMember pcm in kerbals.Keys)
            {
                production += (baseProduction[level] + experienceMultiplier[level] * (pcm.experienceLevel - 1)) * (1 + facilityLevelMultiplier[level] * this.level);
            }
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
                ConfigNode iLevel = levelNode.Value;
                if (iLevel.HasValue("baseProduction")) baseProduction.Add(float.Parse(iLevel.GetValue("baseProduction")));
                else if (levelNode.Key > 0) baseProduction.Add(baseProduction[levelNode.Key - 1]);
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no baseProduction (at least for level 0).");

                if (iLevel.HasValue("experienceMultiplier")) experienceMultiplier.Add(float.Parse(iLevel.GetValue("experienceMultiplier")));
                else if (levelNode.Key > 0) experienceMultiplier.Add(experienceMultiplier[levelNode.Key - 1]);
                else experienceMultiplier.Add(1);

                if (iLevel.HasValue("facilityLevelMultiplier")) facilityLevelMultiplier.Add(float.Parse(iLevel.GetValue("facilityLevelMultiplier")));
                else if (levelNode.Key > 0) facilityLevelMultiplier.Add(facilityLevelMultiplier[levelNode.Key - 1]);
                else facilityLevelMultiplier.Add(1);
            }

            KCProductionInfo productionInfo = (KCProductionInfo)facilityInfo;
            if (productionInfo.CanBuildVessels(level))
            {
                if (!Colony.sharedColonyNodes.Any(n => n.name == "vesselBuildInfo"))
                {
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
