using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.CabFacility;
using KerbalColonies.Settings;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies
{
    /// <summary>
    /// A container class for colony actions with priorities.
    /// <para>
    /// WARNING: Priorities must be unique!
    /// </para>
    /// </summary>
    public class ColonyAction : IComparable<ColonyAction>, IComparer<ColonyAction>
    {
        public Action<colonyClass> action { get; private set; }
        public int priority { get; private set; }

        public static bool operator ==(ColonyAction action0, ColonyAction action1)
        {
            return action0 is null && action1 is null || action0 is not null && action1 is not null && action0.action == action1.action;
        }

        public static bool operator !=(ColonyAction action0, ColonyAction action1)
        {
            return (action0 is not null || action1 is not null) && (action0 is null || action1 is null || action0.action != action1.action);
        }

        public int CompareTo(ColonyAction other)
        {
            return other == null ? 1 : priority.CompareTo(other.priority);
        }

        public int Compare(ColonyAction x, ColonyAction y)
        {
            if (x == null && y == null) return 0;
            return x == null ? -1 : y == null ? 1 : x.priority.CompareTo(y.priority);
        }

        public override bool Equals(object obj) => obj is ColonyAction action && this.action == action.action;

        public override int GetHashCode() => action.GetHashCode();

        public ColonyAction(Action<colonyClass> action, int priority = 10)
        {
            this.action = action;
            this.priority = priority;
        }
    }

    public class colonyClass : IComparable<colonyClass>, IComparer<colonyClass>
    {
        #region comparison
        public int uniqueID => (BodyID * 100000) + ColonyNumber;

        public int CompareTo(colonyClass other) => uniqueID.CompareTo(other.uniqueID);

        public int Compare(colonyClass x, colonyClass y)
        {
            if (x is null && y is null) return 0;
            else if (x is null) return -1;
            else if (y is null) return 1;
            return x.uniqueID.CompareTo(y.uniqueID);
        }

        public static bool operator ==(colonyClass colony0, colonyClass colony1)
        {
            return colony0 is null && colony1 is null || colony0 is not null && colony1 is not null && colony0.uniqueID == colony1.uniqueID;
        }

        public static bool operator !=(colonyClass colony0, colonyClass colony1)
        {
            return (colony0 is not null || colony1 is not null) && (colony0 is null || colony1 is null || colony0.uniqueID != colony1.uniqueID);
        }

        public override bool Equals(object obj) => obj is colonyClass colony && uniqueID == colony.uniqueID;
        public override int GetHashCode() => uniqueID.GetHashCode();
        #endregion

        /// <summary>
        /// Reversed priority, the lower the number, the higher the priority.
        /// </summary>

        /// <summary>
        /// ColonyPreLoad is called BEFORE the facilities are loaded but AFTER the shared nodes are loaded.
        /// Warning: Priorities must be unique!
        /// </summary>
        public static SortedSet<ColonyAction> ColonyPreLoad = [];
        /// <summary>
        /// ColonyLoad is called AFTER the facilities are loaded.
        /// Warning: Priorities must be unique!
        /// </summary>
        public static SortedSet<ColonyAction> ColonyLoad = [];
        public static SortedSet<ColonyAction> ColonyUpdate = [];
        /// <summary>
        /// ColonyPresave is called BEFORE anything is saved.
        /// Warning: Priorities must be unique!
        /// </summary>
        public static SortedSet<ColonyAction> ColonyPreSave = [];
        /// <summary>
        /// ColonySave is called AFTER the config node is created but BEFORE it is saved to the disk
        /// Warning: Priorities must be unique!
        /// </summary>
        public static SortedSet<ColonyAction> ColonySave = [];

        public static colonyClass GetColony(string name)
        {
            return KCSaveGame.colonyDictionary.Values.SelectMany(x => x).FirstOrDefault(c => c.Name == name);
        }

        public string Name { get; private set; }
        private string displayName;
        public string DisplayName { get => UseCustomDisplayName ? displayName ?? Localizer.Format("#LOC_KC_COLONY_DEFAULT_NAME", BodyName, ColonyNumber) : Localizer.Format("#LOC_KC_COLONY_DEFAULT_NAME", BodyName, ColonyNumber); set { displayName = value; UseCustomDisplayName = true; } }
        public bool UseCustomDisplayName { get; private set; } = false;

        public int ColonyNumber { get; private set; }
        public int BodyID => FlightGlobals.GetBodyByName(BodyName).flightGlobalsIndex;
        public readonly string BodyName;

        public bool currentFrameUpdated { get; set; } = false; // Used to prevent multiple updates in the same frame

        public KC_CAB_Facility CAB { get; private set; }

        public List<KCFacilityBase> Facilities { get; private set; } = [];
        public void AddFacility(KCFacilityBase facility) => Facilities.Add(facility);
        public List<KCBuildableBase> Buildables { get; private set; } = [];
        public void AddBuildable(KCBuildableBase buildable) => Buildables.Add(buildable);
        public List<ConfigNode> sharedColonyNodes { get; set; } = [];

        public ConfigNode CreateConfigNode()
        {
            Configuration.writeLog($"Saving colony {Name} with {Facilities.Count} facilities, {Buildables.Count} buildables and {sharedColonyNodes.Count} shared nodes");

            ColonyPreSave.ToList().ForEach(actionClass => actionClass.action.Invoke(this));

            ConfigNode node = new("colonyClass");
            node.AddValue("name", Name);
            node.AddValue("displayName", DisplayName);
            node.AddValue("useCustomDisplayName", UseCustomDisplayName);
            node.AddValue("colonyNumber", ColonyNumber);

            ConfigNode colonyNodes = new("sharedColonyNodes");
            sharedColonyNodes.ForEach(x => colonyNodes.AddNode(x));
            node.AddNode(colonyNodes);

            ConfigNode CABNode = new("CAB");

            CABNode.AddNode(CAB.getConfigNode());
            node.AddNode(CABNode);

            foreach (KCFacilityBase facility in Facilities)
            {
                try
                {
                    ConfigNode facilityNode = new("facility");

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

            foreach (KCBuildableBase buildable in Buildables)
            {
                try
                {
                    ConfigNode buildableNode = new("buildable");
                    buildableNode.AddNode(buildable.GetConfigNode());
                    node.AddNode(buildableNode);
                }
                catch (Exception e)
                {
                    Configuration.writeLog($"Unable to save the buildable {buildable.Name}: {e}");
                }
            }

            ColonySave.ToList().ForEach(actionClass => actionClass.action.Invoke(this));

            return node;
        }

        public void UpdateColony()
        {
            if (currentFrameUpdated || Configuration.Paused) return; // Prevent multiple updates in the same frame
            ColonyUpdate.ToList().ForEach(actionClass => actionClass.action.Invoke(this));
            currentFrameUpdated = true;
        }

        public static void ColonyUpdateHandler(colonyClass colony)
        {
            Configuration.writeDebug($"Updating colony {colony.Name} with {colony.Facilities.Count} facilities and {colony.sharedColonyNodes.Count} shared nodes.");
            colony.CAB.Update();
            colony.Facilities.ForEach(f => f.Update());
        }

        public colonyClass(string name, KC_CAB_Info CABInfo)
        {
            Name = name;
            BodyName = FlightGlobals.currentMainBody.name;
            ColonyNumber = KCSaveGame.colonyDictionary[FlightGlobals.currentMainBody.name].Count + 1;
            CAB = new KC_CAB_Facility(this, CABInfo);
            Facilities = [];
            Buildables = [];
            sharedColonyNodes = [];

            ColonyPreLoad.ToList().ForEach(actionClass => actionClass.action.Invoke(this));
            ColonyLoad.ToList().ForEach(actionClass => actionClass.action.Invoke(this));
        }

        public colonyClass(ConfigNode node, string bodyName)
        {
            Name = node.GetValue("name");

            UseCustomDisplayName = bool.Parse(node.GetValue("useCustomDisplayName"));
            if (UseCustomDisplayName) DisplayName = node.GetValue("displayName");
            this.BodyName = bodyName;
            ColonyNumber = int.Parse(node.GetValue("colonyNumber"));

            Facilities = [];
            Buildables = [];
            sharedColonyNodes = node.GetNode("sharedColonyNodes").GetNodes().ToList();
            Configuration.writeLog($"Loading colony {Name} with {sharedColonyNodes.Count} shared nodes");
            sharedColonyNodes.ForEach(x => Configuration.writeDebug($"Shared node: {x.name}\n{x}"));

            ColonyPreLoad.ToList().ForEach(actionClass => actionClass.action.Invoke(this));

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
                    Configuration.writeLog($"ConfigNode: {facility}");
                }

            }

            ConfigNode CABNode = node.GetNode("CAB");
            CAB = new KC_CAB_Facility(this, CABNode.GetNodes().First());

            foreach (ConfigNode wrapper in node.GetNodes("buildable"))
            {
                ConfigNode buildableNode = wrapper.GetNode("buildableNode");
                try
                {
                    KCBuildableInfoClass info = Configuration.GetBuildableInfoClass(buildableNode.GetValue("name"));
                    Buildables.Add(Configuration.CreateBuildable(info, this, buildableNode));
                }
                catch (Exception e)
                {
                    Configuration.writeLog($"Unable to load the buildable {buildableNode?.GetValue("name")}: {e}");
                }
            }

            ColonyLoad.ToList().ForEach(actionClass => actionClass.action.Invoke(this));
        }
    }
}