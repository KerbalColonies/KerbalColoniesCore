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
        internal static KCFacilityBase Facility = null;
        internal static string ColonyName = "";
        internal static string groupName = "";
        internal static int colonyCount = 0;

        /// <summary>
        /// This function is called after a group is saved.
        /// All statics from the temporary group (activeColony_temp) get copied to activeColony.
        /// The temporary group gets deleted, the function unregisters itself from the KK groupSave and runs the save function.
        /// </summary>
        internal static void PlaceNewGroupSave(KerbalKonstructs.Core.GroupCenter groupCenter)
        {
            if (groupCenter.Group != groupName) { return; }

            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(groupName).ToList();
            GroupPlaceHolder gph = new GroupPlaceHolder(groupName, groupCenter.RadialPosition, groupCenter.Orientation, groupCenter.Heading);
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][ColonyName].Add(gph, new Dictionary<string, List<KCFacilityBase>>());

            if (Facility == null)
            {
                throw new Exception("No facility found");
            }

            if (typeof(KC_CAB_Facility).IsAssignableFrom(Facility.GetType()))
            {
                KC_CAB_Facility kC_CAB_Facility = (KC_CAB_Facility)Facility;

                foreach (KeyValuePair<Type, int> kvp in KC_CAB_Facility.defaultFacilities)
                {
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        KCFacilityBase KCFac = Configuration.CreateInstance(kvp.Key, false);
                        kC_CAB_Facility.addConstructedFacility(KCFac);
                    }
                }
            }

            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                instance.ToggleAllColliders(true);
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][ColonyName][gph].Add(instance.UUID, new List<colonyFacilities.KCFacilityBase> { });
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][ColonyName][gph][instance.UUID].Add(Facility);
            }

            Facility.OnGroupPlaced();

            Configuration.saveColonies = true;
            KerbalKonstructs.API.UnRegisterOnGroupSaved(PlaceNewGroupSave);
            KerbalKonstructs.API.Save();
        }

        /// <summary>
        /// This function opens the groupeditor and lets the player position the group where they want.
        /// Therefore it creates a temporary group so the entire group can be moved together.
        /// It adds the PlaceNewGroupSave method to the KK groupsave to transfer the statics over to the main group.
        /// </summary>
        internal static bool PlaceNewGroup(KCFacilityBase facilityType, string fromGroupName, string newGroupName, string colonyName, int range = int.MaxValue)
        {
            Facility = facilityType;
            // range isn't working
            KerbalKonstructs.API.SetEditorRange(range);
            Colonies.ColonyName = colonyName;
            groupName = newGroupName;
            KerbalKonstructs.API.CopyGroup(newGroupName, fromGroupName, fromBodyName: "Kerbin");
            KerbalKonstructs.API.GetGroupStatics(newGroupName).ForEach(instance => instance.ToggleAllColliders(false));
            KerbalKonstructs.API.OpenGroupEditor(newGroupName);
            KerbalKonstructs.API.RegisterOnGroupSaved(PlaceNewGroupSave);
            return true;
        }

        /// <summary>
        /// This function is called after a group from a facility upgrade is saved.
        /// <para>The function unregisters itself from the KK groupSave and runs the save function.</para>
        /// </summary>
        internal static void AddGroupUpdateSave(KerbalKonstructs.Core.GroupCenter groupCenter)
        {
            if (groupCenter.Group != groupName) { return; }

            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(groupName).ToList();
            GroupPlaceHolder gph = new GroupPlaceHolder(groupName, groupCenter.RadialPosition, groupCenter.Orientation, groupCenter.Heading);
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][ColonyName].Add(gph, new Dictionary<string, List<KCFacilityBase>>());

            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                instance.ToggleAllColliders(true);
                Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][ColonyName][gph].Add(instance.UUID, new List<colonyFacilities.KCFacilityBase> { Facility });
            }

            Configuration.saveColonies = true;
            KerbalKonstructs.API.UnRegisterOnGroupSaved(AddGroupUpdateSave);
            KerbalKonstructs.API.Save();
        }

        /// <summary>
        /// This function opens the groupeditor and lets the player position the group where they want.
        /// Therefore it creates a temporary group so the entire group can be moved together.
        /// It adds the PlaceNewGroupSave method to the KK groupsave to transfer the statics over to the main group.
        /// </summary>
        internal static bool AddGroupUpdate(KCFacilityBase facility, string fromGroupName, string newGroupName, string colonyName, int range = int.MaxValue)
        {
            Facility = facility;
            // range isn't working
            KerbalKonstructs.API.SetEditorRange(range);
            ColonyName = colonyName;
            groupName = newGroupName;
            groupName = KerbalKonstructs.API.CreateGroup(newGroupName);
            KerbalKonstructs.API.CopyGroup(newGroupName, fromGroupName, fromBodyName: "Kerbin");
            KerbalKonstructs.API.GetGroupStatics(newGroupName).ForEach(instance => instance.ToggleAllColliders(false));
            KerbalKonstructs.API.OpenGroupEditor(newGroupName);
            KerbalKonstructs.API.RegisterOnGroupSaved(AddGroupUpdateSave);
            Configuration.saveColonies = true;
            return true;
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

            string colonyName = $"KC_{HighLogic.CurrentGame.Seed.ToString()}_{FlightGlobals.currentMainBody.name}_{colonyCount}";
            string groupName = $"{colonyName}_CAB";

            groupName = KerbalKonstructs.API.CreateGroup(groupName);
            Configuration.coloniesPerBody[HighLogic.CurrentGame.Seed.ToString()][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Add(colonyName, new Dictionary<GroupPlaceHolder, Dictionary<string, List<KCFacilityBase>>> { });

            KC_CAB_Facility cab = new KC_CAB_Facility();

            PlaceNewGroup(cab, "KC_CAB", groupName, colonyName); //CAB: Colony Assembly Hub, initial start group
            return true;
        }
    }
}
