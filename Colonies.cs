using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KerbalKonstructs;
using KerbalKonstructs.Core;
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
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies
{
    internal static class Colonies
    {
        internal static string activeColony = "";
        internal static int colonyCount = 0;

        /// <summary>
        /// This function is called after a group is saved.
        /// All statics from the temporary group (activeColony_temp) get copied to activeColony.
        /// The temporary group gets deleted, the function unregisters itself from the KK groupSave and runs the save function.
        /// </summary>
        internal static void GroupSaved(KerbalKonstructs.Core.GroupCenter groupCenter)
        {
            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(groupCenter.Group);
            // There was a Exception because the AddStaticToGroup changes
            // This ensure that all statics are added
            while (true)
            {
                try
                {
                    foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
                    {
                        Configuration.coloniesPerBody[Configuration.gameNode.name][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][activeColony].Add(instance.UUID, new Dictionary<colonyFacilities.KCFacilityBase, string> { });

                        KerbalKonstructs.API.AddStaticToGroup(instance.UUID, activeColony);
                    }
                    break;
                }
                catch (Exception e) { }

            }

            Configuration.SaveColonies("KCCD");
            KerbalKonstructs.API.RemoveGroup($"{activeColony}_temp");
            KerbalKonstructs.API.UnRegisterOnGroupSaved(GroupSaved);
            KerbalKonstructs.API.Save();
        }

        /// <summary>
        /// This function creates a new colony.
        /// It's meant to be used by the partmodule only.
        /// </summary>
        internal static bool CreateColony()
        {

            if (!Configuration.coloniesPerBody.ContainsKey(Configuration.gameNode.name))
            {
                Configuration.coloniesPerBody.Add(Configuration.gameNode.name, new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<colonyFacilities.KCFacilityBase, string>>>> { { FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody), new Dictionary<string, Dictionary<string, Dictionary<colonyFacilities.KCFacilityBase, string>>> { { $"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}", new Dictionary<string, Dictionary<colonyFacilities.KCFacilityBase, string>> { } } } } });
            }
            else if (Configuration.coloniesPerBody[Configuration.gameNode.name].ContainsKey(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody))){
                colonyCount = Configuration.coloniesPerBody[Configuration.gameNode.name][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count;
                Configuration.coloniesPerBody[Configuration.gameNode.name][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Add($"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}", new Dictionary<string, Dictionary<colonyFacilities.KCFacilityBase, string>> { });
            }
            else
            {
                Configuration.coloniesPerBody[Configuration.gameNode.name].Add(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody), new Dictionary<string, Dictionary<string, Dictionary<colonyFacilities.KCFacilityBase, string>>> { { $"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}", new Dictionary<string, Dictionary<colonyFacilities.KCFacilityBase, string>> { } } });
            }
            activeColony = KerbalKonstructs.API.CreateGroup($"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}");
            EditorGroupPlace("KC_CAB", activeColony); //CAB: Colony Assembly Hub, initial start group
            return true;
        }

        /// <summary>
        /// This function opens the groupeditor and lets the player position the group where they want.
        /// Therefore it creates a temporary group so the entire group can be moved together.
        /// It adds the GroupSaved method to the KK groupsave to transfer the statics over to the main group.
        /// </summary>
        internal static bool EditorGroupPlace(string groupName, string colonyName, int range = int.MaxValue)
        {
            // range isn't working
            KerbalKonstructs.API.SetEditorRange(range);
            activeColony = colonyName;
            KerbalKonstructs.API.CreateGroup($"{colonyName}_temp");
            KerbalKonstructs.API.CopyGroup($"{colonyName}_temp", groupName);
            KerbalKonstructs.API.OpenGroupEditor($"{colonyName}_temp");
            KerbalKonstructs.API.RegisterOnGroupSaved(GroupSaved);
            return true;
        }
    }
}
