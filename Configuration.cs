using System.Collections.Generic;
using System.Reflection;
using System.IO;
using KerbalColonies.colonyFacilities;
using KerbalColonies.Serialization;

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
        // Dictionary 3: static uuid as key
        // Dictionary 4: A KCFacility as key and a "dataString" with all of the building params, might change later
        internal static Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<KCFacilityBase, string>>>>> coloniesPerBody = new Dictionary<string, Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<KCFacilityBase, string>>>>> { };

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
                    coloniesPerBody.Add(saveGame.name, new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<KCFacilityBase, string>>>> { });
                }

                foreach (ConfigNode bodyId in nodes[0].GetNode(saveGame.name).GetNodes())
                {
                    if (!coloniesPerBody[saveGame.name].ContainsKey(int.Parse(bodyId.name)))
                    {
                        coloniesPerBody[saveGame.name].Add(int.Parse(bodyId.name), new Dictionary<string, Dictionary<string, Dictionary<KCFacilityBase, string>>> { });
                    }

                    foreach (ConfigNode colonyName in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNodes())
                    {
                        if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)].ContainsKey(colonyName.name))
                        {
                            coloniesPerBody[saveGame.name][int.Parse(bodyId.name)].Add(colonyName.name, new Dictionary<string, Dictionary<KCFacilityBase, string>> { });
                        }

                        foreach (ConfigNode uuid in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNodes())
                        {
                            if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].ContainsKey(uuid.name))
                            {
                                coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name].Add(uuid.name, new Dictionary<KCFacilityBase, string> { });
                            }

                            foreach (ConfigNode KCFacilityNode in nodes[0].GetNode(saveGame.name).GetNode(bodyId.name).GetNode(colonyName.name).GetNode(uuid.name).GetNodes())
                            {
                                KCFacilityBase kcFacility = KCFacilityClassConverter.DeserializeObject(KCFacilityNode.name);

                                if (!coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][uuid.name].ContainsKey(kcFacility))
                                {
                                    coloniesPerBody[saveGame.name][int.Parse(bodyId.name)][colonyName.name][uuid.name].Add(kcFacility, KCFacilityNode.GetValue("dataString"));
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
                return;
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

                            foreach (KCFacilityBase KCFacility in coloniesPerBody[saveGame][bodyId][colonyName][uuid].Keys)
                            {
                                string serializedKCFacility = KCFacilityClassConverter.SerializeObject(KCFacility);
                                if (!nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(uuid).HasNode(serializedKCFacility))
                                {
                                    nodes[0].GetNode(saveGame).GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(uuid).AddNode(serializedKCFacility, "A serialized KCFacility");
                                }
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
