using KerbalColonies.colonyFacilities;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

    /// <summary>
    /// Reads and holds configuration parameters
    /// </summary> 
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION)]
    internal class Configuration : ScenarioModule
    {
        ConfigNode loadedNode;

        public override void OnLoad(ConfigNode node)
        {
            loadedNode = node.CreateCopy();
            Configuration.writeDebug(loadedNode.ToString());

            KCLegacySaveWarning.LoadSettings();
            LoadColoniesV3();

            string saveName = HighLogic.CurrentGame.Seed.ToString();
            if (KCLegacySaveWarning.LoadedSaves.ContainsKey(saveName))
            {
                loadedSaveVersion = new Version(3, 0, 0);
                return;
            }


            KCgroups.Clear();
            colonyDictionary.Clear();
            GroupFacilities.Clear();
            ColonyBuilding.buildQueue.Clear();
            LoadConfiguration();
            writeDebug("scenariomodule load");
            writeDebug(node.ToString());
            LoadColoniesV4(node);
        }
        public override void OnSave(ConfigNode node)
        {
            KCLegacySaveWarning.SaveSettings();
            SaveColoniesV3();
            if (KCLegacySaveWarning.LoadedSaves.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
            {
                Configuration.writeLog($"Saving legacy colonies");
                node.AddData(loadedNode);

                Configuration.writeDebug($"loadedNode = {loadedNode.ToString()}");
                Configuration.writeDebug($"node = {node.ToString()}");

                return;
            }

            SaveConfiguration();
            SaveColoniesV4(node);
            writeDebug(node.ToString());
            writeDebug("scenariomodule save");
        }


        internal static List<int> windowIDs { get; private set; } = new List<int> { }; // list of all window IDs

        internal static int createWindowID()
        {
            System.Random random = new System.Random();

            while (true)
            {
                int id = random.Next(0xCC00000, 0xCCFFFFF);
                if (!windowIDs.Contains(id))
                {
                    windowIDs.Add(id);
                    return id;
                }
            }
        }

        private static List<KC_CABInfo> cabTypes = new List<KC_CABInfo>(); // The list of all available CAB types
        public static List<KC_CABInfo> CabTypes { get { return cabTypes; } } // The list of all available CAB types

        public static bool RegisterCabInfo(KC_CABInfo info)
        {
            if (!cabTypes.Contains(info))
            {
                cabTypes.Add(info);
                return true;
            }
            return false;
        }

        public static bool UnregisterCabInfo(KC_CABInfo info)
        {
            if (cabTypes.Contains(info))
            {
                cabTypes.Remove(info);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Facilities that can be built from the CAB must be registered here during startup via the RegisterBuildableFacility method.
        /// </summary>
        private static List<KCFacilityInfoClass> buildableFacilities = new List<KCFacilityInfoClass>();

        public static List<KCFacilityInfoClass> BuildableFacilities { get { return buildableFacilities; } }

        public static bool RegisterBuildableFacility(KCFacilityInfoClass info)
        {
            if (!buildableFacilities.Contains(info))
            {
                buildableFacilities.Add(info);
                return true;
            }
            return false;
        }

        public static bool UnregisterBuildableFacility(KCFacilityInfoClass info)
        {
            if (buildableFacilities.Contains(info))
            {
                buildableFacilities.Remove(info);
                return true;
            }
            return false;
        }

        public static KC_CABInfo GetCABInfoClass(string name)
        {
            return cabTypes.FirstOrDefault(c => c.name == name);
        }

        public static KCFacilityInfoClass GetInfoClass(string name)
        {
            return buildableFacilities.FirstOrDefault(c => c.name == name);
        }

        internal static KCFacilityBase CreateInstance(KCFacilityInfoClass info, colonyClass colony, bool enabled)
        {
            Configuration.writeLog($"Creating a new instance of type {info.name} for {colony.Name} with enabled = {enabled}");
            return (KCFacilityBase)Activator.CreateInstance(info.type, new object[] { colony, info, enabled });
        }

        internal static KCFacilityBase CreateInstance(KCFacilityInfoClass info, colonyClass colony, ConfigNode node)
        {
            Configuration.writeLog($"Loading an instance of type {info.name} for {colony.Name}");
            Configuration.writeDebug($"with node = {node.ToString()}");
            return (KCFacilityBase)Activator.CreateInstance(info.type, new object[] { colony, info, node });
        }

        #region parameters
        // configurable parameters
        public static int MaxColoniesPerBody = 5;              // Limits the amount of colonies per celestial body (planet/moon)
                                                               // set it to zero to disable the limit
        public static float FacilityCostMultiplier = 1.0f; // Multiplier for the cost of the facilities
        public static float FacilityTimeMultiplier = 1.0f; // Multiplier for the time of the facilities
        public static float FacilityRangeMultiplier = 1.0f; // Multiplier for the range of the facilities
        public static float VesselCostMultiplier = 1.0f; // Multiplier for the cost of the vessels
        public static float VesselTimeMultiplier = 1.0f; // Multiplier for the time of the vessels

        public static string baseBody = "Kerbin"; // The name of the celestial body where the KK base groups are located
        public static bool ConfigBaseBody = false; // If false, the base body will be set to the homeworld of the current game, if true, it will be read from the configuration file
        public static bool ClickToOpen = true; // If true, the user can click on the KK statics to open the colony window

#if DEBUG
        public static bool enableLogging = true;            // Enable this only in debug purposes as it floods the logs very much
#else
        public static bool enableLogging = false;           // Enable this only in debug purposes as it floods the logs very much
#endif
        #endregion
        public static bool Paused = false;

        #region savingV3

        public static Version saveVersion = new Version(4, 0, 0);
        public static Version loadedSaveVersion;

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
        internal static Dictionary<string, Dictionary<int, Dictionary<string, ConfigNode>>> KCgroups = new Dictionary<string, Dictionary<int, Dictionary<string, ConfigNode>>> { };

        internal static void AddGroup(int bodyIndex, string groupName, KCFacilityBase faciltiy)
        {
            /*
            if (!KCgroups.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
            {
                KCgroups.Add(HighLogic.CurrentGame.Seed.ToString(), new Dictionary<int, Dictionary<string, ConfigNode>> { { bodyIndex, new Dictionary<string, ConfigNode> { { groupName, faciltiy.GetSharedNode() } } } });
            }
            else if (!KCgroups[HighLogic.CurrentGame.Seed.ToString()].ContainsKey(bodyIndex))
            {
                KCgroups[HighLogic.CurrentGame.Seed.ToString()].Add(bodyIndex, new Dictionary<string, ConfigNode> { { groupName, faciltiy.GetSharedNode() } });
            }
            else if (!KCgroups[HighLogic.CurrentGame.Seed.ToString()][bodyIndex].ContainsKey(groupName))
            {
                KCgroups[HighLogic.CurrentGame.Seed.ToString()][bodyIndex].Add(groupName, faciltiy.GetSharedNode());
            }
            */

            if (!GroupFacilities.ContainsKey(groupName)) GroupFacilities.Add(groupName, faciltiy);
            else GroupFacilities[groupName] = faciltiy;
        }

        /// <summary>
        /// This dictionary contains all of the colonies in the current savegame
        /// </summary>
        internal static Dictionary<int, List<colonyClass>> colonyDictionary = new Dictionary<int, List<colonyClass>> { };

        public static int GetBodyIndex(colonyClass colony)
        {
            return colonyDictionary.FirstOrDefault(c => c.Value.Contains(colony)).Key;
        }

        /// <summary>
        /// This dictionary contains the facility attached to a specific KK group. Used for the on click event of the KK statics
        /// <para>the string is the KK group name</para>
        /// </summary>
        internal static Dictionary<string, KCFacilityBase> GroupFacilities = new Dictionary<string, KCFacilityBase> { };

        public static void LoadColoniesV3()
        {
            // Loaded to delete legacy KK groups
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}ColonyDataV3.cfg";

            ConfigNode node = ConfigNode.Load(path);

            if (node != null && node.GetNodes().Length > 0)
            {
                ConfigNode[] nodes = node.GetNodes();
                foreach (ConfigNode saveGame in nodes[0].GetNodes())
                {
                    if (!KCgroups.ContainsKey(saveGame.name))
                    {
                        KCgroups.Add(saveGame.name, new Dictionary<int, Dictionary<string, ConfigNode>> { });

                        foreach (ConfigNode bodyId in nodes[0].GetNode(saveGame.name).GetNodes())
                        {
                            if (!KCgroups[saveGame.name].ContainsKey(int.Parse(bodyId.name)))
                            {
                                KCgroups[saveGame.name].Add(int.Parse(bodyId.name), new Dictionary<string, ConfigNode> { });
                            }

                            foreach (ConfigNode group in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNodes())
                            {
                                if (!KCgroups[saveGame.name][int.Parse(bodyId.name)].ContainsKey(group.name))
                                {
                                    if (group.nodes.Count > 0) KCgroups[saveGame.name][int.Parse(bodyId.name)].Add(group.name, group.GetNodes().FirstOrDefault());
                                    else KCgroups[saveGame.name][int.Parse(bodyId.name)].Add(group.name, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void LoadColoniesV4(ConfigNode persistentNode)
        {
            Version.TryParse(persistentNode.GetValue("version") ?? "4.0.0", out loadedSaveVersion);
            Configuration.writeLog($"Loaded save version: {loadedSaveVersion}");


            if (loadedSaveVersion.Major == 3 || loadedSaveVersion > saveVersion)
            {
                KCLegacySaveWarning.Instance.Open();
                return;
            }

            if (persistentNode.HasNode("colonyNode"))
            {
                ConfigNode primaryNode = persistentNode.GetNode("colonyNode");
                foreach (ConfigNode bodyNode in primaryNode.GetNodes())
                {
                    colonyDictionary.TryAdd(int.Parse(bodyNode.name), new List<colonyClass> { });
                    foreach (ConfigNode colonyNode in bodyNode.GetNodes())
                    {
                        try
                        {
                            colonyDictionary[int.Parse(bodyNode.name)].Add(new colonyClass(colonyNode));
                        }
                        catch (Exception e)
                        {
                            writeLog($"Error while loading the colony {colonyNode.name} on body {bodyNode.name}: {e}");
                            writeLog(colonyNode.ToString());
                        }
                    }
                }

                colonyDictionary.Values.ToList().ForEach(colonyList => colonyList.ForEach(colony =>
                {
                    colony.CAB.KKgroups.ForEach(group => GroupFacilities.Add(group, colony.CAB));
                    colony.Facilities.ForEach(facility =>
                    {
                        facility.KKgroups.ForEach(group => GroupFacilities.TryAdd(group, facility));
                    });
                }));
            }
        }

        public static void SaveColoniesV3()
        {
            string root = "KCgroups";

            ConfigNode[] nodes = new ConfigNode[1] { new ConfigNode() };

            foreach (KeyValuePair<string, Dictionary<int, Dictionary<string, ConfigNode>>> gameKVP in KCgroups)
            {
                ConfigNode saveGameNode = new ConfigNode(gameKVP.Key, "The savegame name");
                foreach (KeyValuePair<int, Dictionary<string, ConfigNode>> bodyKVP in gameKVP.Value)
                {
                    ConfigNode bodyNode = new ConfigNode(bodyKVP.Key.ToString(), "The celestial body id");
                    foreach (KeyValuePair<string, ConfigNode> groupName in bodyKVP.Value)
                    {
                        ConfigNode groupNode = new ConfigNode(groupName.Key, "The KK group name");
                        if (groupName.Value != null) groupNode.AddNode(groupName.Value);
                        bodyNode.AddNode(groupNode);
                    }
                    saveGameNode.AddNode(bodyNode);
                }
                nodes[0].AddNode(saveGameNode);
            }

            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}ColonyDataV3.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = root;
            node.AddNode(nodes[0]);
            node.Save(path);
        }

        public static void SaveColoniesV4(ConfigNode persistentNode)
        {
            Configuration.writeLog($"Saving {colonyDictionary.SelectMany(x => x.Value).Count()} on {colonyDictionary.Count} bodies");
            int colonyNodeCount = 0;
            int bodyNodeCount = 0;
            ConfigNode ColonyDictionaryNode = new ConfigNode("colonyNode", "The Colony node");
            foreach (KeyValuePair<int, List<colonyClass>> bodyKVP in colonyDictionary)
            {
                ConfigNode bodyNode = new ConfigNode(bodyKVP.Key.ToString(), "The celestial body id");
                foreach (colonyClass colony in bodyKVP.Value)
                {
                    try
                    {
                        ConfigNode colonyNode = colony.CreateConfigNode();
                        bodyNode.AddNode(colonyNode);
                        colonyNodeCount++;
                    }
                    catch (Exception e)
                    {
                        writeLog($"Error while saving the colony {colony.Name} on body {bodyKVP.Key}: {e}");
                        writeLog(colony.ToString());
                    }
                }
                ColonyDictionaryNode.AddNode(bodyNode);
                bodyNodeCount++;
            }
            writeLog($"Saved {colonyNodeCount} colonies on {bodyNodeCount} bodies");

            persistentNode.AddValue("version", saveVersion.ToString());

            persistentNode.AddNode(ColonyDictionaryNode);
        }
        #endregion

        // static parameters
        public const string APP_NAME = "KerbalColonies";

        public static void LoadConfiguration()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}KC.cfg";
            ConfigNode node = ConfigNode.Load(path);

            if (node == null)
            {
                writeLog("No configuration file found, using default values");
                return;
            }

            node = node.GetNode(APP_NAME);

            writeLog("Loading configuration file");
            writeLog(node.ToString());

#if DEBUG
            enableLogging = true;
#else
            bool.TryParse(node.GetValue("enableLogging"), out enableLogging);
#endif

            if (node.HasValue("baseBody"))
            {
                baseBody = node.GetValue("baseBody");
                ConfigBaseBody = true;
            }
            else
            {
                ConfigBaseBody = false;
                baseBody = FlightGlobals.Bodies.First(body => body.isHomeWorld).bodyName;
                Configuration.writeLog($"No baseBody found in configuration, using the homebody: {baseBody}");
            }

            if (node.HasValue("ClickToOpen")) bool.TryParse(node.GetValue("ClickToOpen"), out ClickToOpen);
            else ClickToOpen = true;

            writeLog($"Configuration loaded: enableLogging = {enableLogging}, CLickToOpen = {ClickToOpen}");
        }

        internal static void SaveConfiguration()
        {
            ConfigNode[] nodes = new ConfigNode[1] { new ConfigNode() };

            // config params
            nodes[0].SetValue("enableLogging", enableLogging, "Enable this only in debug purposes as it floods the logs very much", createIfNotFound: true);
            if (ConfigBaseBody) nodes[0].SetValue("baseBody", baseBody, "The name of the celestial body where the KK base groups are located", createIfNotFound: true);
            nodes[0].SetValue("ClickToOpen", ClickToOpen, "If true, the user can click on the KK statics to open the colony window", createIfNotFound: true);

            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}KC.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = APP_NAME;
            node.AddNode(nodes[0]);
            node.Save(path);
        }


        internal static void writeDebug(string text)
        {
            if (Configuration.enableLogging)
            {
                writeLog("Debug: " + text);
            }
        }

        internal static void writeLog(string text)
        {
            KSPLog.print(Configuration.APP_NAME + ": " + text);
        }
    }
}
