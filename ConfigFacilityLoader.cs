using KerbalColonies.colonyFacilities;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
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
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCProductionFacility, KCProductionInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCResourceConverterFacility, KCResourceConverterInfo>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCLaunchpadFacility, KCZeroUpgradeInfoClass>();
            KCFacilityTypeRegistry.RegisterFacilityInfo<KCCommNetFacility, KCZeroUpgradeInfoClass>();

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
            ConfigNode[] facilityConfigs = GameDatabase.Instance.GetConfigNodes("facilityConfigs");
            foreach (ConfigNode node in facilityConfigs)
            {
                foreach (ConfigNode facilityNode in node.GetNodes("facility"))
                {
                    try
                    {
                        KCFacilityInfoClass facilityInfo = (KCFacilityInfoClass)Activator.CreateInstance(KCFacilityTypeRegistry.GetInfoType(KCFacilityTypeRegistry.GetType(facilityNode.GetValue("type"))), new object[] { facilityNode });

                        if (!(facilityInfo is KC_CABInfo))
                        {
                            if (!Configuration.RegisterBuildableFacility(facilityInfo)) throw new Exception($"A facility with the name {facilityInfo.name} already exists.");
                        }
                        else Configuration.RegisterCabInfo(facilityInfo as KC_CABInfo);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        if (facilityNode.HasValue("name"))
                        {
                            failedConfigs.Add(facilityNode.GetValue("name"));
                            Configuration.writeLog($"Invalid facility config: {facilityNode.GetValue("name")} \n\nConfig: {facilityNode.ToString()} \n\nException: {e}");
                        }
                        else
                        {
                            failedConfigs.Add("Unknown");
                            Configuration.writeLog($"Invalid facility config without a name: {facilityNode.ToString()} \n\nException: {e}");
                        }
                    }
                }
            }
        }
    }
}
