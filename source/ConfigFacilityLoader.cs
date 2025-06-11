using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.KCMiningFacility;
using KerbalColonies.Electricity;
using KerbalColonies.UI;
using KerbalColonies.UI.SingleTimeWindow;
using System;
using System.Collections.Generic;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (f) 2024-2025 AMPW, Halengar

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
    public class FacilityConfigExceptionWindow : KCWindowBase
    {
        private static FacilityConfigExceptionWindow instance = null;
        internal static FacilityConfigExceptionWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FacilityConfigExceptionWindow();
                }
                return !ConfigFacilityLoader.loaded ? instance : null;
            }
        }

        Vector2 scrollPosition = new Vector2(0, 0);

        protected override void OnClose()
        {
            ConfigFacilityLoader.loaded = true;
        }

        protected override void CustomWindow()
        {
            if (Configuration.CabTypes.Count == 0 && Configuration.BuildableFacilities.Count == 0 && ConfigFacilityLoader.failedConfigs.Count == 0)
            {
                GUILayout.Label("No facility configs were found, Kerbal Colonies can't work without facility configs.");
                GUILayout.Label("Since the v1.0.1 update the facility configs are seperate mods, the previously included ones should be on ckan soon.");
                return;
            }

            if (Configuration.CabTypes.Count == 0) GUILayout.Label("No CAB Type was loaded which means this mod won't work.");
            if (Configuration.BuildableFacilities.Count == 0) GUILayout.Label("No Buildable Facilities were loaded.");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                for (int i = 0; i < ConfigFacilityLoader.failedConfigs.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"{ConfigFacilityLoader.failedConfigs[i]}:");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(ConfigFacilityLoader.exceptions[i].Message);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("The mod MIGHT work, but some facilities may not be loaded.");
            GUILayout.Label("I recommend to fix the configs before using the mod.");
            GUILayout.Space(10);
            GUILayout.Label("Please check the log for more details.");
            GUILayout.Label("If you are a modder, please check the wiki for more information.");
        }

        public FacilityConfigExceptionWindow() : base(Configuration.createWindowID(), "Exceptions while loading the facility Configs:")
        {
            toolRect = new Rect(100, 100, 500, 400);
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ConfigFacilityLoader : MonoBehaviour
    {
        // A config file with facility nodes
        // Each facility node has the standart parameters and a list of custom parameters
        // Also includes a node for the base group names
        // display name and facility type

        public static List<string> failedConfigs = new List<string>();
        public static List<Exception> exceptions = new List<Exception>();
        public static bool loaded = false;

        protected void Awake()
        {
            KCFacilityTypeRegistry.RegisterType<KCStorageFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCrewQuarters>();
            KCFacilityTypeRegistry.RegisterType<KCResearchFacility>();
            KCFacilityTypeRegistry.RegisterType<KC_CAB_Facility>();
            KCFacilityTypeRegistry.RegisterType<KCMiningFacility>();
            KCFacilityTypeRegistry.RegisterType<KCProductionFacility>();
            KCFacilityTypeRegistry.RegisterType<KCResourceConverterFacility>();
            KCFacilityTypeRegistry.RegisterType<KCHangarFacility>();
            KCFacilityTypeRegistry.RegisterType<KCLaunchpadFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCommNetFacility>();

            KCFacilityTypeRegistry.RegisterFacilityInfo<KC_CAB_Facility, KC_CABInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCCommNetFacility, KCZeroUpgradeInfoClass>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCCrewQuarters, KCKerbalFacilityInfoClass>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCHangarFacility, KCHangarInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCLaunchpadFacility, KCZeroUpgradeInfoClass>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCMiningFacility, KCMiningFacilityInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCProductionFacility, KCProductionInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCResearchFacility, KCResearchFacilityInfoClass>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCResourceConverterFacility, KCResourceConverterInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCStorageFacility, KCStorageFacilityInfo>();

            try
            {
                KCResourceConverterFacility.LoadResourceConversionLists();
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                failedConfigs.Add("ResourceConversionLists");
                Configuration.writeLog($"Error while loading the resource conversion lists: {e}");
            }

#if !DEBUG
            SingleTimeWindowManager.windows.Add(new Changelogwindow());
#endif

            colonyClass.ColonyUpdate.Add(new ColonyUpdateAction(colonyClass.ColonyUpdateHandler, 0));
            colonyClass.ColonyUpdate.Add(new ColonyUpdateAction(KCProductionFacility.ExecuteProduction, 10));
            colonyClass.ColonyUpdate.Add(new ColonyUpdateAction(KCECManager.ElectricityUpdate, 5));
        }

        protected void Start()
        {
            LoadFacilityConfigs();

            if (FacilityConfigExceptionWindow.Instance != null && (failedConfigs.Count > 0 || Configuration.CabTypes.Count == 0))
            {
                FacilityConfigExceptionWindow.Instance.Open();
            }
        }

        public static void LoadFacilityConfigs()
        {
            ConfigNode[] facilityConfigs = GameDatabase.Instance.GetConfigNodes("KCFacilityConfig");
            foreach (ConfigNode node in facilityConfigs)
            {

                try
                {
                    KCFacilityInfoClass facilityInfo = (KCFacilityInfoClass)Activator.CreateInstance(KCFacilityTypeRegistry.GetInfoType(KCFacilityTypeRegistry.GetType(node.GetValue("type"))), new object[] { node });

                    if (!(facilityInfo is KC_CABInfo))
                    {
                        if (!Configuration.RegisterBuildableFacility(facilityInfo)) throw new Exception($"A facility with the name {facilityInfo.name} already exists.");
                    }
                    else Configuration.RegisterCabInfo(facilityInfo as KC_CABInfo);
                }
                catch (Exception e)
                {
                    exceptions.Add(e.InnerException);
                    if (node.HasValue("name"))
                    {
                        failedConfigs.Add(node.GetValue("name"));
                        Configuration.writeLog($"Invalid facility config: {node.GetValue("name")} \n\nConfig: {node.ToString()} \n\nException: {e}");
                    }
                    else
                    {
                        failedConfigs.Add("Unknown");
                        Configuration.writeLog($"Invalid facility config without a name: {node.ToString()} \n\nException: {e}");
                    }
                }
            }

            List<KCFacilityInfoClass> invalidFacilities = new List<KCFacilityInfoClass> { };
            Configuration.BuildableFacilities.ForEach(f =>
            {
                try
                {
                    f.lateInit();
                }
                catch (Exception e)
                {
                    invalidFacilities.Add(f);
                    exceptions.Add(e);
                    failedConfigs.Add(f.name);
                    Configuration.writeLog($"Invalid facility config: {f.name} \n\nConfig: {f.ToString()} \n\nException: {e}");
                }
            });
            List<KC_CABInfo> invalidCABInfos = new List<KC_CABInfo>();
            Configuration.CabTypes.ForEach(f =>
            {
                try
                {
                    f.lateInit();
                }
                catch (Exception e)
                {
                    invalidCABInfos.Add(f);
                    exceptions.Add(e);
                    failedConfigs.Add(f.name);
                    Configuration.writeLog($"Invalid CAB config: {f.name} \n\nConfig: {f.ToString()} \n\nException: {e}");
                }
            });
            invalidCABInfos.ForEach(f =>
            {
                Configuration.UnregisterCabInfo(f);
                Configuration.writeLog($"Removed invalid CAB config: {f.name}");
            });
        }
    }
}
