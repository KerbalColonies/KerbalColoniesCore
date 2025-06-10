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

        private Dictionary<string, int> defaultFacilityStrings = new Dictionary<string, int>();
        private Dictionary<string, int> priorityDefaultFacilityStrings = new Dictionary<string, int>();

        public override void lateInit()
        {
            priorityDefaultFacilityStrings.ToList().ForEach(priorityFac =>
            {
                KCFacilityInfoClass priorityInfo = Configuration.GetInfoClass(priorityFac.Key);
                if (priorityInfo == null) throw new MissingFieldException($"The priority facility type {priorityFac.Key} was not found");
                else addPriorityDefaultFacility(priorityInfo, priorityFac.Value);
            });

            defaultFacilityStrings.ToList().ForEach(defaultFac =>
            {
                KCFacilityInfoClass defaultInfo = Configuration.GetInfoClass(defaultFac.Key);
                if (defaultInfo == null) throw new MissingFieldException($"The default facility type {defaultFac.Key} was not found");
                else addDefaultFacility(defaultInfo, defaultFac.Value);
            });
        }

        public KC_CABInfo(ConfigNode node) : base(node)
        {

            if (node.HasNode("priorityDefaultFacilities"))
            {
                ConfigNode priorityNode = node.GetNode("priorityDefaultFacilities");
                foreach (ConfigNode.Value v in priorityNode.values)
                {
                    priorityDefaultFacilityStrings.Add(v.name, int.Parse(v.value));

                }
            }

            if (node.HasNode("defaultFacilities"))
            {
                ConfigNode defaultNode = node.GetNode("defaultFacilities");
                foreach (ConfigNode.Value v in defaultNode.values)
                {
                    defaultFacilityStrings.Add(v.name, int.Parse(v.value));
                }
            }
        }
    }

    internal class KC_CAB_Window : KCWindowBase
    {
        private KC_CAB_Facility CABFacility;

        Type selectedType;
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosFacilities = new Vector2();
        protected override void CustomWindow()
        {
            CABFacility.Update();
            bool playerInColony = CABFacility.PlayerInColony;

            SortedDictionary<Type, List<KCFacilityBase>> facilitiesByType = new SortedDictionary<Type, List<KCFacilityBase>>(Comparer<Type>.Create((x, y) => string.Compare(x.FullName, y.FullName)));

            void addType(KCFacilityBase facility)
            {
                Type facilityType = facility.GetType();
                if (!facilitiesByType.ContainsKey(facilityType)) facilitiesByType.Add(facilityType, new List<KCFacilityBase> { facility });
                else if (!facilitiesByType[facilityType].Contains(facility)) facilitiesByType[facilityType].Add(facility);
            }

            CABFacility.Colony.Facilities.Where(facility => !CABFacility.ConstructingFacilities.ContainsKey(facility)).ToList().ForEach(facility => addType(facility));

            facilitiesByType.ToList().ForEach(kvp => kvp.Value.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName)));

            selectedType = selectedType ?? facilitiesByType.Keys.FirstOrDefault();

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(250));
                scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes);
                {
                    facilitiesByType.ToList().ForEach(kvp =>
                    {
                        if (selectedType == kvp.Key) GUI.enabled = false;
                        if (GUILayout.Button($"{kvp.Key.Name} ({kvp.Value.Count})")) selectedType = kvp.Key;
                        GUI.enabled = true;
                    });
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(620));
                GUILayout.Label($"Facilities of type {selectedType.Name} in {CABFacility.Colony.DisplayName}:");
                scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities);
                {
                    GUILayout.Space(10);
                    for (int i = 0; i < facilitiesByType[selectedType].Count; i++)
                    {
                        KCFacilityBase facility = facilitiesByType[selectedType][i];
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical(GUILayout.Width(195));
                            {
                                GUILayout.Label(facility.DisplayName);
                                GUILayout.Label($"Level: {facility.level}");
                                if (facility.AllowClick && playerInColony || facility.AllowRemote && !playerInColony)
                                {
                                    if (CABFacility.ConstructedFacilities.Contains(facility) || (!facility.AllowClick && playerInColony) || (!facility.AllowRemote && !playerInColony))
                                        GUI.enabled = false;

                                    if (GUILayout.Button("Open"))
                                    {
                                        facility.Update();
                                        if (playerInColony) facility.OnBuildingClicked();
                                        else facility.OnRemoteClicked();
                                    }
                                    GUI.enabled = true;
                                }
                            }
                            GUILayout.EndVertical();
                            GUILayout.BeginVertical(GUILayout.Width(195));
                            {
                                GUILayout.Label(facility.GetFacilityProductionDisplay());
                            }
                            GUILayout.EndVertical();
                            GUILayout.BeginVertical(GUILayout.Width(195));
                            {
                                if (CABFacility.ConstructedFacilities.Contains(facility))
                                {
                                    if (!playerInColony) GUI.enabled = false;
                                    if (GUILayout.Button("Place"))
                                    {
                                        facility.enabled = true;

                                        CABFacility.ConstructedFacilities.Remove(facility);

                                        string newGroupName = $"{CABFacility.Colony.Name}_{facility.name}_0_{facility.facilityTypeNumber}";

                                        ColonyBuilding.PlaceNewGroup(facility, newGroupName);
                                    }
                                    GUI.enabled = true;
                                }
                                else if (CABFacility.UpgradedFacilities.Contains(facility))
                                {
                                    if (!playerInColony) GUI.enabled = false;
                                    if (GUILayout.Button("Place upgrade"))
                                    {
                                        KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facility);
                                        CABFacility.UpgradedFacilities.Remove(facility);
                                    }
                                    GUI.enabled = true;
                                }
                                else if (CABFacility.UpgradingFacilities.Keys.Contains(facility))
                                {
                                    GUI.enabled = false;
                                    GUILayout.Button("Upgrading...");
                                    GUI.enabled = true;
                                }
                                else
                                {
                                    if (facility.upgradeable && facility.level < facility.maxLevel)
                                    {
                                        if (!facility.facilityInfo.checkResources(facility.level + 1, CABFacility.Colony)) GUI.enabled = false;
                                        if (GUILayout.Button("Upgrade"))
                                        {
                                            facility.facilityInfo.removeResources(facility.level + 1, CABFacility.Colony);
                                            CABFacility.AddUpgradeableFacility(facility);
                                        }
                                        GUI.enabled = true;
                                        GUILayout.BeginHorizontal();
                                        {
                                            GUILayout.Label("Upgrade cost:");
                                            GUILayout.BeginVertical();
                                            {
                                                facility.facilityInfo.resourceCost[facility.level + 1].ToList().ForEach(pair =>
                                                {
                                                    GUILayout.Label($"{pair.Key.displayName}: {pair.Value * Configuration.FacilityCostMultiplier}");
                                                });
                                                if (facility.facilityInfo.Funds[facility.level + 1] != 0) GUILayout.Label($"Funds: {facility.facilityInfo.Funds[facility.level + 1] * Configuration.FacilityCostMultiplier}");
                                            }
                                            GUILayout.EndVertical();
                                        }
                                        GUILayout.EndHorizontal();
                                        GUILayout.Label($"Time: {facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier}");
                                    }
                                    else
                                    {
                                        GUI.enabled = false;
                                        GUILayout.Button("Max level reached");
                                        GUI.enabled = true;
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                        if (i < facilitiesByType[selectedType].Count - 1)
                        {
                            GUILayout.Space(10);
                            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            GUILayout.Space(10);
                        }
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public KC_CAB_Window(KC_CAB_Facility facility) : base(Configuration.createWindowID(), facility.name)
        {
            this.CABFacility = facility;
            this.toolRect = new Rect(100, 100, 890, 600);
        }
    }

    public class KC_CAB_Facility : KCFacilityBase
    {
        public bool PlayerInColony { get; private set; }

        private KC_CAB_Window window;

        private ConfigNode cabNode;

        private Dictionary<KCFacilityBase, double> constructingFacilities = new Dictionary<KCFacilityBase, double>();
        public Dictionary<KCFacilityBase, double> ConstructingFacilities { get => constructingFacilities; set => constructingFacilities = value; }
        private List<KCFacilityBase> constructedFacilities = new List<KCFacilityBase>();
        public List<KCFacilityBase> ConstructedFacilities { get => constructedFacilities; set => constructedFacilities = value; }
        public void addConstructingFacility(KCFacilityBase facility, double time)
        {
            if (!(constructingFacilities.ContainsKey(facility) || constructedFacilities.Contains(facility)))
            {
                constructingFacilities.Add(facility, time);
            }
        }
        public void addConstructedFacility(KCFacilityBase facility)
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
        public Dictionary<KCFacilityBase, double> UpgradingFacilities { get => upgradingFacilities; set => upgradingFacilities = value; }
        private List<KCFacilityBase> upgradedFacilities = new List<KCFacilityBase>();
        public List<KCFacilityBase> UpgradedFacilities { get => upgradedFacilities; set => upgradedFacilities = value; }
        public void addUpgradingFacility(KCFacilityBase facility, double time)
        {
            if (!(upgradingFacilities.ContainsKey(facility) || upgradedFacilities.Contains(facility)))
            {
                upgradingFacilities.Add(facility, time);
            }
        }
        public void addUpgradedFacility(KCFacilityBase facility)
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
                if (staticInstance == null)
                {
                    PlayerInColony = false;
                    Configuration.writeLog($"KC: Unable to find any static instance for {name} in {Colony.Name} (kkgroup 0: {KKgroups[0]}");
                }
                else
                    PlayerInColony = Vector3.Distance(KerbalKonstructs.API.GetGameObject(staticInstance.UUID).transform.position, FlightGlobals.ship_position) < 1000 ? true : false;
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
                addUpgradingFacility(facility, facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier);
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier == 0)
            {
                addConstructedFacility(facility);
            }
            else
            {
                addConstructingFacility(facility, facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier);
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

