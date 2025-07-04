﻿using KerbalColonies.colonyFacilities;
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

namespace KerbalColonies
{
    public class colonyClass
    {
        public static colonyClass GetColony(string name)
        {
            return Configuration.colonyDictionary.Values.SelectMany(x => x).FirstOrDefault(c => c.Name == name);
        }

        public string Name { get; private set; }
        private string displayName;
        public string DisplayName { get => UseCustomDisplayName ? displayName ?? $"{FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == BodyID).bodyName} colony {ColonyNumber}" : $"{FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == BodyID).bodyName} colony {ColonyNumber}"; set { displayName = value; UseCustomDisplayName = true; } }
        public bool UseCustomDisplayName { get; private set; } = false;

        public int ColonyNumber { get; private set; }
        public int BodyID { get; private set; }

        public KC_CAB_Facility CAB { get; private set; }

        public List<KCFacilityBase> Facilities { get; private set; }
        public void AddFacility(KCFacilityBase facility) => Facilities.Add(facility);
        public List<ConfigNode> sharedColonyNodes { get; set; }

        public ConfigNode CreateConfigNode()
        {
            Configuration.writeLog($"Saving colony {Name} with {Facilities.Count} facilites and {sharedColonyNodes.Count} shared nodes");

            ConfigNode node = new ConfigNode("colonyClass");
            node.AddValue("name", Name);
            node.AddValue("displayName", DisplayName);
            node.AddValue("useCustomDisplayName", UseCustomDisplayName);
            node.AddValue("colonyNumber", ColonyNumber);
            node.AddValue("bodyID", BodyID);

            ConfigNode colonyNodes = new ConfigNode("sharedColonyNodes");
            this.sharedColonyNodes.ForEach(x => colonyNodes.AddNode(x));
            node.AddNode(colonyNodes);

            ConfigNode CABNode = new ConfigNode("CAB");

            CABNode.AddNode(CAB.getConfigNode());
            node.AddNode(CABNode);

            foreach (KCFacilityBase facility in Facilities)
            {
                try
                {
                    ConfigNode facilityNode = new ConfigNode("facility");

                    ConfigNode facilityConfigNode = facility.getConfigNode();
                    if (facilityConfigNode.name == "facilityNode")
                    {
                        facilityNode.AddNode(facilityConfigNode);
                        node.AddNode(facilityNode);
                    }
                    else
                    {
                        ConfigFacilityLoader.loaded = false;
                        ConfigFacilityLoader.failedConfigs.Add(facility.GetType().FullName);
                        ConfigFacilityLoader.exceptions.Add(new Exception($"The facility {facility.GetType()} does not use the confignode provided by the KCFacilityBase. This will lead to errors when loading again."));
                    }
                }
                catch (Exception e)
                {
                    Configuration.writeLog($"Unable to save the facility {facility.name}: {e}");
                }
            }
            return node;
        }

        public void UpdateColony()
        {
            CAB.Update();
            Facilities.ForEach(f => f.Update());
            Configuration.writeLog($"Updating colony {Name} with {Facilities.Count} facilities and {sharedColonyNodes.Count} shared nodes.");
        }

        public colonyClass(string name, KC_CABInfo CABInfo)
        {
            Name = name;
            BodyID = FlightGlobals.GetBodyIndex(FlightGlobals.currentMainBody);
            ColonyNumber = Configuration.colonyDictionary[FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count + 1;
            CAB = new KC_CAB_Facility(this, CABInfo);
            Facilities = new List<KCFacilityBase>();
            sharedColonyNodes = new List<ConfigNode>();
        }

        public colonyClass(ConfigNode node)
        {
            Name = node.GetValue("name");

            if (Configuration.loadedSaveVersion == new Version(3, 1, 1))
            {
                UseCustomDisplayName = false;
                BodyID = 3;
                ColonyNumber = 0;
            }
            else
            {
                UseCustomDisplayName = bool.Parse(node.GetValue("useCustomDisplayName"));
                if (UseCustomDisplayName) DisplayName = node.GetValue("displayName");
                BodyID = int.Parse(node.GetValue("bodyID"));
                ColonyNumber = int.Parse(node.GetValue("colonyNumber"));
            }

            Facilities = new List<KCFacilityBase>();
            sharedColonyNodes = node.GetNode("sharedColonyNodes").GetNodes().ToList();
            Configuration.writeLog($"Loading colony {Name} with {sharedColonyNodes.Count} shared nodes");
            sharedColonyNodes.ForEach(x => Configuration.writeDebug($"Shared node: {x.name}\n{x.ToString()}"));

            foreach (ConfigNode facilityNode in node.GetNodes("facility"))
            {
                ConfigNode facility = facilityNode.GetNode("facilityNode");

                try
                {
                    Facilities.Add(Configuration.CreateInstance(
                        Configuration.GetInfoClass(facility.GetValue("name")),
                        this,
                        facility
                    ));
                }
                catch (Exception e)
                {
                    Configuration.writeLog($"Unable to load the facility {facility.name}: {e}");
                    Configuration.writeLog($"ConfigNode: {facility.ToString()}");
                }

                ConfigNode CABNode = node.GetNode("CAB");

                CAB = new KC_CAB_Facility(this, CABNode.GetNodes().First());
            }
        }
    }
}