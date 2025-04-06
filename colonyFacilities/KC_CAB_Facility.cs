using KerbalColonies.Serialization;
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
            KCFacilityBase.GetInformationByFacilty(facility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);

            GUILayout.BeginScrollView(new Vector2());
            {
                foreach (Type t in Configuration.BuildableFacilities.Keys)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(t.Name);
                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < Configuration.BuildableFacilities[t].resourceCost[0].Count; i++)
                        {
                            GUILayout.Label($"{Configuration.BuildableFacilities[t].resourceCost[0].ElementAt(i).Key.displayName}: {Configuration.BuildableFacilities[t].resourceCost[0].ElementAt(i).Value}");
                        }
                    }
                    GUILayout.EndVertical();

                    if (!KCFacilityCostClass.checkResources(Configuration.BuildableFacilities[t], 0, saveGame, bodyIndex, colonyName)) { GUI.enabled = false; }
                    if (GUILayout.Button("Build"))
                    {
                        KCFacilityCostClass.removeResources(Configuration.BuildableFacilities[t], 0, saveGame, bodyIndex, colonyName);
                        KCFacilityBase KCFac = Configuration.CreateInstance(t, false);

                        facility.AddconstructingFacility(KCFac);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Facilities in this Colony:");

                GUILayout.BeginVertical();

                List<KCFacilityBase> colonyFacilitiyList = Configuration.colonyDictionary[bodyIndex].Find(c => c.Name == colonyName).Facilities;


                colonyFacilitiyList.ForEach(colonyFacility =>
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(colonyFacility.name);
                    GUILayout.Label(colonyFacility.level.ToString());
                    GUILayout.Label(colonyFacility.GetFacilityProductionDisplay());

                    if (colonyFacility.upgradeable && colonyFacility.level < colonyFacility.maxLevel)
                    {
                        if (!KCFacilityCostClass.checkResources(Configuration.BuildableFacilities[colonyFacility.GetType()], colonyFacility.level + 1, saveGame, bodyIndex, colonyName))
                        {
                            GUI.enabled = false;
                        }
                        GUILayout.BeginVertical();
                        {
                            Configuration.BuildableFacilities[colonyFacility.GetType()].resourceCost[colonyFacility.level].ToList().ForEach(pair =>
                            {
                                GUILayout.Label($"{pair.Key.displayName}: {pair.Value}");
                            });

                            if (!(facility.UpgradingFacilities.ContainsKey(colonyFacility) || facility.UpgradedFacilities.Contains(colonyFacility)))
                            {
                                if (GUILayout.Button("Upgrade"))
                                {
                                    KCFacilityCostClass.removeResources(Configuration.BuildableFacilities[colonyFacility.GetType()], colonyFacility.level + 1, saveGame, bodyIndex, colonyName);
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

                        KCFacilityBase.CountFacilityType(newFacility.GetType(), saveGame, bodyIndex, colonyName, out int count);
                        string groupName = $"{colonyName}_{newFacility.GetType().Name}_0_{count + 1}";

                        ColonyBuilding.PlaceNewGroup(newFacility, groupName, colonyName);
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

    [System.Serializable]
    public class KC_CAB_Facility : KCFacilityBase
    {
        /// <summary>
        /// All of the default facilties that are queued to be placed after the cab is placed.
        /// </summary>
        public static Dictionary<Type, int> defaultFacilities = new Dictionary<Type, int>();
        /// <summary>
        /// All of the default facilties that are queued to be placed before the cab is placed.
        /// </summary>
        public static Dictionary<Type, int> priorityDefaultFacilities = new Dictionary<Type, int>();
        public static void addDefaultFacility(Type facilityType, int amount)
        {
            if (typeof(KCFacilityBase).IsAssignableFrom(facilityType))
            {
                if (!defaultFacilities.ContainsKey(facilityType))
                {
                    defaultFacilities.Add(facilityType, amount);
                }
            }
        }
        public static void addPriorityDefaultFacility(Type facilityType, int amount)
        {
            if (typeof(KCFacilityBase).IsAssignableFrom(facilityType))
            {
                if (!priorityDefaultFacilities.ContainsKey(facilityType))
                {
                    priorityDefaultFacilities.Add(facilityType, amount);
                }
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
                double totalProduction = 0;
                KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs);

                List<KCFacilityBase> colonyFacilities = KCFacilityBase.GetFacilitiesInColony(saveGame, bodyIndex, colonyName);
                colonyFacilities.ForEach(facility =>
                {
                    if (typeof(KCProductionFacility).IsAssignableFrom(facility.GetType()))
                    {
                        totalProduction += ((KCProductionFacility)facility).dailyProduction() * deltaTime / 24 / 60 / 60;
                    }
                });

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
            ConfigNode node = new ConfigNode("cabNode");

            ConfigNode constructing = new ConfigNode("constructingFacilities");

            foreach (KeyValuePair<KCFacilityBase, double> facility in constructingFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility.Key).Replace("{", "<<>").Replace("}", "<>>");

                facilityNode.AddValue("facilityString", serializedFacility);
                facilityNode.AddValue("remainingTime", facility.Value);

                ConfigNode customNode = facility.Key.getConfigNode();
                if (customNode != null)
                {
                    facilityNode.AddNode(customNode);
                }

                constructing.AddNode(facilityNode);
            }

            ConfigNode constructed = new ConfigNode("constructedFacilities");

            foreach (KCFacilityBase facility in constructedFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility).Replace("{", "<<>").Replace("}", "<>>");

                facilityNode.AddValue("facilityString", serializedFacility);

                ConfigNode customNode = facility.getConfigNode();
                if (customNode != null)
                {
                    facilityNode.AddNode(facility.getConfigNode());
                }

                constructed.AddNode(facilityNode);
            }

            ConfigNode upgrading = new ConfigNode("upgradingFacilities");

            foreach (KeyValuePair<KCFacilityBase, double> facility in upgradingFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility.Key).Replace("{", "<<>").Replace("}", "<>>");

                facilityNode.AddValue("facilityString", serializedFacility);
                facilityNode.AddValue("remainingTime", facility.Value);

                ConfigNode customNode = facility.Key.getConfigNode();
                if (customNode != null)
                {
                    facilityNode.AddNode(customNode);
                }

                upgrading.AddNode(facilityNode);
            }

            ConfigNode upgraded = new ConfigNode("upgradedFacilities");

            foreach (KCFacilityBase facility in upgradedFacilities)
            {
                ConfigNode facilityNode = new ConfigNode("facilityNode");

                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility).Replace("{", "<<>").Replace("}", "<>>");

                facilityNode.AddValue("facilityString", serializedFacility);

                ConfigNode customNode = facility.getConfigNode();
                if (customNode != null)
                {
                    facilityNode.AddNode(facility.getConfigNode());
                }

                upgraded.AddNode(facilityNode);
            }

            node.AddNode(constructing);
            node.AddNode(constructed);
            node.AddNode(upgrading);
            node.AddNode(upgraded);

            return node;
        }

        public override void loadCustomNode(ConfigNode customNode)
        {
            if (customNode != null)
            {
                if (customNode.HasNode("constructingFacilities"))
                {
                    foreach (ConfigNode facilityNode in customNode.GetNode("constructingFacilities").GetNodes())
                    {
                        KCFacilityBase facility = KCFacilityClassConverter.DeserializeObject(facilityNode.GetValue("facilityString").Replace("<<>", "{").Replace("<>>", "}"));

                        if (facilityNode.GetNodes().Length == 1)
                        {
                            facility.loadCustomNode(facilityNode.GetNodes()[0]);
                        }

                        double time = double.Parse(facilityNode.GetValue("remainingTime"));

                        constructingFacilities.Add(facility, time);
                    }
                }

                if (customNode.HasNode("constructedFacilities"))
                {
                    foreach (ConfigNode facilityNode in customNode.GetNode("constructedFacilities").GetNodes())
                    {
                        KCFacilityBase facility = KCFacilityClassConverter.DeserializeObject(facilityNode.GetValue("facilityString").Replace("<<>", "{").Replace("<>>", "}"));

                        if (facilityNode.GetNodes().Length == 1)
                        {
                            facility.loadCustomNode(facilityNode.GetNodes()[0]);
                        }

                        constructedFacilities.Add(facility);
                    }
                }

                if (customNode.HasNode("upgradingFacilities"))
                {
                    foreach (ConfigNode facilityNode in customNode.GetNode("upgradingFacilities").GetNodes())
                    {
                        KCFacilityBase facility = KCFacilityClassConverter.DeserializeObject(facilityNode.GetValue("facilityString").Replace("<<>", "{").Replace("<>>", "}"));

                        if (facilityNode.GetNodes().Length == 1)
                        {
                            facility.loadCustomNode(facilityNode.GetNodes()[0]);
                        }

                        double time = double.Parse(facilityNode.GetValue("remainingTime"));

                        upgradingFacilities.Add(facility, time);
                    }
                }

                if (customNode.HasNode("upgradedFacilities"))
                {
                    foreach (ConfigNode facilityNode in customNode.GetNode("upgradedFacilities").GetNodes())
                    {
                        KCFacilityBase facility = KCFacilityClassConverter.DeserializeObject(facilityNode.GetValue("facilityString").Replace("<<>", "{").Replace("<>>", "}"));

                        if (facilityNode.GetNodes().Length == 1)
                        {
                            facility.loadCustomNode(facilityNode.GetNodes()[0]);
                        }

                        upgradedFacilities.Add(facility);
                    }
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            window = new KC_CAB_Window(this);
            enabled = true;
        }

        public KC_CAB_Facility() : base("KCCABFacility", true)
        {

        }
    }
}
