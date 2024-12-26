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
        internal static Type FacilityType = null;
        internal static string colonyName = "";
        internal static string groupName = "";
        internal static int colonyCount = 0;

        /// <summary>
        /// This function is called after a group is saved.
        /// All statics from the temporary group (activeColony_temp) get copied to activeColony.
        /// The temporary group gets deleted, the function unregisters itself from the KK groupSave and runs the save function.
        /// </summary>
        internal static void GroupSaved(KerbalKonstructs.Core.GroupCenter groupCenter)
        {
            if (groupCenter.Group != groupName) { return; }

            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(groupName).ToList();
            GroupPlaceHolder gph = new GroupPlaceHolder(groupName, groupCenter.RadialPosition, groupCenter.Orientation, groupCenter.Heading);
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][colonyName].Add(gph, new Dictionary<string, List<KCFacilityBase>>());

            if (FacilityType == null)
            {
                throw new Exception("Invalid facility type");
            }
            KCFacilityBase fac = Configuration.CreateInstance(FacilityType, true, "");

            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][colonyName][gph].Add(instance.UUID, new List<colonyFacilities.KCFacilityBase> { });
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][colonyName][gph][instance.UUID].Add(fac);
            }

            Configuration.SaveColonies();
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

            string colonyName = $"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}";
            string groupName = $"{colonyName}_CAB";

            groupName = KerbalKonstructs.API.CreateGroup(groupName);
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Add(colonyName, new Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>> { });
            EditorGroupPlace(typeof(KC_CAB_Facility), "KC_CAB", groupName, colonyName); //CAB: Colony Assembly Hub, initial start group
            return true;
        }

        /// <summary>
        /// This function opens the groupeditor and lets the player position the group where they want.
        /// Therefore it creates a temporary group so the entire group can be moved together.
        /// It adds the GroupSaved method to the KK groupsave to transfer the statics over to the main group.
        /// </summary>
        internal static bool EditorGroupPlace(Type facilityType, string fromGroupName, string newGroupName, string colonyName, int range = int.MaxValue)
        {
            FacilityType = facilityType;
            // range isn't working
            KerbalKonstructs.API.SetEditorRange(range);
            Colonies.colonyName = colonyName;
            groupName = newGroupName;
            KerbalKonstructs.API.CopyGroup(newGroupName, fromGroupName);
            KerbalKonstructs.API.OpenGroupEditor(newGroupName);
            KerbalKonstructs.API.RegisterOnGroupSaved(GroupSaved);
            return true;
        }
    }
}
