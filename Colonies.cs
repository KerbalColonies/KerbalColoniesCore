using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;

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
            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(groupCenter.Group).ToList();
            GroupPlaceHolder gph = new GroupPlaceHolder(activeColony, groupCenter.RadialPosition, groupCenter.Orientation, groupCenter.Heading);
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][activeColony].Add(gph, new Dictionary<string, List<KCFacilityBase>>());
            // There was a Exception because the AddStaticToGroup changes
            // This ensure that all statics are added
            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][activeColony][gph].Add(instance.UUID, new List<colonyFacilities.KCFacilityBase> { });
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][activeColony][gph][instance.UUID].Add(new KCCrewQuarters(true));

                KerbalKonstructs.API.AddStaticToGroup(instance.UUID, activeColony);
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

            if (!Configuration.coloniesPerBody.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
            {
                Configuration.coloniesPerBody.Add(HighLogic.CurrentGame.Seed.ToString(), new Dictionary<int, Dictionary<string, Dictionary<GroupPlaceHolder, Dictionary<string, List<colonyFacilities.KCFacilityBase>>>>> { { FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody), new Dictionary<string, Dictionary<GroupPlaceHolder, Dictionary<string, List<colonyFacilities.KCFacilityBase>>>> { } } });
            }
            else if (!Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()].ContainsKey(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)))
            {
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()].Add(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody), new Dictionary<string, Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>>> { });
            }
            else if (Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count() >= Configuration.maxColoniesPerBody)
            {
                return false;
            }

            colonyCount = Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count();
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Add($"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}", new Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>> { });
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
