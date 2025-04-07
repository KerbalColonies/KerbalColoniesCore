using KerbalColonies.colonyFacilities;
using KerbalColonies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (C) 2024 AMPW, Halengar

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

    /// <summary>
    /// Reads and holds configuration parameters
    /// </summary> 
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT)]
    internal class Configuration : ScenarioModule
    {
        public override void OnLoad(ConfigNode node)
        {
            KCgroups.Clear();
            colonyDictionary.Clear();
            GroupFacilities.Clear();
            LoadColoniesV3(node);
            writeDebug(node.ToString());
        }
        public override void OnSave(ConfigNode node)
        {
            SaveColoniesV3(node);
            writeDebug(node.ToString());
        }


        internal static Dictionary<int, KCFacilityBase> windowIDs = new Dictionary<int, KCFacilityBase>();

        internal static int createWindowID(KCFacilityBase facility)
        {
            System.Random random = new System.Random();

            while (true)
            {
                int id = random.Next(0xCC00000, 0xCCFFFFF);
                if (!windowIDs.ContainsKey(id))
                {
                    windowIDs.Add(id, facility);
                    return id;
                }
            }
        }

        /// <summary>
        /// Facilities that can be built from the CAB must be registered here during startup via the RegisterBuildableFacility method.
        /// </summary>
        internal static Dictionary<Type, KCFacilityCostClass> BuildableFacilities = new Dictionary<Type, KCFacilityCostClass>();

        internal static bool RegisterBuildableFacility(Type facilityType, KCFacilityCostClass cost)
        {
            if (!BuildableFacilities.ContainsKey(facilityType))
            {
                BuildableFacilities.Add(facilityType, cost);
                return true;
            }
            return false;
        }

        internal static KCFacilityBase CreateInstance(Type t, colonyClass colony, bool enabled)
        {
            return (KCFacilityBase)Activator.CreateInstance(t, new object[] { colony, enabled });
        }

        internal static KCFacilityBase CreateInstance(Type t, colonyClass colony, ConfigNode node)
        {
            return (KCFacilityBase)Activator.CreateInstance(t, new object[] { colony, node });
        }

        // configurable parameters
        private static Type crewQuarterType = typeof(KCCrewQuarters); // The default type for crew quarters, I want that other mods can change this. The only restriction is that it must be derived from KCCrewQuarters
        internal static Type CrewQuarterType { get { return crewQuarterType; } set { if (typeof(KCCrewQuarters).IsAssignableFrom(value)) { crewQuarterType = value; } } }


        internal static float spawnHeight = -5;                  // The height the active vessel should be set above the surface, this is done to prevent the vessel getting destroyed by the statics
        internal static int maxColoniesPerBody = 3;              // Limits the amount of colonies per celestial body (planet/moon)
                                                                 // set it to zero to disable the limit
        internal static int oreRequiredPerColony = 1000;     // The required amount of ore to start a Colony
                                                             // It's planned to change this so different resources can be used
        internal static bool enableLogging = true;            // Enable this only in debug purposes as it floods the logs very much

        // this is the GAME confignode (the confignode from the save file)
        internal static ConfigNode gameNode = HighLogic.CurrentGame.config;

        #region savingV3
        // New saving
        // The current system for creating facilities requieres exactly one facility per group
        // -> Only the group name needs to be saved in the extra file and it only needs to be checked if one of the group statics is clicked
        // 
        // Extra file:
        // Dictionary 0: the SaveGame name (the "name" field in the GAME node) as key
        // Dictionary 1: bodyindex as key
        // Dictionary 1: KK Groups as value
        //
        // Savegame content:
        // Dictionary 0: the SaveGame name (the "name" field in the GAME node) as key
        // Dictionary 1: bodyindex as key
        // Dictionary 2: a Colony class object as key
        // Dictionary 2: facilities with a List of KK groups
        //
        // Additional ram dictionary:
        // Dictionary 0: group names as key and a of facilitiy as value
        // Used for the on click event of the KK statics

        /// <summary>
        /// This dictionary contains all of the groups across all savegames. It's used to disable the groups from other savegames to enable per savegame colonies
        /// </summary>
        internal static Dictionary<string, Dictionary<int, List<string>>> KCgroups = new Dictionary<string, Dictionary<int, List<string>>> { };

        internal static void AddGroup(int bodyIndex, string groupName, KCFacilityBase faciltiy)
        {
            if (!KCgroups.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
            {
                KCgroups.Add(HighLogic.CurrentGame.Seed.ToString(), new Dictionary<int, List<string>> { { bodyIndex, new List<string> { groupName } } });
            }
            else if (!KCgroups[HighLogic.CurrentGame.Seed.ToString()].ContainsKey(bodyIndex))
            {
                KCgroups[HighLogic.CurrentGame.Seed.ToString()].Add(bodyIndex, new List<string> { groupName });
            }
            else if (!KCgroups[HighLogic.CurrentGame.Seed.ToString()][bodyIndex].Contains(groupName))
            {
                KCgroups[HighLogic.CurrentGame.Seed.ToString()][bodyIndex].Add(groupName);
            }

            GroupFacilities.Add(groupName, faciltiy);
        }

        /// <summary>
        /// This dictionary contains all of the colonies in the current savegame
        /// </summary>
        internal static Dictionary<int, List<colonyClass>> colonyDictionary = new Dictionary<int, List<colonyClass>> { };

        /// <summary>
        /// This dictionary contains all of the facilties attached to a specific KK group. Used for the on click event of the KK statics
        /// <para>the string is the KK group name</para>
        /// </summary>
        internal static Dictionary<string, KCFacilityBase> GroupFacilities = new Dictionary<string, KCFacilityBase> { };

        public static void LoadColoniesV3(ConfigNode persistentNode)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/ColonyDataV3.cfg";

            ConfigNode node = ConfigNode.Load(path);

            if ((node == null) || (node.GetNodes().Length == 0))
            {
                return;
            }
            ConfigNode[] nodes = node.GetNodes();

            foreach (ConfigNode saveGame in nodes[0].GetNodes())
            {
                if (!KCgroups.ContainsKey(saveGame.name))
                {
                    KCgroups.Add(saveGame.name, new Dictionary<int, List<string>> { });

                    foreach (ConfigNode bodyId in nodes[0].GetNode(saveGame.name).GetNodes())
                    {
                        if (!KCgroups[saveGame.name].ContainsKey(int.Parse(bodyId.name)))
                        {
                            KCgroups[saveGame.name].Add(int.Parse(bodyId.name), new List<string> { });
                        }

                        foreach (ConfigNode group in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNodes())
                        {
                            if (!KCgroups[saveGame.name][int.Parse(bodyId.name)].Contains(group.name))
                            {
                                KCgroups[saveGame.name][int.Parse(bodyId.name)].Add(group.name);
                            }
                        }
                    }
                }
            }

            if (persistentNode.HasNode("colonyNode"))
            {
                ConfigNode primaryNode = persistentNode.GetNode("colonyNode");
                foreach (ConfigNode bodyNode in primaryNode.GetNodes())
                {
                    colonyDictionary.Add(int.Parse(bodyNode.name), new List<colonyClass> { });
                    foreach (ConfigNode colonyNode in bodyNode.GetNodes())
                    {
                        colonyDictionary[int.Parse(bodyNode.name)].Add(new colonyClass(colonyNode));
                    }
                }

                colonyDictionary.Values.ToList().ForEach(colonyList => colonyList.ForEach(colony =>
                {
                    colony.CAB.KKgroups.ForEach(group => GroupFacilities.Add(group, colony.CAB));
                    colony.Facilities.ForEach(facility =>
                    {
                        facility.KKgroups.ForEach(group => GroupFacilities.Add(group, facility));
                    });
                }));
            }
        }

        public static void SaveColoniesV3(ConfigNode persistentNode)
        {
            string root = "KCgroups";

            ConfigNode[] nodes = new ConfigNode[1] { new ConfigNode() };

            foreach (KeyValuePair<string, Dictionary<int, List<string>>> gameKVP in KCgroups)
            {
                ConfigNode saveGameNode = new ConfigNode(gameKVP.Key, "The savegame name");
                foreach (KeyValuePair<int, List<string>> bodyKVP in gameKVP.Value)
                {
                    ConfigNode bodyNode = new ConfigNode(bodyKVP.Key.ToString(), "The celestial body id");
                    foreach (string groupName in bodyKVP.Value)
                    {
                        bodyNode.AddNode(groupName, "The KK group name");
                    }
                    saveGameNode.AddNode(bodyNode);
                }
                nodes[0].AddNode(saveGameNode);
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/ColonyData.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = root;
            node.AddNode(nodes[0]);
            node.Save(path);

            ConfigNode ColonyDictionaryNode = new ConfigNode("colonyNode", "The Colony node");
            foreach (KeyValuePair<int, List<colonyClass>> bodyKVP in colonyDictionary)
            {
                ConfigNode bodyNode = new ConfigNode(bodyKVP.Key.ToString(), "The celestial body id");
                foreach (colonyClass colony in bodyKVP.Value)
                {
                    ConfigNode colonyNode = colony.CreateConfigNode();
                    bodyNode.AddNode(colonyNode);
                }
                ColonyDictionaryNode.AddNode(bodyNode);
            }

            // potentially usefull in the future if any changes to the saving are necessary
            persistentNode.AddValue("majorVersion", 3);
            persistentNode.AddValue("minorVersion", 0);
            persistentNode.AddValue("fixVersion", 0);

            persistentNode.AddNode(ColonyDictionaryNode);
        }
        #endregion

        // static parameters
        internal const string APP_NAME = "KerbalColonies";

        public static void LoadConfiguration(string root)
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(root);

            if ((nodes == null) || (nodes.Length == 0))
            {
                return;
            }
            float.TryParse(nodes[0].GetValue("spawnHeight"), out spawnHeight);
            int.TryParse(nodes[0].GetValue("maxColoniesPerBody"), out maxColoniesPerBody);
            int.TryParse(nodes[0].GetValue("oreRequiredPerColony"), out oreRequiredPerColony);

            bool.TryParse(nodes[0].GetValue("enableLogging"), out enableLogging);
        }

        internal static void SaveConfiguration(string root)
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(root);
            if ((nodes == null) || (nodes.Length == 0))
            {
                return;
            }

            // config params
            nodes[0].SetValue("spawnHeight", spawnHeight, "The height above the ground at which the active vessel will be set when spawning a new Colony. This is done to prevent the vessel from exploding from the static meshes", createIfNotFound: true);
            nodes[0].SetValue("maxColoniesPerBody", maxColoniesPerBody, "Limits the amount of colonies per celestial body (planet/moon)\n\facilityType// set it to zero to disable the limit", createIfNotFound: true);
            nodes[0].SetValue("oreRequiredPerColony", oreRequiredPerColony, "The required amount of ore to start a Colony", createIfNotFound: true);
            nodes[0].SetValue("enableLogging", enableLogging, "Enable this only in debug purposes as it floods the logs very much", createIfNotFound: true);

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/KC.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = "KC";
            node.AddNode(nodes[0]);
            node.Save(path);
        }

        internal static void writeDebug(string text)
        {
            if (Configuration.enableLogging)
            {
                writeLog(text);
            }
        }

        internal static void writeLog(string text)
        {
            KSPLog.print(Configuration.APP_NAME + ": " + text);
        }
    }
}
