using KerbalColonies.UI;
using KerbalKonstructs.Modules;
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

        string selectedType;
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosFacilities = new Vector2();
        protected override void CustomWindow()
        {
            CABFacility.Colony.UpdateColony();
            bool playerInColony = CABFacility.PlayerInColony;

            SortedDictionary<string, List<KCFacilityBase>> facilitiesByType = new SortedDictionary<string, List<KCFacilityBase>>();

            void addType(KCFacilityBase facility)
            {
                string category = facility.facilityInfo.category;
                if (!facilitiesByType.ContainsKey(category)) facilitiesByType.Add(category, new List<KCFacilityBase> { facility });
                else if (!facilitiesByType[category].Contains(facility)) facilitiesByType[category].Add(facility);
            }

            CABFacility.Colony.Facilities.Where(facility => !KCProductionFacility.ConstructingFacilities[facility.Colony].ContainsKey(facility)).ToList().ForEach(facility => addType(facility));

            facilitiesByType.ToList().ForEach(kvp => kvp.Value.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName)));

            selectedType = selectedType ?? facilitiesByType.Keys.FirstOrDefault();

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(250));
                scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes);
                {
                    if (selectedType == "CAB") GUI.enabled = false;
                    if (GUILayout.Button($"CAB")) selectedType = "CAB";
                    GUI.enabled = true;

                    facilitiesByType.ToList().ForEach(kvp =>
                    {
                        if (selectedType == kvp.Key) GUI.enabled = false;
                        if (GUILayout.Button($"{kvp.Key} ({kvp.Value.Count})")) selectedType = kvp.Key;
                        GUI.enabled = true;
                    });
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(620));
                GUILayout.Label($"Facilities of type {selectedType} in {CABFacility.Colony.DisplayName}:");
                scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities);
                {
                    GUILayout.Space(10);

                    if (selectedType == "CAB")
                    {
                        GUILayout.BeginVertical(GUILayout.Width(195));
                        {
                            if (KCProductionFacility.ConstructedFacilities[CABFacility.Colony].Contains(CABFacility))
                            {
                                if (!playerInColony) GUI.enabled = false;
                                if (GUILayout.Button("Place"))
                                {
                                    CABFacility.enabled = true;

                                    KCProductionFacility.ConstructedFacilities[CABFacility.Colony].Remove(CABFacility);

                                    string newGroupName = $"{CABFacility.Colony.Name}_{CABFacility.name}_0_{CABFacility.facilityTypeNumber}";

                                    ColonyBuilding.PlaceNewGroup(CABFacility, newGroupName);
                                }
                                GUI.enabled = true;
                            }
                            else if (KCProductionFacility.UpgradedFacilities[CABFacility.Colony].Contains(CABFacility))
                            {
                                if (!playerInColony) GUI.enabled = false;
                                if (GUILayout.Button("Place upgrade"))
                                {
                                    KCFacilityBase.UpgradeFacilityWithAdditionalGroup(CABFacility);
                                    KCProductionFacility.UpgradedFacilities[CABFacility.Colony].Remove(CABFacility);
                                }
                                GUI.enabled = true;
                            }
                            else if (KCProductionFacility.UpgradingFacilities[CABFacility.Colony].Keys.Contains(CABFacility))
                            {
                                GUI.enabled = false;
                                GUILayout.Button("Upgrading...");
                                GUI.enabled = true;
                            }
                            else
                            {
                                if (CABFacility.upgradeable && CABFacility.level < CABFacility.maxLevel)
                                {
                                    if (!CABFacility.facilityInfo.checkResources(CABFacility.level + 1, CABFacility.Colony)) GUI.enabled = false;
                                    if (GUILayout.Button("Upgrade"))
                                    {
                                        Configuration.writeLog($"KC: Upgrading facility {CABFacility.DisplayName} in {CABFacility.Colony.DisplayName} to level {CABFacility.level + 1}");
                                        CABFacility.facilityInfo.removeResources(CABFacility.level + 1, CABFacility.Colony);
                                        CABFacility.AddUpgradeableFacility(CABFacility);
                                    }
                                    GUI.enabled = true;
                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label("Upgrade cost:");
                                        GUILayout.BeginVertical();
                                        {
                                            CABFacility.facilityInfo.resourceCost[CABFacility.level + 1].ToList().ForEach(pair =>
                                            {
                                                GUILayout.Label($"{pair.Key.displayName}: {pair.Value * Configuration.FacilityCostMultiplier}");
                                            });
                                            if (CABFacility.facilityInfo.Funds[CABFacility.level + 1] != 0) GUILayout.Label($"Funds: {CABFacility.facilityInfo.Funds[CABFacility.level + 1] * Configuration.FacilityCostMultiplier}");
                                        }
                                        GUILayout.EndVertical();
                                    }
                                    GUILayout.EndHorizontal();
                                    GUILayout.Label($"Time: {CABFacility.facilityInfo.UpgradeTimes[CABFacility.level + 1] * Configuration.FacilityTimeMultiplier}");
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
                    else
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
                                        if (KCProductionFacility.ConstructedFacilities[facility.Colony].Contains(facility) || (!facility.AllowClick && playerInColony) || (!facility.AllowRemote && !playerInColony))
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
                                    if (KCProductionFacility.ConstructedFacilities[facility.Colony].Contains(facility))
                                    {
                                        if (!playerInColony) GUI.enabled = false;
                                        if (GUILayout.Button("Place"))
                                        {
                                            facility.enabled = true;

                                            KCProductionFacility.ConstructedFacilities[facility.Colony].Remove(facility);

                                            string newGroupName = $"{CABFacility.Colony.Name}_{facility.name}_0_{facility.facilityTypeNumber}";

                                            ColonyBuilding.PlaceNewGroup(facility, newGroupName);
                                        }
                                        GUI.enabled = true;
                                    }
                                    else if (KCProductionFacility.UpgradedFacilities[facility.Colony].Contains(facility))
                                    {
                                        if (!playerInColony) GUI.enabled = false;
                                        if (GUILayout.Button("Place upgrade"))
                                        {
                                            KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facility);
                                            KCProductionFacility.UpgradedFacilities[facility.Colony].Remove(facility);
                                        }
                                        GUI.enabled = true;
                                    }
                                    else if (KCProductionFacility.UpgradingFacilities[facility.Colony].Keys.Contains(facility))
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
                                                Configuration.writeLog($"KC: Upgrading facility {facility.DisplayName} in {CABFacility.Colony.DisplayName} to level {facility.level + 1}");
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

        private bool initialized = false;
        public override void Update()
        {
            if (!initialized)
            {
                initialized = true;
                if (cabNode != null && cabNode.HasNode("constructingFacilities"))
                {
                    foreach (ConfigNode facilityNode in cabNode.GetNode("constructingFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if(facility != null) KCProductionFacility.AddConstructingFacility(facility, double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("constructedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddConstructedFacility(facility);
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("upgradingFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddUpgradingFacility(facility, double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("upgradedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddUpgradedFacility(facility);
                    }
                }
            }

            PlayerInColony = playerNearFacility();

            base.Update();
        }

        public void AddUpgradeableFacility(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier == 0)
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
                        KCProductionFacility.AddUpgradedFacility(facility);
                        break;
                }
            }
            else
            {
                KCProductionFacility.AddUpgradingFacility(facility, facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier);
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier == 0)
            {
                KCProductionFacility.AddConstructedFacility(facility);
            }
            else
            {
                KCProductionFacility.AddConstructingFacility(facility, facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier);
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

            return node;
        }

        public KC_CAB_Facility(colonyClass colony, ConfigNode node) : base(Configuration.GetCABInfoClass(node.GetValue("name")), node)
        {
            this.Colony = colony;

            cabNode = node;

            window = new KC_CAB_Window(this);
        }

        public KC_CAB_Facility(colonyClass colony, KC_CABInfo CABInfo) : base(CABInfo)
        {
            this.Colony = colony;

            initialized = true;

            window = new KC_CAB_Window(this);
        }
    }
}

