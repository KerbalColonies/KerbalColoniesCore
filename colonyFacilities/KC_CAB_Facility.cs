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

        Vector2 scrollPosFacilities = new Vector2();


        protected override void CustomWindow()
        {
            facility.Update();
            bool playerInColony = facility.PlayerInColony;

            GUILayout.Label("Facilities in this Colony:");

            scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities);
            {
                GUILayout.BeginVertical();
                {

                    facility.Colony.Facilities.Where(fac =>
                        !facility.ConstructingFacilities.ContainsKey(fac)
                    ).ToList().ForEach(colonyFacility =>
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label(colonyFacility.displayName);
                        GUILayout.Label($"Level: {colonyFacility.level.ToString()}");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(colonyFacility.GetFacilityProductionDisplay());
                        if (
                        ((colonyFacility.AllowClick && playerInColony) || (colonyFacility.AllowRemote && !playerInColony))
                        && !facility.ConstructedFacilities.Contains(colonyFacility))
                        {
                            if (GUILayout.Button("Open", GUILayout.Width(200)))
                            {
                                colonyFacility.Update();
                                if (playerInColony) colonyFacility.OnBuildingClicked();
                                else colonyFacility.OnRemoteClicked();
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (colonyFacility.upgradeable && colonyFacility.level < colonyFacility.maxLevel &&
                        !(facility.UpgradingFacilities.ContainsKey(colonyFacility)
                        || facility.UpgradedFacilities.Contains(colonyFacility)
                        || facility.ConstructingFacilities.ContainsKey(colonyFacility)
                        || facility.ConstructedFacilities.Contains(colonyFacility)
                        ))
                        {
                            KCFacilityInfoClass facilityInfo = Configuration.GetInfoClass(colonyFacility.name);
                            if (!facilityInfo.checkResources(colonyFacility.level + 1, facility.Colony) || !playerInColony) GUI.enabled = false;

                            GUILayout.BeginVertical();
                            {
                                facilityInfo.resourceCost[colonyFacility.level + 1].ToList().ForEach(pair =>
                                {
                                    GUILayout.Label($"{pair.Key.displayName}: {pair.Value}");
                                });
                            }
                            GUILayout.EndVertical();

                            if (GUILayout.Button("Upgrade"))
                            {
                                facilityInfo.removeResources(colonyFacility.level + 1, facility.Colony);
                                facility.AddUpgradeableFacility(colonyFacility);
                            }
                        }
                        else if (facility.UpgradedFacilities.Contains(colonyFacility))
                        {
                            if (!playerInColony) { GUI.enabled = false; }
                            if (GUILayout.Button("Place upgrade"))
                            {
                                KCFacilityBase.UpgradeFacilityWithAdditionalGroup(colonyFacility);
                                facility.UpgradedFacilities.Remove(colonyFacility);
                            }
                        }
                        else if (facility.ConstructedFacilities.Contains(colonyFacility))
                        {
                            if (!playerInColony) { GUI.enabled = false; }
                            if (GUILayout.Button("Place"))
                            {
                                colonyFacility.enabled = true;

                                facility.ConstructedFacilities.Remove(colonyFacility);

                                string newGroupName = $"{facility.Colony.Name}_{colonyFacility.name}_0_{colonyFacility.facilityTypeNumber}";

                                ColonyBuilding.PlaceNewGroup(colonyFacility, newGroupName);
                            }
                        }

                        GUI.enabled = true;
                        GUILayout.Space(10);
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        GUILayout.Space(10);
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
            KCProductionFacility.DailyProductions(Colony, out double dailyProduction, out double dailyVesselProduction);
            dailyProduction = (((dailyProduction * deltaTime) / 6) / 60) / 60; // convert from Kerbin days (6 hours) to seconds
            dailyVesselProduction = (((dailyVesselProduction * deltaTime) / 6) / 60) / 60;

            List<StoredVessel> constructingVessel = KCHangarFacility.GetConstructingVessels(Colony);

            if (constructingVessel.Count > 0)
            {
                while (dailyVesselProduction > 0 && constructingVessel.Count > 0)
                {
                    if (constructingVessel[0].vesselBuildTime > dailyVesselProduction)
                    {
                        double buildingVesselMass = (double)(constructingVessel[0].vesselDryMass * (dailyVesselProduction / constructingVessel[0].entireVesselBuildTime));
                        if (!KCHangarFacility.CanBuildVessel(buildingVesselMass, Colony)) break;

                        KCHangarFacility.BuildVessel(buildingVesselMass, Colony);
                        constructingVessel[0].vesselBuildTime -= dailyVesselProduction;
                        if (Math.Round((double)constructingVessel[0].vesselBuildTime, 2) <= 0)
                        {
                            constructingVessel[0].vesselBuildTime = null;
                            constructingVessel[0].entireVesselBuildTime = null;
                            ScreenMessages.PostScreenMessage($"KC: Vessel {constructingVessel[0].vesselName} was fully built on colony {Colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                        }
                        dailyVesselProduction = 0;
                        break;
                    }
                    else
                    {
                        if (constructingVessel[0].vesselBuildTime == null) { constructingVessel.RemoveAt(0); continue; }
                        double buildingVesselMass = (double)(constructingVessel[0].vesselDryMass * (constructingVessel[0].vesselBuildTime / constructingVessel[0].entireVesselBuildTime));
                        if (!KCHangarFacility.CanBuildVessel(buildingVesselMass, Colony)) break;

                        KCHangarFacility.BuildVessel(buildingVesselMass, Colony);
                        dailyVesselProduction -= (double)constructingVessel[0].vesselBuildTime;
                        constructingVessel[0].vesselBuildTime = null;
                        constructingVessel[0].entireVesselBuildTime = null;
                        ScreenMessages.PostScreenMessage($"KC: Vessel {constructingVessel[0].vesselName} was fully built on colony {Colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    }
                }
            }

            dailyProduction += dailyVesselProduction;

            if (upgradingFacilities.Count > 0 || constructingFacilities.Count > 0)
            {

                while (dailyProduction > 0)
                {
                    if (upgradingFacilities.Count > 0)
                    {
                        if (upgradingFacilities.ElementAt(0).Value > dailyProduction)
                        {
                            upgradingFacilities[upgradingFacilities.ElementAt(0).Key] -= dailyProduction;
                            dailyProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = upgradingFacilities.ElementAt(0).Key;
                            dailyProduction -= upgradingFacilities.ElementAt(0).Value;
                            upgradingFacilities.Remove(facility);

                            ScreenMessages.PostScreenMessage($"KC: Facility {facility.displayName} was fully upgraded on colony {Colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);

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
                        if (constructingFacilities.ElementAt(0).Value > dailyProduction)
                        {
                            constructingFacilities[constructingFacilities.ElementAt(0).Key] -= dailyProduction;
                            dailyProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = constructingFacilities.ElementAt(0).Key;
                            dailyProduction -= constructingFacilities.ElementAt(0).Value;
                            constructingFacilities.Remove(facility);
                            addConstructedFacility(facility);
                            ScreenMessages.PostScreenMessage($"KC: Facility {facility.displayName} was fully built on colony {Colony.DisplayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
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
                addUpgradingFacility(facility, facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier);
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

