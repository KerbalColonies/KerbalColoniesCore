using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalColonies.UI;

namespace KerbalColonies.colonyFacilities
{
    public class KCProductionWindow : KCWindowBase
    {
        KCProductionFacility facility;
        public KerbalGUI kerbalGUI;
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosUnfinishedFacilities = new Vector2();

        protected override void CustomWindow()
        {
            facility.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(facility, true);
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal(GUILayout.Height(400));
            GUILayout.BeginVertical(GUILayout.Width(300));
            kerbalGUI.StaffingInterface();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(480));
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
                    GUILayout.Space(20);
                    GUI.enabled = true;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
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
                        GUILayout.Label(pair.Key.name);
                        double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1];
                        GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                        GUILayout.EndHorizontal();
                    });

                    GUILayout.Space(10);

                    facility.Colony.CAB.ConstructingFacilities.ToList().ForEach(pair =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(pair.Key.name);
                        double max = pair.Key.facilityInfo.UpgradeTimes[0];
                        GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                        GUILayout.EndHorizontal();
                    });
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
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

        private void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());
                if (iLevel.HasValue("baseProduction")) baseProduction.Add(float.Parse(iLevel.GetValue("baseProduction")));
                else if (i > 0) baseProduction.Add(baseProduction[i - 1]);
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no baseProduction (at least for level 0).");

                if (iLevel.HasValue("experienceMultiplier")) experienceMultiplier.Add(float.Parse(iLevel.GetValue("experienceMultiplier")));
                else if (i > 0) experienceMultiplier.Add(experienceMultiplier[i - 1]);
                else experienceMultiplier.Add(1);

                if (iLevel.HasValue("facilityLevelMultiplier")) facilityLevelMultiplier.Add(float.Parse(iLevel.GetValue("facilityLevelMultiplier")));
                else if (i > 0) facilityLevelMultiplier.Add(facilityLevelMultiplier[i - 1]);
                else facilityLevelMultiplier.Add(1);
            }
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            prdWindow = new KCProductionWindow(this);
        }

        public KCProductionFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            prdWindow = new KCProductionWindow(this);
        }
    }
}
