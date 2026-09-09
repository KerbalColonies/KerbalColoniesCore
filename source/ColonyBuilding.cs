using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.CabFacility;
using KerbalColonies.colonyFacilities.ProductionFacility;
using KerbalColonies.Settings;
using KerbalColonies.UI;
using KerbalKonstructs;
using KerbalKonstructs.Modules;
using KerbalKonstructs.UI;
using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

namespace KerbalColonies
{
    internal class CABSelectorWindow : KCWindowBase
    {
        private static CABSelectorWindow instance = null;
        internal static CABSelectorWindow Instance
        {
            get
            {
                instance ??= new CABSelectorWindow();
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
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_COLONYBUILDING_INSUFFICIENT_RESOURCE", vesselAmount.ToString("f2"), (resource.Value * Configuration.FacilityCostMultiplier).ToString("f2"), resource.Key.displayName), 10f, ScreenMessageStyle.UPPER_RIGHT);
                    insufficientResources = true;
                }
            }

            if (Funding.Instance != null)
            {
                Configuration.writeLog($"Funds: {Funding.Instance.Funds:f2} / {info.Funds[0] * Configuration.FacilityCostMultiplier:f2}");
                if (Funding.Instance.Funds < info.Funds[0] * Configuration.FacilityCostMultiplier)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_COLONYBUILDING_INSUFFICIENT_FUNDS", Funding.Instance.Funds, info.Funds[0] * Configuration.FacilityCostMultiplier), 10f, ScreenMessageStyle.UPPER_RIGHT);
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

            Funding.Instance?.AddFunds(-info.Funds[0] * Configuration.FacilityCostMultiplier, TransactionReasons.None);
        }

        private Vector2 scrollPosition = new(0, 0);
        protected override void CustomWindow()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                foreach (KC_CAB_Info info in Configuration.CabTypes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{info.displayName}\t");
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < info.resourceCost[0].Count; i++)
                        {
                            GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_LABEL_VALUE", info.resourceCost[0].ElementAt(i).Key.displayName, info.resourceCost[0].ElementAt(i).Value));
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_FUNDS", info.Funds.Count > 0 ? info.Funds[0] : 0));
                    //GUILayout.Label($"ECperSecond: {t.ECperSecond}");
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    if (!checkVesselResources(info)) { GUI.enabled = false; }

                    if (GUILayout.Button(Localizer.Format("#LOC_KC_COMMON_BUILD")))
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

        internal CABSelectorWindow() : base(Configuration.createWindowID(), Localizer.Format("#LOC_KC_COLONYBUILDING_SELECT_CAB"))
        {
            toolRect = new Rect(100, 100, 500, 400);
        }
    }

    internal static class ColonyBuilding
    {
        internal static Queue<QueueInformation> buildQueue = new();
        public static bool placedGroup = false;
        public static bool nextFrame = false;

        internal class QueueInformation
        {
            internal IKCColonyBuildable Buildable = null;
            internal string groupName = null;
            internal string fromGroupName = null;
            internal int targetLevel;
            internal bool singleStatic;
            internal bool isUpgrade;


            internal QueueInformation(IKCColonyBuildable buildable, string groupName, string fromGroupName, int targetLevel, bool singleStatic, bool isUpgrade = false)
            {
                Buildable = buildable;
                this.groupName = groupName;
                this.fromGroupName = fromGroupName;
                this.targetLevel = targetLevel;
                this.singleStatic = singleStatic;
                this.isUpgrade = isUpgrade;
            }
        }

