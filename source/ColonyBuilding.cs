using KerbalColonies.colonyFacilities;
using KerbalColonies.UI;
using KerbalKonstructs;
using KerbalKonstructs.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

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
    internal class CABSelectorWindow : KCWindowBase
    {
        private static CABSelectorWindow instance = null;
        internal static CABSelectorWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CABSelectorWindow();
                }
                return instance;
            }
        }

        public static bool checkVesselResources(KCFacilityInfoClass info)
        {
            Configuration.writeLog($"Checking resources for {info.displayName}");
            bool insufficientResources = false;
            foreach (KeyValuePair<PartResourceDefinition, double> resource in info.resourceCost[0])
            {
                double vesselAmount = 0;

                FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);
                vesselAmount = amount;

                Configuration.writeLog($"{resource.Key.displayName}: {vesselAmount} / {resource.Value * Configuration.FacilityCostMultiplier}");

                if (vesselAmount >= resource.Value * Configuration.FacilityCostMultiplier) continue;
                else
                {
                    Configuration.writeLog($"Insufficient {resource.Key.displayName} resources on vessel.");
                    ScreenMessages.PostScreenMessage($"KC: {vesselAmount:f2}/{(resource.Value * Configuration.FacilityCostMultiplier):f2} {resource.Key.displayName}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    insufficientResources = true;
                }
            }

            if (Funding.Instance != null)
            {
                Configuration.writeLog($"Funds: {Funding.Instance.Funds:f2} / {(info.Funds[0] * Configuration.FacilityCostMultiplier):f2}");
                if (Funding.Instance.Funds < info.Funds[0] * Configuration.FacilityCostMultiplier)
                {
                    ScreenMessages.PostScreenMessage($"KC: {Funding.Instance.Funds}/{info.Funds[0] * Configuration.FacilityCostMultiplier} Funds", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    insufficientResources = true;
                }
            }

            return !insufficientResources;
        }

        public static void removeVesselResources(KCFacilityInfoClass info)
        {
            foreach (KeyValuePair<PartResourceDefinition, double> resource in info.resourceCost[0])
            {
                FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, resource.Value * Configuration.FacilityCostMultiplier, true);
            }

            if (Funding.Instance != null)
            {
                Funding.Instance.AddFunds(-info.Funds[0] * Configuration.FacilityCostMultiplier, TransactionReasons.None);
            }
        }

        private Vector2 scrollPosition = new Vector2(0, 0);
        protected override void CustomWindow()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                foreach (KC_CABInfo info in Configuration.CabTypes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{info.displayName}\t");
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < info.resourceCost[0].Count; i++)
                        {
                            GUILayout.Label($"{info.resourceCost[0].ElementAt(i).Key.displayName}: {info.resourceCost[0].ElementAt(i).Value}");
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    GUILayout.Label($"Funds: {(info.Funds.Count > 0 ? info.Funds[0] : 0)}");
                    //GUILayout.Label($"Electricity: {t.Electricity}");
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    if (!checkVesselResources(info)) { GUI.enabled = false; }

                    if (GUILayout.Button("Build"))
                    {
                        removeVesselResources(info);
                        ColonyBuilding.BuildColony(info);
                        Close();
                    }
                    GUILayout.Space(20);
                    GUI.enabled = true;
                }
            }
            GUILayout.EndScrollView();
        }

        internal CABSelectorWindow() : base(Configuration.createWindowID(), "Select a CAB")
        {
            toolRect = new Rect(100, 100, 500, 400);
        }
    }

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

            KerbalKonstructs.API.UnRegisterOnGroupSaved(PlaceNewGroupSave);
            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(buildQueue.Peek().groupName).ToList();

            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                instance.ToggleAllColliders(true);
            }

            buildQueue.Peek().Facility.enabled = true;
            buildQueue.Peek().Facility.OnGroupPlaced(KCGroupEditor.selectedGroup);

            KerbalKonstructs.API.Save();

            KCGroupEditor.selectedGroup = null;
            KCGroupEditor.selectedFacility = null;

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
            ColonyBuilding.placedGroup = false;
            if (buildQueue.Count() > 0)
            {
                KerbalKonstructs.API.RemoveGroup(ColonyBuilding.buildQueue.Peek().groupName); // remove the group if it exists
                KerbalKonstructs.API.CreateGroup(ColonyBuilding.buildQueue.Peek().groupName);
                KerbalKonstructs.API.CopyGroup(ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().fromGroupName, fromBodyName: Configuration.baseBody);
                KerbalKonstructs.API.GetGroupStatics(ColonyBuilding.buildQueue.Peek().groupName).ForEach(instance => instance.ToggleAllColliders(false));

                EditorGUI.CloseEditors();
                MapDecalEditor.Instance.Close();
                GroupEditor.instance.Close();
                GroupEditor.selectedGroup = API.GetGroupCenter(ColonyBuilding.buildQueue.Peek().groupName);
                UI.KCGroupEditor.selectedFacility = ColonyBuilding.buildQueue.Peek().Facility;
                UI.KCGroupEditor.KCInstance.Open();

                KerbalKonstructs.API.RegisterOnGroupSaved(ColonyBuilding.PlaceNewGroupSave);
                ColonyBuilding.buildQueue.Peek().Facility.KKgroups.Add(ColonyBuilding.buildQueue.Peek().groupName); // add the group to the facility groups
                Configuration.AddGroup(FlightGlobals.GetBodyIndex(FlightGlobals.currentMainBody), ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().Facility);
            }
        }

        /// <summary>
        /// This function creates a new Colony.
        /// It's meant to be used by the partmodule only.
        /// 0 = success, 1 = insufficient resources, 2 = too many colonies, 3 cabselector open
        /// </summary>
        internal static int CreateColony()
        {
            if (CABSelectorWindow.Instance.IsOpen()) { return 3; }

            if (!Configuration.colonyDictionary.ContainsKey(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)))
            {
                Configuration.colonyDictionary.Add(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody), new List<colonyClass> { });
            }

            int colonyCount = Configuration.colonyDictionary[FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count;

            if (colonyCount >= Configuration.MaxColoniesPerBody && Configuration.MaxColoniesPerBody != 0)
            {
                return 2;
            }

            if (Configuration.CabTypes.Count == 1)
            {
                if (!CABSelectorWindow.checkVesselResources(Configuration.CabTypes[0])) { return 1; }
                KC_CABInfo info = Configuration.CabTypes[0];
                CABSelectorWindow.removeVesselResources(info);
                BuildColony(info);
            }
            else CABSelectorWindow.Instance.Open();

            return 0;
        }

        internal static void BuildColony(KC_CABInfo CABInfo)
        {
            int colonyCount = Configuration.colonyDictionary[FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Count + 1;

            string colonyName = $"KC_{HighLogic.CurrentGame.Seed.ToString()}_{FlightGlobals.currentMainBody.name}_{colonyCount}";
            string groupName = $"{colonyName}_CAB";

            colonyClass colony = new colonyClass(colonyName, CABInfo);

            Configuration.colonyDictionary[FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].Add(colony);

            KC_CAB_Facility cab = colony.CAB;

            foreach (KeyValuePair<KCFacilityInfoClass, int> kvp in CABInfo.priorityDefaultFacilities)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    KCFacilityBase KCFac = Configuration.CreateInstance(kvp.Key, colony, false);
                    string facilityGroupName = $"{colonyName}_{KCFac.name}_0_{KCFac.facilityTypeNumber}";

                    PlaceNewGroup(KCFac, facilityGroupName);
                }
            }

            PlaceNewGroup(cab, groupName); //CAB: colonyClass Assembly Hub, initial start group

            foreach (KeyValuePair<KCFacilityInfoClass, int> kvp in CABInfo.defaultFacilities)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    KCFacilityBase KCFac = Configuration.CreateInstance(kvp.Key, colony, false);
                    string facilityGroupName = $"{colonyName}_{KCFac.name}_0_{KCFac.facilityTypeNumber}";

                    PlaceNewGroup(KCFac, facilityGroupName);
                }
            }
        }
    }
}
