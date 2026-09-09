using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.CabFacility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

namespace KerbalColonies.Settings
{
    /// <summary>
    /// Reads and holds configuration parameters
    /// </summary> 
    public class Configuration
    {
        internal static List<int> windowIDs { get; private set; } = []; // list of all ColonyChangeWindow IDs

        internal static int createWindowID()
        {
            System.Random random = new();

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

        private static List<KC_CAB_Info> cabTypes = []; // The list of all available CAB types
        public static List<KC_CAB_Info> CabTypes { get { return cabTypes; } } // The list of all available CAB types

        public static bool RegisterCabInfo(KC_CAB_Info info)
        {
            if (!cabTypes.Contains(info))
            {
                cabTypes.Add(info);
                return true;
            }
            return false;
        }

        public static bool UnregisterCabInfo(KC_CAB_Info info)
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
        private static List<KCFacilityInfoClass> buildableFacilities = [];

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

        public static KC_CAB_Info GetCABInfoClass(string name)
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
            Configuration.writeDebug($"with node = {node}");
            return (KCFacilityBase)Activator.CreateInstance(info.type, new object[] { colony, info, node });
        }

        #region parameters
        // configurable parameters
        public static int MaxColoniesPerBody = 5;              // Limits the amount of colonies per celestial body (planet/moon)
                                                               // set it to zero to disable the limit
        public static float FacilityCostMultiplier = 1.0f; // Multiplier for the cost of the facilities
        public static float FacilityTimeMultiplier = 1.0f; // Multiplier for the time of the facilities
        public static float FacilityRangeMultiplier = 1.0f; // Multiplier for the range of the facilities
        public static float EditorRangeMultiplier = 1.0f; // Multiplier for the KC/KK group editor
        public static float ECConsumptionMultiplier = 1.0f;
        public static float VesselCostMultiplier = 1.0f; // Multiplier for the cost of the vessels
        public static float VesselTimeMultiplier = 1.0f; // Multiplier for the time of the vessels

        public static string baseBody = "Kerbin"; // The name of the celestial body where the KK base groups are located
        public static bool ConfigBaseBody = false; // If false, the base body will be set to the homeworld of the current game, if true, it will be read from the configuration file
        public static bool ClickToOpen = true; // If true, the user can click on the KK statics to open the colony ColonyChangeWindow

#if DEBUG
        public static bool enableLogging = true;            // Enable this only in debug purposes as it floods the logs very much
#else
        public static bool enableLogging = false;           // Enable this only in debug purposes as it floods the logs very much
#endif
        #endregion
        public static bool Paused = false;

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
            ConfigNode[] nodes = new ConfigNode[1] { new() };

            // config params
            nodes[0].SetValue("enableLogging", enableLogging, "Enable this only in debug purposes as it floods the logs very much", createIfNotFound: true);
            if (ConfigBaseBody) nodes[0].SetValue("baseBody", baseBody, "The name of the celestial body where the KK base groups are located", createIfNotFound: true);
            nodes[0].SetValue("ClickToOpen", ClickToOpen, "If true, the user can click on the KK statics to open the colony ColonyChangeWindow", createIfNotFound: true);

            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}KC.cfg";

            ConfigNode node = new();
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
