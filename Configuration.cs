using KerbalColonies.colonyFacilities;
using KerbalColonies.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
        // configurable parameters
        // TODO: add the spawnheight to the save/load
        internal static float spawnHeight = 2;                  // The height the active vessel should be set above the surface, this is done to prevent the vessel getting destroyed by the statics
        internal static int maxColoniesPerBody = 3;              // Limits the amount of colonies per celestial body (planet/moon)
                                                                 // set it to zero to disable the limit
        internal static int oreRequiredPerColony = 1000;     // The required amount of ore to start a colony
                                                             // It's planned to change this so different resources can be used
        internal static bool enableLogging = true;            // Enable this only in debug purposes as it floods the logs very much

        // this is the GAME confignode (the confignode from the save file)
        internal static ConfigNode gameNode;

        // saves the colonies per body with
        // Dictionary 0: the SaveGame name (the "name" field in the GAME node) as key
        // Dictionary 1: bodyindex as key
        // Dictionary 2: colonyName as key
        // Dictionary 3: static uuid as key and a KCFacilityBase List as value
        internal static Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<string, List<KCFacilityBase>>>>> coloniesPerBody = new Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<string, List<KCFacilityBase>>>>> { };

        // static parameters
        internal const string APP_NAME = "KerbalColonies";

        public static void LoadConfiguration(string root)
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(root);

            if ((nodes == null) || (nodes.Length == 0))
            {
                return;
            }
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
            nodes[0].SetValue("maxColoniesPerBody", maxColoniesPerBody, "Limits the amount of colonies per celestial body (planet/moon)\n\t// set it to zero to disable the limit", createIfNotFound: true);
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
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(root);

            if ((nodes == null) || (nodes.Length == 0))
            {
                return;
            }

            foreach (ConfigNode saveGame in nodes[0].GetNodes())
            {
                if (!coloniesPerBody.ContainsKey(saveGame.name))
                {
                    coloniesPerBody.Add(saveGame.name, new Dictionary<int, Dictionary<string, Dictionary<string, List<KCFacilityBase>>>> { });
                }

                foreach (ConfigNode bodyId in nodes[0].GetNode(saveGame.name).GetNodes())
                {
                    if (!coloniesPerBody[saveGame.name].ContainsKey(int.Parse(bodyId.name)))
                    {
                        coloniesPerBody[saveGame.name].Add(int.Parse(bodyId.name), new Dictionary<string, Dictionary<string, List<KCFacilityBase>>> { });
                    }

                    foreach (ConfigNode colonyName in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNodes())
                    {
                        if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)].ContainsKey(colonyName.name))
                        {
                            coloniesPerBody[saveGame.name][int.Parse(bodyId.name)].Add(colonyName.name, new Dictionary<string, List<KCFacilityBase>> { });
                        }

                        foreach (ConfigNode uuid in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNodes())
                        {
                            if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].ContainsKey(uuid.name))
                            {
                                coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].Add(uuid.name, new List<KCFacilityBase> { });
                            }

                            foreach (ConfigNode KCFacilityNode in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNode(uuid.name).GetNodes())
                            {
                                string kcFacilityName = $"{KCFacilityNode.name}|{{{KCFacilityNode.GetValue("serializedData")}}}";
                                
                                KCFacilityBase kcFacility = KCFacilityClassConverter.DeserializeObject(kcFacilityName);

                                if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][uuid.name].Contains(kcFacility))
                                {
                                    coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][uuid.name].Add(kcFacility);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static void SaveColonies(string root)
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(root);
            if ((nodes == null) || (nodes.Length == 0))
            {
                nodes = new ConfigNode[1] { new ConfigNode() };
            }

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
                        foreach (string uuid in coloniesPerBody[saveGame][bodyId][colonyName].Keys)
                        {

                            if (!nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).HasNode(uuid))
                            {
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).AddNode(uuid, "A uuid from a KK static");
                            }
                            else
                            {
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(uuid).ClearNodes();
                            }

                            foreach (KCFacilityBase KCFacility in coloniesPerBody[saveGame][bodyId][colonyName][uuid])
                            {
                                string serializedKCFacility = KCFacilityClassConverter.SerializeObject(KCFacility);
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(uuid).AddNode(serializedKCFacility.Split('|')[0], "A serialized KCFacility");
                                nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(uuid).GetNode(serializedKCFacility.Split('|')[0]).AddValue("serializedData", serializedKCFacility.Split('|')[1].Replace("{", "").Replace("}", ""), "Serialized Data from a KC facility");
                            }
                        }
                    }
                }
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/ColonyData.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = "KCCD";
            node.AddNode(nodes[0]);
            node.Save(path);
        }

    }
}
