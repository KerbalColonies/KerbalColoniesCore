using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    public class KC_CABInfo : KCFacilityInfoClass
    {
        /// <summary>
        /// All of the default facilties that are queued to be placed after the cab is placed.
        /// </summary>
        public Dictionary<KCFacilityInfoClass, int> defaultFacilities = new Dictionary<KCFacilityInfoClass, int>();
        /// <summary>
        /// All of the default facilties that are queued to be placed before the cab is placed.
        /// </summary>
        public Dictionary<KCFacilityInfoClass, int> priorityDefaultFacilities = new Dictionary<KCFacilityInfoClass, int>();
        public void addDefaultFacility(KCFacilityInfoClass facilityInfo, int amount)
        {
            if (!defaultFacilities.Any(c => facilityInfo.name == c.Key.name))
            {
                defaultFacilities.Add(facilityInfo, amount);
            }
        }
        public void addPriorityDefaultFacility(KCFacilityInfoClass facilityInfo, int amount)
        {
            if (!priorityDefaultFacilities.Any(c => facilityInfo.name == c.Key.name))
            {
                priorityDefaultFacilities.Add(facilityInfo, amount);
            }
        }

        public KC_CABInfo(ConfigNode node) : base(node)
        {

            if (node.HasNode("priorityDefaultFacilities"))
            {
                ConfigNode priorityNode = node.GetNode("priorityDefaultFacilities");
                foreach (ConfigNode.Value v in priorityNode.values)
                {
                    KCFacilityInfoClass priorityInfo = Configuration.GetInfoClass(v.name);
                    if (priorityInfo == null) throw new MissingFieldException($"The priority facility type {v.name} was not found");
                    else addPriorityDefaultFacility(priorityInfo, int.Parse(v.value));
                }
            }

            if (node.HasNode("defaultFacilities"))
            {
                ConfigNode defaultNode = node.GetNode("defaultFacilities");
                foreach (ConfigNode.Value v in defaultNode.values)
                {
                    KCFacilityInfoClass defaultInfo = Configuration.GetInfoClass(v.name);
                    if (defaultInfo == null) throw new MissingFieldException($"The default facility type {v.name} was not found");
                    else addDefaultFacility(defaultInfo, int.Parse(v.value));
                }
            }
        }
    }

    internal class KC_CAB_Window : KCWindowBase
    {
        private KC_CAB_Facility facility;

        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosFacilities = new Vector2();
        Vector2 scrollPosUnfinishedFacilities = new Vector2();


        protected override void CustomWindow()
        {
            facility.Update();
            bool playerInColony = facility.PlayerInColony;

            GUILayout.Label($"Facility types");
            scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes, GUILayout.Height(300));
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

                        facility.AddconstructingFacility(KCFac);
                    }
                    GUILayout.Space(20);
                    GUI.enabled = true;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            GUILayout.Label("Facilities in this Colony:");

            scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities, GUILayout.Height(300));
            {
                GUILayout.BeginVertical();
                {

                    facility.Colony.Facilities.Where(fac =>
                        !facility.UpgradingFacilities.ContainsKey(fac) &&
                        !facility.UpgradedFacilities.Contains(fac) &&
                        !facility.ConstructingFacilities.ContainsKey(fac) &&
                        !facility.ConstructedFacilities.Contains(fac)
                    ).ToList().ForEach(colonyFacility =>
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label(colonyFacility.displayName);
                        GUILayout.Label($"Level: {colonyFacility.level.ToString()}");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(colonyFacility.GetFacilityProductionDisplay());
                        if (colonyFacility.AllowRemote)
                        {
                            if (GUILayout.Button("Open", GUILayout.Width(200)))
                            {
                                colonyFacility.Update();
                                if (playerInColony) colonyFacility.OnBuildingClicked();
                                else colonyFacility.OnRemoteClicked();
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (colonyFacility.upgradeable && colonyFacility.level < colonyFacility.maxLevel)
                        {
                            KCFacilityInfoClass facilityInfo = Configuration.GetInfoClass(colonyFacility.name);

                            if (!facilityInfo.checkResources(colonyFacility.level + 1, facility.Colony)) GUI.enabled = false;

                            GUILayout.BeginVertical();
                            {
                                facilityInfo.resourceCost[colonyFacility.level + 1].ToList().ForEach(pair =>
                                {
                                    GUILayout.Label($"{pair.Key.displayName}: {pair.Value}");
                                });
                            }
                            GUILayout.EndVertical();

                            GUILayout.FlexibleSpace();

                            if (!(facility.UpgradingFacilities.ContainsKey(colonyFacility)
                                || facility.UpgradedFacilities.Contains(colonyFacility)
                                || facility.ConstructingFacilities.ContainsKey(colonyFacility)
                                || facility.ConstructedFacilities.Contains(colonyFacility)
                            ))
                            {
                                if (GUILayout.Button("Upgrade"))
                                {
                                    facilityInfo.removeResources(colonyFacility.level + 1, facility.Colony);
                                    if (colonyFacility.facilityInfo.UpgradeTypes[facility.level + 1] == UpgradeType.withGroupChange)
                                    {
                                        KCFacilityBase.UpgradeFacilityWithGroupChange(colonyFacility);
                                    }
                                    else if (colonyFacility.facilityInfo.UpgradeTypes[facility.level + 1] == UpgradeType.withoutGroupChange)
                                    {
                                        KCFacilityBase.UpgradeFacilityWithoutGroupChange(colonyFacility);
                                    }
                                    else if (colonyFacility.facilityInfo.UpgradeTypes[facility.level + 1] == UpgradeType.withAdditionalGroup)
                                    {
                                        facility.AddUpgradeableFacility(colonyFacility);

                                        //KCFacilityBase.UpgradeFacilityWithAdditionalGroup(colonyFacility);
                                    }
                                }
                            }
                        }

                        GUI.enabled = true;
                    });
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            GUILayout.Label("Unfinished facilities");

            scrollPosUnfinishedFacilities = GUILayout.BeginScrollView(scrollPosUnfinishedFacilities, GUILayout.Height(250));
            {
                GUILayout.Label("Facilities under construction:");
                GUILayout.BeginVertical();
                {
                    facility.ConstructingFacilities.ToList().ForEach(pair =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(pair.Key.name);
                        double max = pair.Key.facilityInfo.UpgradeTimes[0];
                        GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                        GUILayout.EndHorizontal();
                    });

                    GUILayout.Space(10);

                    GUILayout.Label("Facilities waiting to be placed:");
                    facility.ConstructedFacilities.ToList().ForEach(newFacility =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(newFacility.name);
                        if (!playerInColony) { GUI.enabled = false; }
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
                        double max = pair.Key.facilityInfo.UpgradeTimes[pair.Key.level + 1];
                        GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                        GUILayout.EndHorizontal();
                    });

                    GUILayout.Space(10);

                    GUILayout.Label("Upgraded Facilities:");
                    facility.UpgradedFacilities.ToList().ForEach(facilityUpgrade =>
                    {
                        GUILayout.Label(facilityUpgrade.name);

                        if (!playerInColony) { GUI.enabled = false; }
                        if (GUILayout.Button("Place upgrade"))
                        {
                            KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facilityUpgrade);
                            facility.UpgradedFacilities.Remove(facilityUpgrade);
                        }
                        GUI.enabled = true;
                    });
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }


        public KC_CAB_Window(KC_CAB_Facility facility) : base(Configuration.createWindowID(), facility.name)
        {
            this.facility = facility;
            this.toolRect = new Rect(100, 100, 800, 1000);
        }
    }

    public class KC_CAB_Facility : KCFacilityBase
    {
        public bool PlayerInColony { get; private set; }

        private KC_CAB_Window window;

        private ConfigNode cabNode;

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
            if (upgradingFacilities == null)
            {
                constructingFacilities = new Dictionary<KCFacilityBase, double>();
                constructedFacilities = new List<KCFacilityBase>();
                upgradingFacilities = new Dictionary<KCFacilityBase, double>();
                upgradedFacilities = new List<KCFacilityBase>();

                if (cabNode != null)
                {
                    foreach (ConfigNode facilityNode in cabNode.GetNode("constructingFacilities").GetNodes("facilityNode"))
                    {
                        constructingFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))), double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("constructedFacilities").GetNodes("facilityNode"))
                    {
                        constructedFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("upgradingFacilities").GetNodes("facilityNode"))
                    {
                        upgradingFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))), double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("upgradedFacilities").GetNodes("facilityNode"))
                    {
                        upgradedFacilities.Add(KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID"))));
                    }
                }
            }

            if (FlightGlobals.ActiveVessel == null || KKgroups.Count == 0) PlayerInColony = false;
            else
            {
                KerbalKonstructs.Core.StaticInstance staticInstance = KerbalKonstructs.API.GetGroupStatics(KKgroups[0], FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == Configuration.GetBodyIndex(Colony)).name).FirstOrDefault();
                PlayerInColony = Vector3.Distance(KerbalKonstructs.API.GetGameObject(staticInstance.UUID).transform.position, FlightGlobals.ship_position) < 1000 ? true : false;
            }

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

                            switch (facility.facilityInfo.UpgradeTypes[facility.level + 1])
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
            if (facility.facilityInfo.UpgradeTimes[facility.level + 1] == 0)
            {
                switch (facility.facilityInfo.UpgradeTypes[facility.level + 1])
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
                addUpgradingFacility(facility, facility.facilityInfo.UpgradeTimes[facility.level + 1]);
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTimes[0] == 0)
            {
                addConstructedFacility(facility);
            }
            else
            {
                addConstructingFacility(facility, facility.facilityInfo.UpgradeTimes[0]);
            }
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }

        public override void OnRemoteClicked()
        {
            window.Toggle();
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            if (constructingFacilities == null || constructedFacilities == null || upgradingFacilities == null || upgradedFacilities == null)
            {
                constructingFacilities = null;
                constructedFacilities = null;
                upgradingFacilities = null;
                upgradedFacilities = null;

                Update();
            }

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

        public KC_CAB_Facility(colonyClass colony, ConfigNode node) : base(Configuration.GetCABInfoClass(node.GetValue("name")), node)
        {
            this.Colony = colony;

            constructingFacilities = null;
            constructedFacilities = null;
            upgradingFacilities = null;
            upgradedFacilities = null;
            cabNode = node;

            window = new KC_CAB_Window(this);
        }

        public KC_CAB_Facility(colonyClass colony, KC_CABInfo CABInfo) : base(CABInfo)
        {
            this.Colony = colony;

            constructingFacilities = new Dictionary<KCFacilityBase, double>();
            constructedFacilities = new List<KCFacilityBase>();
            upgradingFacilities = new Dictionary<KCFacilityBase, double>();
            upgradedFacilities = new List<KCFacilityBase>();

            window = new KC_CAB_Window(this);
        }
    }
}

