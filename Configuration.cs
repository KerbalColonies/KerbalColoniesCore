using KerbalColonies.colonyFacilities;
using KerbalColonies.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniLinq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a colony system with Kerbal Konstructs statics
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
    internal static class Configuration
    {
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

        internal static KCFacilityBase CreateInstance(Type t, bool enabled, string facilityData)
        {
            return (KCFacilityBase) Activator.CreateInstance(t, new object[] { enabled, facilityData });
        }

        internal static KCFacilityBase CreateInstance(Type t, bool enabled, string facilityData, int facilityLevel)
        {
            return (KCFacilityBase)Activator.CreateInstance(t, new object[] { enabled, facilityData, facilityLevel});
        }

        // configurable parameters
        private static Type crewQuarterType = typeof(KCCrewQuarters); // The default type for crew quarters, I want that other mods can change this. The only restriction is that it must be derived from KCCrewQuarters
        internal static Type CrewQuarterType { get { return crewQuarterType; } set { if (typeof(KCCrewQuarters).IsAssignableFrom(value)) { crewQuarterType = value; } } }


        internal static float spawnHeight = 2;                  // The height the active vessel should be set above the surface, this is done to prevent the vessel getting destroyed by the statics
        internal static int maxColoniesPerBody = 3;              // Limits the amount of colonies per celestial body (planet/moon)
                                                                 // set it to zero to disable the limit
        internal static int oreRequiredPerColony = 1000;     // The required amount of ore to start a colony
                                                             // It's planned to change this so different resources can be used
        internal static bool enableLogging = true;            // Enable this only in debug purposes as it floods the logs very much

        // this is the GAME confignode (the confignode from the save file)
        internal static ConfigNode gameNode = HighLogic.CurrentGame.config;

        // saves the colonies per body with
        // Dictionary 0: the SaveGame name (the "name" field in the GAME node) as key
        // Dictionary 1: bodyindex as key
        // Dictionary 2: colonyName as key
        // Dictionary 3: groupPlaceholder class as key
        // Dictionary 4: static uuid as key and a KCFacilityBase List as value
        internal static Dictionary<string,
            Dictionary<int,
                Dictionary<string,
                    Dictionary<GroupPlaceHolder,
                        Dictionary<string,
                            List<KCFacilityBase>>>>>> coloniesPerBody =

                                new Dictionary<string,
                            Dictionary<int,
                        Dictionary<string,
                    Dictionary<GroupPlaceHolder,
                Dictionary<string,
            List<KCFacilityBase>>>>>>();


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
            nodes[0].SetValue("spawnHeight", spawnHeight, "The height above the ground at which the active vessel will be set when spawning a new colony. This is done to prevent the vessel from exploding from the static meshes", createIfNotFound: true);
            nodes[0].SetValue("maxColoniesPerBody", maxColoniesPerBody, "Limits the amount of colonies per celestial body (planet/moon)\n\facilityType// set it to zero to disable the limit", createIfNotFound: true);
            nodes[0].SetValue("oreRequiredPerColony", oreRequiredPerColony, "The required amount of ore to start a colony", createIfNotFound: true);
            nodes[0].SetValue("enableLogging", enableLogging, "Enable this only in debug purposes as it floods the logs very much", createIfNotFound: true);

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/KC.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = "KC";
            node.AddNode(nodes[0]);
            node.Save(path);
        }
        internal static void LoadColonies(string root)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/ColonyData.cfg";

            ConfigNode node = ConfigNode.Load(path);

            if ((node == null) || (node.GetNodes().Length == 0))
            {
                return;
            }
            ConfigNode[] nodes = node.GetNodes();

            coloniesPerBody = new Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>>>>> { };

            foreach (ConfigNode saveGame in nodes[0].GetNodes())
            {
                if (!coloniesPerBody.ContainsKey(saveGame.name))
                {
                    coloniesPerBody.Add(saveGame.name, new Dictionary<int, Dictionary<string, Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>>>> { });
                }

                foreach (ConfigNode bodyId in nodes[0].GetNode(saveGame.name).GetNodes())
                {
                    if (!coloniesPerBody[saveGame.name].ContainsKey(int.Parse(bodyId.name)))
                    {
                        coloniesPerBody[saveGame.name].Add(int.Parse(bodyId.name), new Dictionary<string, Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>>> { });
                    }

                    foreach (ConfigNode colonyName in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNodes())
                    {
                        if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)].ContainsKey(colonyName.name))
                        {
                            coloniesPerBody[saveGame.name][int.Parse(bodyId.name)].Add(colonyName.name, new Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>> { });
                        }

                        foreach (ConfigNode groupplaceholder in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNodes())
                        {
                            GroupPlaceHolder gph;
                            if (!System.Linq.Enumerable.Any(coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].Keys, g => g.GroupName == groupplaceholder.name))
                            {
                                gph = new GroupPlaceHolder(groupplaceholder.name,
                                    new Vector3(float.Parse(groupplaceholder.GetValue("positionX")), float.Parse(groupplaceholder.GetValue("positionY")), float.Parse(groupplaceholder.GetValue("positionZ"))),
                                    new Vector3(float.Parse(groupplaceholder.GetValue("rotationX")), float.Parse(groupplaceholder.GetValue("rotationY")), float.Parse(groupplaceholder.GetValue("rotationZ"))),
                                    float.Parse(groupplaceholder.GetValue("heading"))
                                );

                                coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].Add(gph, new Dictionary<string, List<KCFacilityBase>> { });
                            }
                            else
                            {
                                gph = System.Linq.Enumerable.FirstOrDefault(coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].Keys, g => g.GroupName == groupplaceholder.name);
                                gph.Position = new Vector3(float.Parse(groupplaceholder.GetValue("positionX")), float.Parse(groupplaceholder.GetValue("positionY")), float.Parse(groupplaceholder.GetValue("positionZ")));
                                gph.Orientation = new Vector3(float.Parse(groupplaceholder.GetValue("rotationX")), float.Parse(groupplaceholder.GetValue("rotationY")), float.Parse(groupplaceholder.GetValue("rotationZ")));
                                gph.Heading = float.Parse(groupplaceholder.GetValue("heading"));
                            }

                            foreach (ConfigNode uuid in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNode(groupplaceholder.name).GetNodes())
                            {
                                if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][gph].ContainsKey(uuid.name))
                                {
                                    coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][gph].Add(uuid.name, new List<KCFacilityBase> { });
                                }

                                foreach (ConfigNode KCFacilityNode in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNode(groupplaceholder.name).GetNode(uuid.name).GetNodes())
                                {
                                    string kcFacilityName = $"{KCFacilityNode.name}/{{{KCFacilityNode.GetValue("serializedData")}}}";

                                    KCFacilityBase kcFacility = KCFacilityClassConverter.DeserializeObject(kcFacilityName);

                                    if (KCFacilityBase.GetFacilityByID(kcFacility.id, out KCFacilityBase fac))
                                    {
                                        coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][gph][uuid.name].Add(fac);
                                    }
                                    else
                                    {
                                        if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][gph][uuid.name].Contains(kcFacility))
                                        {
                                            coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][gph][uuid.name].Add(kcFacility);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static void SaveColonies()
        {
            string root = "KCCD";

            ConfigNode[] nodes = new ConfigNode[1] { new ConfigNode() };

            foreach (string saveGame in coloniesPerBody.Keys)
            {
                if (!nodes[0].HasNode(saveGame))
                {
                    nodes[0].AddNode(saveGame, "The savegame name");
                }
                foreach (int bodyId in coloniesPerBody[saveGame].Keys)
                {
                    if (!nodes[0].GetNode(saveGame).HasNode(bodyId.ToString()))
                    {
                        nodes[0].GetNode(saveGame).AddNode(bodyId.ToString(), "The celestial body id");
                    }
                    foreach (string colonyName in coloniesPerBody[saveGame][bodyId].Keys)
                    {
                        if (!nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).HasNode(colonyName))
                        {
                            nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).AddNode(colonyName, "the colony name");
                        }
                        foreach (GroupPlaceHolder gph in coloniesPerBody[saveGame][bodyId][colonyName].Keys)
                        {
                            if (!nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).HasNode(gph.GroupName))
                            {
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).AddNode(gph.GroupName, "The node for groupPlaceholders, it contains some necessary information for upgrading the facilities. DON'T CHANGE THE VALUES HERE!");
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("positionX", gph.Position.x);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("positionY", gph.Position.y);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("positionZ", gph.Position.z);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("rotationX", gph.Orientation.x);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("rotationY", gph.Orientation.y);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("rotationZ", gph.Orientation.z);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddValue("heading", gph.Heading);
                            }

                            foreach (string uuid in coloniesPerBody[saveGame][bodyId][colonyName][gph].Keys)
                            {

                                if (!nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).HasNode(uuid))
                                {
                                    nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).AddNode(uuid, "A uuid from a KK static");
                                }
                                else
                                {
                                    nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).GetNode(uuid).ClearNodes();
                                }

                                foreach (KCFacilityBase KCFacility in coloniesPerBody[saveGame][bodyId][colonyName][gph][uuid])
                                {
                                    string serializedKCFacility = KCFacilityClassConverter.SerializeObject(KCFacility);
                                    nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).GetNode(uuid).AddNode(serializedKCFacility.Split('/')[0], "A serialized KCFacility");
                                    nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(gph.GroupName).GetNode(uuid).GetNode(serializedKCFacility.Split('/')[0]).AddValue("serializedData", serializedKCFacility.Split('/')[1].Replace("{", "").Replace("}", ""), "Serialized Data from a KC facility");
                                }
                            }
                        }
                    }
                }
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/ColonyData.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = root;
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
