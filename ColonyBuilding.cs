using KerbalColonies.colonyFacilities;
using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
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
    internal static class ColonyBuilding
    {
        internal static Queue<QueueInformation> buildQueue = new Queue<QueueInformation>();
        public static bool placedGroup = false;
        public static bool nextFrame = false;

        internal class QueueInformation
        {
            internal KCFacilityBase Facility = null;
            internal string groupName = null;
            internal string fromGroupName = null;


            internal QueueInformation(KCFacilityBase facility, string groupName, string fromGroupName)
            {
                Facility = facility;
                this.groupName = groupName;
                this.fromGroupName = fromGroupName;
            }
        }

        /// <summary>
        /// This function is called after a group is saved.
        /// the function unregisters itself from the KK groupSave
        /// </summary>
        internal static void PlaceNewGroupSave(KerbalKonstructs.Core.GroupCenter groupCenter)
        {
            if (groupCenter.Group != buildQueue.Peek().groupName) { return; }

            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(buildQueue.Peek().groupName).ToList();

            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                instance.ToggleAllColliders(true);
            }

            buildQueue.Peek().Facility.enabled = true;
            buildQueue.Peek().Facility.OnGroupPlaced();

            KerbalKonstructs.API.UnRegisterOnGroupSaved(PlaceNewGroupSave);
            KerbalKonstructs.API.Save();

            buildQueue.Dequeue();
            placedGroup = true;
            nextFrame = false;
        }

        /// <summary>
        /// This function opens the groupeditor and lets the player position the group where they want.
        /// It adds the PlaceNewGroupSave method to the KK groupsave
        /// It's also used for additional group upgrades.
        /// The facility level must be set correctly before calling this function.
        /// </summary>
        internal static bool PlaceNewGroup(KCFacilityBase facility, string newGroupName)
        {
            QueueInformation buildObj = new QueueInformation(facility, newGroupName, facility.GetBaseGroupName(facility.level));

            buildQueue.Enqueue(buildObj);
            placedGroup = true;
            nextFrame = false;
            return true;
        }

        internal static void QueuePlacer()
        {
            if (buildQueue.Count() > 0)
            {
                KerbalKonstructs.API.CreateGroup(ColonyBuilding.buildQueue.Peek().groupName);
                KerbalKonstructs.API.CopyGroup(ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().fromGroupName, fromBodyName: "Kerbin");
                KerbalKonstructs.API.GetGroupStatics(ColonyBuilding.buildQueue.Peek().groupName).ForEach(instance => instance.ToggleAllColliders(false));
                KerbalKonstructs.API.OpenGroupEditor(ColonyBuilding.buildQueue.Peek().groupName);
                KerbalKonstructs.API.RegisterOnGroupSaved(ColonyBuilding.PlaceNewGroupSave);
                ColonyBuilding.buildQueue.Peek().Facility.KKgroups.Add(ColonyBuilding.buildQueue.Peek().groupName); // add the group to the facility groups
                Configuration.AddGroup(FlightGlobals.GetBodyIndex(FlightGlobals.currentMainBody), ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().Facility);
            }
            ColonyBuilding.placedGroup = false;
        }

        /// <summary>
        /// This function creates a new Colony.
        /// It's meant to be used by the partmodule only.
        /// </summary>
        internal static bool CreateColony()
        {
            if (!Configuration.colonyDictionary.ContainsKey(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)))
            {
                Configuration.colonyDictionary.Add(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody), new List<colonyClass> { });
            }

            int colonyCount = Configuration.colonyDictionary[FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count;

            if (colonyCount > Configuration.maxColoniesPerBody)
            {
                return false;
            }

            string colonyName = $"KC_{HighLogic.CurrentGame.Seed.ToString()}_{FlightGlobals.currentMainBody.name}_{colonyCount}";
            string groupName = $"{colonyName}_CAB";

            colonyClass colony = new colonyClass(colonyName);

            Configuration.colonyDictionary[FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Add(colony);

            KC_CAB_Facility cab = colony.CAB;

            foreach (KeyValuePair<Type, int> kvp in KC_CAB_Facility.priorityDefaultFacilities)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    KCFacilityBase KCFac = Configuration.CreateInstance(kvp.Key, colony, false);
                    string facilityGroupName = $"{colonyName}_{KCFac.GetType().Name}_0_{KCFacilityBase.CountFacilityType(KCFac.GetType(), colony) + 1}";

                    PlaceNewGroup(KCFac, facilityGroupName);
                }
            }

            PlaceNewGroup(cab, groupName); //CAB: colonyClass Assembly Hub, initial start group

            foreach (KeyValuePair<Type, int> kvp in KC_CAB_Facility.defaultFacilities)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    KCFacilityBase KCFac = Configuration.CreateInstance(kvp.Key, colony, false);
                    string facilityGroupName = $"{colonyName}_{KCFac.GetType().Name}_0_{KCFacilityBase.CountFacilityType(KCFac.GetType(), colony) + 1}";

                    PlaceNewGroup(KCFac, facilityGroupName);
                }
            }

            return true;
        }
    }
}
