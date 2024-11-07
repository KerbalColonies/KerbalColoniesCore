using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

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


        // saves the colonies per body with
        // Dictionary 0: bodyindex as key
        // Dictionary 1: colonyName as key
        // Dictionary 2: static uuid as key and a string with all of the building params, might change later
        internal static Dictionary<int, Dictionary<string, Dictionary<string, string>>> coloniesPerBody = new Dictionary<int, Dictionary<string, Dictionary<string, string>>> { };

        // static parameters
        internal const string APP_NAME = "KerbalColonies";

        internal static void saveConfiguration(string root)
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

        internal static void saveColonies(string root)
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(root);
            if ((nodes == null) || (nodes.Length == 0))
            {
                return;
            }

            // coloniesPerBody
            foreach (int bodyId in coloniesPerBody.Keys)
            {
                if (!nodes[0].HasNode(bodyId.ToString()))
                {
                    nodes[0].AddNode(bodyId.ToString(), "The celestial body id");
                }
                foreach (string colonyName in coloniesPerBody[bodyId].Keys)
                {
                    if (!nodes[0].GetNode(bodyId.ToString()).HasNode(colonyName))
                    {
                        nodes[0].GetNode(bodyId.ToString()).AddNode(colonyName, "the colony name");
                    }
                    foreach (string uuid in coloniesPerBody[bodyId][colonyName].Keys)
                    {
                        if (!nodes[0].GetNode(bodyId.ToString()).GetNode(colonyName).HasNode(uuid))
                        {
                            nodes[0].GetNode(bodyId.ToString()).GetNode(colonyName).AddNode(uuid, "A uuid from a KK static");
                        }
                        nodes[0].GetNode(bodyId.ToString()).GetNode(colonyName).GetNode(uuid).SetValue("valueString", coloniesPerBody[bodyId][colonyName][uuid]);
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
