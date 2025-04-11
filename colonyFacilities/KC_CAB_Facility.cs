using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KC_CAB_Window : KCWindowBase
    {
        private KC_CAB_Facility facility;

        protected override void CustomWindow()
        {
            facility.Update();

            GUILayout.BeginScrollView(new Vector2());
            {
                foreach (KCFacilityInfoClass t in Configuration.BuildableFacilities)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(t.displayName);
                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < t.resourceCost[0].Count; i++)
                        {
                            GUILayout.Label($"{t.resourceCost[0].ElementAt(i).Key.displayName}: {t.resourceCost[0].ElementAt(i).Value}");
                        }
                    }
                    GUILayout.EndVertical();

                    if (!t.checkResources(0, facility.Colony)) { GUI.enabled = false; }

                    if (GUILayout.Button("Build"))
                    {
                        t.removeResources(0, facility.Colony);
                        KCFacilityBase KCFac = Configuration.CreateInstance(t, facility.Colony, false);

                        facility.AddconstructingFacility(KCFac);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Facilities in this Colony:");

                GUILayout.BeginVertical();

                facility.Colony.Facilities.ForEach(colonyFacility =>
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(colonyFacility.displayName);
                    GUILayout.Label(colonyFacility.level.ToString());
                    GUILayout.Label(colonyFacility.GetFacilityProductionDisplay());

                    if (colonyFacility.upgradeable && colonyFacility.level < colonyFacility.maxLevel)
                    {
                        KCFacilityInfoClass facilityInfo = Configuration.GetInfoClass(colonyFacility.name);

                        if (!facilityInfo.checkResources(colonyFacility.level + 1, facility.Colony)) GUI.enabled = false;

                        GUILayout.BeginVertical();
                        {
                            facilityInfo.resourceCost[colonyFacility.level].ToList().ForEach(pair =>
                            {
                                GUILayout.Label($"{pair.Key.displayName}: {pair.Value}");
                            });

                            if (!(facility.UpgradingFacilities.ContainsKey(colonyFacility) || facility.UpgradedFacilities.Contains(colonyFacility)))
                            {
                                if (GUILayout.Button("Upgrade"))
                                {
                                    facilityInfo.removeResources(colonyFacility.level + 1, facility.Colony);
                                    if (colonyFacility.upgradeType == UpgradeType.withGroupChange)
                                    {
                                        KCFacilityBase.UpgradeFacilityWithGroupChange(colonyFacility);
                                    }
                                    else if (colonyFacility.upgradeType == UpgradeType.withoutGroupChange)
                                    {
                                        KCFacilityBase.UpgradeFacilityWithoutGroupChange(colonyFacility);
                                    }
                                    else if (colonyFacility.upgradeType == UpgradeType.withAdditionalGroup)
                                    {
                                        facility.AddUpgradeableFacility(colonyFacility);

                                        //KCFacilityBase.UpgradeFacilityWithAdditionalGroup(colonyFacility);
                                    }
                                }
                            }
                        }
                        GUI.enabled = true;
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Facilities under construction:");
                facility.ConstructingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime(0);
                    GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Facilities waiting to be placed:");
                facility.ConstructedFacilities.ToList().ForEach(newFacility =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(newFacility.name);
                    if (FlightGlobals.ActiveVessel == null) { GUI.enabled = false; }
                    if (GUILayout.Button("Place"))
                    {
                        newFacility.enabled = true;

                        facility.ConstructedFacilities.Remove(newFacility);

                        string newGroupName = $"{facility.Colony.Name}_{newFacility.GetType().Name}_0_{KCFacilityBase.CountFacilityType(newFacility.GetType(), facility.Colony) + 1}";

                        ColonyBuilding.PlaceNewGroup(newFacility, newGroupName);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Upgrading Facilities:");
                facility.UpgradingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime(pair.Key.level + 1);
                    GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Upgraded Facilities:");
                facility.UpgradedFacilities.ToList().ForEach(facilityUpgrade =>
                {
                    GUILayout.Label(facilityUpgrade.name);

                    if (FlightGlobals.ActiveVessel == null) { GUI.enabled = false; }
                    if (GUILayout.Button("Place upgrade"))
                    {
                        KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facilityUpgrade);
                        facility.UpgradedFacilities.Remove(facilityUpgrade);
                    }
                    GUI.enabled = true;
                });

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }


        public KC_CAB_Window(KC_CAB_Facility facility) : base(Configuration.createWindowID(facility), facility.name)
        {
            this.facility = facility;
            this.toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    public class KC_CAB_Facility : KCFacilityBase
    {
        /// <summary>
        /// All of the default facilties that are queued to be placed after the cab is placed.
        /// </summary>
        public static Dictionary<string, int> defaultFacilities = new Dictionary<string, int>();
        /// <summary>
        /// All of the default facilties that are queued to be placed before the cab is placed.
        /// </summary>
        public static Dictionary<string, int> priorityDefaultFacilities = new Dictionary<string, int>();
        public static void addDefaultFacility(string facilityName, int amount)
        {
            if (!defaultFacilities.ContainsKey(facilityName))
            {
                defaultFacilities.Add(facilityName, amount);
            }
        }
        public static void addPriorityDefaultFacility(string facilityName, int amount)
        {
            if (!priorityDefaultFacilities.ContainsKey(facilityName))
            {
                priorityDefaultFacilities.Add(facilityName, amount);
            }
        }

        private KC_CAB_Window window;

        private Dictionary<KCFacilityBase, double> constructingFacilities = new Dictionary<KCFacilityBase, double>();
        private List<KCFacilityBase> constructedFacilities = new List<KCFacilityBase>();
        internal void addConstructingFacility(KCFacilityBase facility, double time)
        {
            if (!(constructingFacilities.ContainsKey(facility) || constructedFacilities.Contains(facility)))
            {
                constructingFacilities.Add(facility, time);
            }
        }
        internal void addConstructedFacility(KCFacilityBase facility)
        {
            if (constructingFacilities.ContainsKey(facility))
            {
                constructingFacilities.Remove(facility);
            }
            if (!constructedFacilities.Contains(facility))
            {
                constructedFacilities.Add(facility);
            }
        }

        /// <summary>
        /// A dictionary containg all additional group upgrades for facilities.
        /// <para>The additional groups can be placed when the </para>
        /// <para>The key is the facilityUpgrade id</para>
        /// </summary>
        private Dictionary<KCFacilityBase, double> upgradingFacilities = new Dictionary<KCFacilityBase, double>();
        private List<KCFacilityBase> upgradedFacilities = new List<KCFacilityBase>();
        internal void addUpgradingFacility(KCFacilityBase facility, double time)
        {
            if (!(upgradingFacilities.ContainsKey(facility) || upgradedFacilities.Contains(facility)))
            {
                upgradingFacilities.Add(facility, time);
            }
        }
        internal void addUpgradedFacility(KCFacilityBase facility)
        {
            if (upgradingFacilities.ContainsKey(facility))
            {
                upgradingFacilities.Remove(facility);
            }

            if (!upgradedFacilities.Contains(facility))
            {
                upgradedFacilities.Add(facility);
            }
        }

        public Dictionary<KCFacilityBase, double> UpgradingFacilities
        {
            get { return upgradingFacilities; }
        }

        public List<KCFacilityBase> UpgradedFacilities
        {
            get { return upgradedFacilities; }
            set { upgradedFacilities = value; }
        }

        public Dictionary<KCFacilityBase, double> ConstructingFacilities
        {
            get { return constructingFacilities; }
        }
        public List<KCFacilityBase> ConstructedFacilities
        {
            get { return constructedFacilities; }
            set { constructedFacilities = value; }
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            if (upgradingFacilities.Count > 0 || constructingFacilities.Count > 0)
            {
                double totalProduction = Colony.Facilities.Where(f => f is KCProductionFacility).ToList().Sum(facility => ((KCProductionFacility)facility).dailyProduction() * deltaTime / 24 / 60 / 60);

                while (totalProduction > 0)
                {

                    if (upgradingFacilities.Count > 0)
                    {
                        if (upgradingFacilities.ElementAt(0).Value > totalProduction)
                        {
                            upgradingFacilities[upgradingFacilities.ElementAt(0).Key] -= totalProduction;
                            totalProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = upgradingFacilities.ElementAt(0).Key;
                            totalProduction -= upgradingFacilities.ElementAt(0).Value;
                            upgradingFacilities.Remove(facility);

                            switch (facility.upgradeType)
                            {
                                case UpgradeType.withGroupChange:
                                    KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                                    break;
                                case UpgradeType.withoutGroupChange:
                                    KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                                    break;
                                case UpgradeType.withAdditionalGroup:
                                    addUpgradedFacility(facility);
                                    break;
                            }
                        }
                    }
                    else if (constructingFacilities.Count > 0)
                    {
                        if (constructingFacilities.ElementAt(0).Value > totalProduction)
                        {
                            constructingFacilities[constructingFacilities.ElementAt(0).Key] -= totalProduction;
                            totalProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = constructingFacilities.ElementAt(0).Key;
                            totalProduction -= constructingFacilities.ElementAt(0).Value;
                            constructingFacilities.Remove(facility);
                            addConstructedFacility(facility);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            base.Update();
        }

        public void AddUpgradeableFacility(KCFacilityBase facility)
        {
            if (facility.GetUpgradeTime(facility.level + 1) == 0)
            {
                switch (facility.upgradeType)
                {
                    case UpgradeType.withGroupChange:
                        KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                        break;
                    case UpgradeType.withoutGroupChange:
                        KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                        break;
                    case UpgradeType.withAdditionalGroup:
                        addUpgradedFacility(facility);
                        break;
                }
            }
            else
            {
                addUpgradingFacility(facility, facility.GetUpgradeTime(facility.level + 1));
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.GetUpgradeTime(0) == 0)
            {
                addConstructedFacility(facility);
            }
            else
            {
                addConstructingFacility(facility, facility.GetUpgradeTime(0));
            }
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            ConfigNode constructing = new ConfigNode("constructingFacilities");

            foreach (KeyValuePair<KCFacilityBase, double> facility in constructingFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                facilityNode.AddValue("facilityID", facility.Key.id);
                facilityNode.AddValue("remainingTime", facility.Value);

                constructing.AddNode(facilityNode);
            }

            ConfigNode constructed = new ConfigNode("constructedFacilities");

            foreach (KCFacilityBase facility in constructedFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                facilityNode.AddValue("facilityID", facility.id);

                constructed.AddNode(facilityNode);
            }

            ConfigNode upgrading = new ConfigNode("upgradingFacilities");

            foreach (KeyValuePair<KCFacilityBase, double> facility in upgradingFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                facilityNode.AddValue("facilityID", facility.Key.id);
                facilityNode.AddValue("remainingTime", facility.Value);

                upgrading.AddNode(facilityNode);
            }

            ConfigNode upgraded = new ConfigNode("upgradedFacilities");

            foreach (KCFacilityBase facility in upgradedFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                facilityNode.AddValue("facilityID", facility.id);

                upgraded.AddNode(facilityNode);
            }

            node.AddNode(constructing);
            node.AddNode(constructed);
            node.AddNode(upgrading);
            node.AddNode(upgraded);

            return node;
        }

        public override string GetBaseGroupName(int level)
        {
            return "KC_CAB";
        }

        public KC_CAB_Facility(colonyClass colony, ConfigNode node) : base(colony, node, "KCCABFacility", "CAB")
        {
            constructingFacilities = new Dictionary<KCFacilityBase, double>();
            constructedFacilities = new List<KCFacilityBase>();
            upgradingFacilities = new Dictionary<KCFacilityBase, double>();
            upgradedFacilities = new List<KCFacilityBase>();

            foreach (ConfigNode facilityNode in node.GetNode("constructingFacilities").GetNodes("facilityNode"))
            {
                constructingFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))), double.Parse("remainingTime"));
            }
            foreach (ConfigNode facilityNode in node.GetNode("constructedFacilities").GetNodes("facilityNode"))
            {
                constructedFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))));
            }
            foreach (ConfigNode facilityNode in node.GetNode("upgradingFacilities").GetNodes("facilityNode"))
            {
                upgradingFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))), double.Parse("remainingTime"));
            }
            foreach (ConfigNode facilityNode in node.GetNode("upgradedFacilities").GetNodes("facilityNode"))
            {
                upgradedFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))));
            }

            window = new KC_CAB_Window(this);
        }

        public KC_CAB_Facility(colonyClass colony) : base(colony, "KCCABFacility", "CAB")
        {
            constructingFacilities = new Dictionary<KCFacilityBase, double>();
            constructedFacilities = new List<KCFacilityBase>();
            upgradingFacilities = new Dictionary<KCFacilityBase, double>();
            upgradedFacilities = new List<KCFacilityBase>();

            window = new KC_CAB_Window(this);
        }
    }
}