        /// <summary>
        /// This function is called after a group is saved.
        /// the function unregisters itself from the KK groupSave
        /// </summary>
        internal static void PlaceNewGroupSave(KerbalKonstructs.Core.GroupCenter groupCenter)
        {
            QueueInformation placement = buildQueue.Peek();
            Configuration.writeLog($"Placing group {groupCenter.Group} ({placement.groupName} wanted) for {placement.Buildable.Name} at level {placement.targetLevel}");

            if (groupCenter.Group != buildQueue.Peek().groupName) { return; }

            KerbalKonstructs.API.UnRegisterOnGroupSaved(PlaceNewGroupSave);
            List<KerbalKonstructs.Core.StaticInstance> instances = KerbalKonstructs.API.GetGroupStatics(buildQueue.Peek().groupName).ToList();

            foreach (KerbalKonstructs.Core.StaticInstance instance in instances)
            {
                instance.ToggleAllColliders(true);
            }

            if (placement.Buildable is KCFacilityBase facility)
            {
                facility.enabled = true;
                facility.OnGroupPlaced(KCGroupEditor.selectedGroup);
            }

            KerbalKonstructs.API.Save();

            KCGroupEditor.selectedGroup = null;
            KCGroupEditor.selectedBuildable = null;

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
        internal static bool PlaceNewGroup(IKCColonyBuildable buildable, string newGroupName)
        {
            Configuration.writeLog($"Adding {buildable.Name} with group {newGroupName} from colony {buildable.Colony.Name} to the placement queue");

            QueueInformation buildObj = new(buildable, newGroupName, buildable.BuildableInfo.BasegroupNames[buildable.Level], buildable.Level, false);

            buildQueue.Enqueue(buildObj);
            placedGroup = true;
            nextFrame = false;
            return true;
        }

        internal static bool PlaceSingleStatic(KCSingleStaticBuildable buildable, int targetLevel, bool isUpgrade)
        {
            string sharedGroupName = GetStaticBuildablesGroupName(buildable.Colony);
            string pointerName = ((KCSingleStaticBuildableInfo)buildable.BuildableInfo).PointerNames[targetLevel];
            buildQueue.Enqueue(new QueueInformation(buildable, sharedGroupName, pointerName, targetLevel, true, isUpgrade));
            placedGroup = true;
            nextFrame = false;
            return true;
        }

        internal static void PlaceBuildable(KCBuildableBase buildable, int targetLevel, bool upgrade)
        {
            if (buildable is KCSingleStaticBuildable singleStatic)
            {
                PlaceSingleStatic(singleStatic, targetLevel, upgrade);
                return;
            }

            KCGroupedBuildable grouped = (KCGroupedBuildable)buildable;
            if (upgrade) grouped.Upgrade(targetLevel);
            string groupName = $"{buildable.Colony.Name}_{buildable.Name}_{targetLevel}_{buildable.BuildableTypeNumber}";
            PlaceNewGroup(grouped, groupName);
        }

        internal static string GetStaticBuildablesGroupName(colonyClass colony) => $"{colony.Name}_Buildables";

        internal static void QueuePlacer()
        {
            ColonyBuilding.placedGroup = false;
            if (buildQueue.Count() > 0)
            {
                Configuration.writeLog($"Placing group {buildQueue.Peek().groupName} for {buildQueue.Peek().Buildable.Name} at level {buildQueue.Peek().targetLevel}");

                if (ColonyBuilding.buildQueue.Peek().singleStatic)
                {
                    QueueInformation placement = ColonyBuilding.buildQueue.Peek();
                    KerbalKonstructs.Core.GroupCenter sharedGroup = API.GetGroupCenter(placement.groupName, placement.Buildable.Colony.BodyName);
                    if (sharedGroup == null)
                    {
                        API.CreateGroup(placement.groupName);
                        sharedGroup = API.GetGroupCenter(placement.groupName, placement.Buildable.Colony.BodyName);
                    }
                    if (sharedGroup == null)
                    {
                        RestoreSingleStaticPlacement(placement);
                        buildQueue.Dequeue();
                        placedGroup = true;
                        return;
                    }
                    sharedGroup.isInSavegame = true;

                    string uuid = API.SpawnObject(ColonyBuilding.buildQueue.Peek().fromGroupName);
                    if (uuid == null)
                    {
                        RestoreSingleStaticPlacement(placement);
                        buildQueue.Dequeue();
                        placedGroup = true;
                        return;
                    }
                    KerbalKonstructs.Core.StaticInstance instance = API.getStaticInstanceByUUID(uuid);
                    if (instance == null)
                    {
                        API.RemoveStatic(uuid);
                        RestoreSingleStaticPlacement(placement);
                        buildQueue.Dequeue();
                        placedGroup = true;
                        return;
                    }
                    instance.isInSavegame = true;
                    instance.ToggleAllColliders(false);
                    if (!API.AddStaticToGroup(uuid, placement.groupName, placement.Buildable.Colony.BodyName))
                    {
                        API.RemoveStatic(uuid);
                        RestoreSingleStaticPlacement(placement);
                        buildQueue.Dequeue();
                        placedGroup = true;
                        return;
                    }
                    CareerEditor.instance.Close();
                    KCInstanceEditor.Instance.Open(instance, CompleteSingleStaticPlacement, CancelSingleStaticPlacement);
                    return;
                }

                API.RemoveGroup(ColonyBuilding.buildQueue.Peek().groupName); // remove the group if it exists
                API.CreateGroup(ColonyBuilding.buildQueue.Peek().groupName);
                API.CopyGroup(ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().fromGroupName, fromBodyName: Configuration.baseBody);

                EditorGUI.CloseEditors();
                MapDecalEditor.Instance.Close();
                GroupEditor.instance.Close();
                GroupEditor.selectedGroup = API.GetGroupCenter(ColonyBuilding.buildQueue.Peek().groupName);
                KCGroupEditor.selectedBuildable = ColonyBuilding.buildQueue.Peek().Buildable;
                KCGroupEditor.KCInstance.Open();

                API.RegisterOnGroupSaved(ColonyBuilding.PlaceNewGroupSave);
                if (ColonyBuilding.buildQueue.Peek().Buildable is KCFacilityBase facility)
                {
                    facility.KKgroups.Add(ColonyBuilding.buildQueue.Peek().groupName);
                    KCSaveGame.AddGroup(FlightGlobals.GetBodyIndex(FlightGlobals.currentMainBody), ColonyBuilding.buildQueue.Peek().groupName, facility);
                }
                else if (ColonyBuilding.buildQueue.Peek().Buildable is KCGroupedBuildable groupedBuildable)
                {
                    groupedBuildable.KKGroups.Add(ColonyBuilding.buildQueue.Peek().groupName);
                }
            }
            else
            {
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }
        }

        private static void CompleteSingleStaticPlacement(KerbalKonstructs.Core.StaticInstance instance)
        {
            QueueInformation placement = buildQueue.Peek();
            KCSingleStaticBuildable buildable = (KCSingleStaticBuildable)placement.Buildable;

            if (placement.isUpgrade && buildable.BuildableInfo.UpgradeTypes[placement.targetLevel] == UpgradeType.withGroupChange)
            {
                buildable.StaticIds.ForEach(uuid => API.RemoveStatic(uuid));
                buildable.StaticIds.Clear();
            }

            buildable.StaticIds.Add(instance.UUID);
            buildable.Upgrade(placement.targetLevel);
            API.Save();
            FinishSingleStaticPlacement();
        }

        private static void CancelSingleStaticPlacement(KerbalKonstructs.Core.StaticInstance instance)
        {
            QueueInformation placement = buildQueue.Peek();
            API.RemoveStatic(instance.UUID);
            RestoreSingleStaticPlacement(placement);
            API.Save();
            FinishSingleStaticPlacement();
        }

        private static void RestoreSingleStaticPlacement(QueueInformation placement)
        {
            KCSingleStaticBuildable buildable = (KCSingleStaticBuildable)placement.Buildable;
            if (placement.isUpgrade) KCProductionFacility.AddUpgradedBuildable(buildable);
            else KCProductionFacility.AddConstructedBuildable(buildable);
        }

        private static void FinishSingleStaticPlacement()
        {
            buildQueue.Dequeue();
            placedGroup = true;
            nextFrame = false;
        }

        /// <summary>
        /// This function creates a new Colony.
        /// It's meant to be used by the partmodule only.
        /// 0 = success, 1 = insufficient resources, 2 = too many colonies, 3 cabselector open
        /// </summary>
        internal static int CreateColony()
        {
            if (CABSelectorWindow.Instance.IsOpen()) { return 3; }

            if (!KCSaveGame.colonyDictionary.ContainsKey(FlightGlobals.currentMainBody.name))
            {
                KCSaveGame.colonyDictionary.Add(FlightGlobals.currentMainBody.name, []);
            }

            int colonyCount = KCSaveGame.colonyDictionary[FlightGlobals.currentMainBody.name].Count;

            if (colonyCount >= Configuration.MaxColoniesPerBody && Configuration.MaxColoniesPerBody != 0)
            {
                return 2;
            }

            if (Configuration.CabTypes.Count == 1)
            {
                if (!CABSelectorWindow.checkVesselResources(Configuration.CabTypes[0])) { return 1; }
                KC_CAB_Info info = Configuration.CabTypes[0];
                CABSelectorWindow.removeVesselResources(info);
                BuildColony(info);
            }
            else if (Configuration.CabTypes.Count == 0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_COLONYBUILDING_NO_CABS"), 10f, ScreenMessageStyle.UPPER_RIGHT);
                Configuration.writeLog("Unable to create colony: no CABs are unlocked yet.");
                return 1;
            }
            else CABSelectorWindow.Instance.Open();

            return 0;
        }

        internal static void BuildColony(KC_CAB_Info CABInfo)
        {
            int colonyCount = KCSaveGame.colonyDictionary[FlightGlobals.currentMainBody.name].Count + 1;

            string colonyName = $"KC_{FlightGlobals.currentMainBody.name}_{colonyCount}";
            string groupName = $"{colonyName}_CAB";

            colonyClass colony = new(colonyName, CABInfo);

            KCSaveGame.colonyDictionary[FlightGlobals.currentMainBody.name].Add(colony);

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
