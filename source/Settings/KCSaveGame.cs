using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.ProductionFacility;
using KerbalColonies.UI;
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

namespace KerbalColonies.Settings
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION)]
    public class KCSaveGame : ScenarioModule
    {
        private ConfigNode loadedNode;

        public override void OnLoad(ConfigNode node)
        {
            loadedNode = node.CreateCopy();

            KCProductionFacility.ConstructedFacilities.Clear();
            KCProductionFacility.UpgradedFacilities.Clear();
            KCProductionFacility.ProductionQueues.Clear();

            colonyDictionary.Clear();
            GroupFacilities.Clear();
            ColonyBuilding.buildQueue.Clear();
            Configuration.LoadConfiguration();
            Configuration.writeDebug("scenariomodule load");
            Configuration.writeDebug(loadedNode.ToString());
            LoadColoniesV4(loadedNode);
        }

        public override void OnSave(ConfigNode node)
        {
            Configuration.SaveConfiguration();
            SaveColoniesV4(node);
            Configuration.writeDebug(node.ToString());
            Configuration.writeDebug("scenariomodule save");
        }

        #region saving
        public static Version saveVersion = new(4, 1, 0);
        public static Version loadedSaveVersion;

        internal static void AddGroup(int bodyIndex, string groupName, KCFacilityBase faciltiy)
        {
            if (!GroupFacilities.ContainsKey(groupName)) GroupFacilities.Add(groupName, faciltiy);
            else GroupFacilities[groupName] = faciltiy;
        }

        /// <summary>
        /// This dictionary contains all of the colonies in the current savegame
        /// </summary>
        internal static Dictionary<string, List<colonyClass>> colonyDictionary = [];

        public static colonyClass GetColonyByID(int colonyID) => colonyDictionary.SelectMany(c => c.Value).FirstOrDefault(colony => colony.uniqueID == colonyID);

        /// <summary>
        /// This dictionary contains the facility attached to a specific KK group. Used for the on click event of the KK statics
        /// <para>the string is the KK group name</para>
        /// </summary>
        internal static Dictionary<string, KCFacilityBase> GroupFacilities = [];

        public static void LoadColoniesV4(ConfigNode persistentNode)
        {
            Version.TryParse(persistentNode.GetValue("version") ?? "4.0.1", out loadedSaveVersion);
            Configuration.writeLog($"Loaded save version: {loadedSaveVersion}");

            if (persistentNode.HasNode("colonyNode"))
            {
                ConfigNode primaryNode = persistentNode.GetNode("colonyNode");
                foreach (ConfigNode bodyNode in primaryNode.GetNodes())
                {
                    string bodyName = bodyNode.name;

                    colonyDictionary.TryAdd(bodyName, []);
                    foreach (ConfigNode colonyNode in bodyNode.GetNodes())
                    {
                        try
                        {
                            colonyDictionary[bodyName].Add(new colonyClass(colonyNode, bodyName));
                        }
                        catch (Exception e)
                        {
                            Configuration.writeLog($"Error while loading the colony {colonyNode.name} on body {bodyNode.name}: {e}");
                            Configuration.writeLog(colonyNode.ToString());
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

        public static void SaveColoniesV4(ConfigNode persistentNode)
        {
            Configuration.writeLog($"Saving {colonyDictionary.SelectMany(x => x.Value).Count()} on {colonyDictionary.Count} bodies");
            int colonyNodeCount = 0;
            int bodyNodeCount = 0;
            ConfigNode ColonyDictionaryNode = new("colonyNode", "The Colony node");
            foreach (KeyValuePair<string, List<colonyClass>> bodyKVP in colonyDictionary)
            {
                ConfigNode bodyNode = new(bodyKVP.Key, "The celestial body name");
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
                        Configuration.writeLog($"Error while saving the colony {colony.Name} on body {bodyKVP.Key}: {e}");
                        Configuration.writeLog(colony.ToString());
                    }
                }
                ColonyDictionaryNode.AddNode(bodyNode);
                bodyNodeCount++;
            }
            Configuration.writeLog($"Saved {colonyNodeCount} colonies on {bodyNodeCount} bodies");

            persistentNode.AddValue("version", saveVersion.ToString());

            persistentNode.AddNode(ColonyDictionaryNode);
        }
        #endregion
    }
}
