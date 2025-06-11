using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public class ColonyUpdateAction : IComparable<ColonyUpdateAction>, IComparer<ColonyUpdateAction>
    {
        public Action<colonyClass> action { get; private set; }
        public int priority { get; private set; }

        public static bool operator ==(ColonyUpdateAction action0, ColonyUpdateAction action1)
        {
            if (ReferenceEquals(null, action0) && ReferenceEquals(null, action1)) return true;
            if (ReferenceEquals(null, action0) || ReferenceEquals(null, action1)) return false;
            else return action0.action == action1.action;
        }

        public static bool operator !=(ColonyUpdateAction action0, ColonyUpdateAction action1)
        {
            if (ReferenceEquals(null, action0) && ReferenceEquals(null, action1)) return false;
            if (ReferenceEquals(null, action0) || ReferenceEquals(null, action1)) return true;
            else return action0.action != action1.action;
        }

        public int CompareTo(ColonyUpdateAction other)
        {
            if (other == null) return 1;
            return priority.CompareTo(other.priority);
        }

        public int Compare(ColonyUpdateAction x, ColonyUpdateAction y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.priority.CompareTo(y.priority);
        }

        public override bool Equals(object obj) => obj is ColonyUpdateAction action && this.action == action.action;

        public override int GetHashCode() => action.GetHashCode();

        public ColonyUpdateAction(Action<colonyClass> action, int priority = 10)
        {
            this.action = action;
            this.priority = priority;
        }
    }

    public class colonyClass : IComparable<colonyClass>, IComparer<colonyClass>
    {
        #region comparison
        public int uniqueID => BodyID * 100000 + ColonyNumber;

        public int CompareTo(colonyClass other) => uniqueID.CompareTo(other.uniqueID);

        public int Compare(colonyClass x, colonyClass y)
        {
            if (ReferenceEquals(null, x) && ReferenceEquals(null, y)) return 0;
            else if (ReferenceEquals(null, x)) return -1;
            else if (ReferenceEquals(null, y)) return 1;
            return x.uniqueID.CompareTo(y.uniqueID);
        }

        public static bool operator ==(colonyClass colony0, colonyClass colony1)
        {
            if (ReferenceEquals(null, colony0) && ReferenceEquals(null, colony1)) return true;
            if (ReferenceEquals(null, colony0) || ReferenceEquals(null, colony1)) return false;
            else return colony0.uniqueID == colony1.uniqueID;
        }

        public static bool operator !=(colonyClass colony0, colonyClass colony1)
        {
            if (ReferenceEquals(null, colony0) && ReferenceEquals(null, colony1)) return false;
            if (ReferenceEquals(null, colony0) || ReferenceEquals(null, colony1)) return true;
            else return colony0.uniqueID != colony1.uniqueID;
        }

        public override bool Equals(object obj) => obj is colonyClass colony && this.uniqueID == colony.uniqueID;
        public override int GetHashCode() => uniqueID.GetHashCode();
        #endregion

        /// <summary>
        /// Reversed priority, the lower the number, the higher the priority.
        /// </summary>
        public static List<ColonyUpdateAction> ColonyUpdate = new List<ColonyUpdateAction> { };

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
            Configuration.writeLog($"Saving colony {Name}");

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

        public void UpdateColony() => ColonyUpdate.ForEach(actionClass => actionClass.action.Invoke(this));

        public static void ColonyUpdateHandler(colonyClass colony)
        {
            Configuration.writeLog($"Updating colony {colony.Name} with {colony.Facilities.Count} facilities and {colony.sharedColonyNodes.Count} shared nodes.");
            colony.CAB.Update();
            colony.Facilities.ForEach(f => f.Update());
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
            Configuration.writeDebug($"Loading colony {Name} with {sharedColonyNodes.Count} shared nodes");
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