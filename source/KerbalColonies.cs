using BackgroundResourceProcessing;
using KerbalColonies.colonyFacilities;
using KerbalColonies.Settings;
using KerbalColonies.UI;
using KerbalColonies.VesselAutoTransfer;
using KSP.UI.Screens;
using System;
using System.Linq;
using ToolbarControl_NS;
using UnityEngine;
using static KerbalKonstructs.API;

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
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class KerbalColonies : MonoBehaviour
    {
        private double lastTime = 0;
        private bool despawned = false;
        private int waitCounter = 0;
        public static bool UpdateNextFrame = false;

        internal static ToolbarControl toolbarControl;

        protected void Awake()
        {
            KSPLog.print("KC awake");

            KerbalKonstructs.API.RegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
            KerbalKonstructs.API.RegisterOnStaticMouseEnter(KCFacilityBase.OnBuildingHoverHandler);
            KerbalKonstructs.API.RegisterOnStaticMouseExit(KCFacilityBase.OnBuildingHoverExitHandler);

            //GameEvents.OnRevertToLaunchFlightState.Add(saveGroupDataFromRevert);
            //GameEvents.OnRevertToPrelaunchFlightState.Add(saveGroupDataFromRevert);

            GameEvents.onGamePause.Add(Pause);
            GameEvents.onGameUnpause.Add(UnPause);

        }

        protected void Start()
        {
            //ResearchAndDevelopment.GetTechnologyState

            KSPLog.print("KC start");
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(
                null,
                null,
                ApplicationLauncher.AppScenes.ALWAYS,
                "KerbalColonies_NS",
                "KerbalColoniesButton",
                "KerbalColonies/KC",
                "KerbalColonies/KC",
                toolTip: "Kerbal Colonies overview"
            );
            toolbarControl.AddLeftRightClickCallbacks(
                () =>
                {
                    if ((Configuration.loadedSaveVersion.Major == 3 || Configuration.loadedSaveVersion > Configuration.saveVersion) && KCLegacySaveWarning.LoadedSaves.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
                    {
                        despawned = false;
                        KCLegacySaveWarning.Instance.Open();
                        return;
                    }
                    OverviewWindow.Instance.Toggle();
                },
                () =>
                {
                    Configuration.ClickToOpen = !Configuration.ClickToOpen;
                    Configuration.writeDebug($"Toggling ClickToOpen: {Configuration.ClickToOpen}");
                    ScreenMessages.PostScreenMessage($"KC: {(Configuration.ClickToOpen ? "enabled" : "disabled")} clicking on buildings.", 10f, ScreenMessageStyle.UPPER_RIGHT);
                }
            );

            realTime = Time.time;
            Configuration.Paused = false;

            BackgroundResourceProcessing.BackgroundResourceProcessor.onAfterVesselChangepoint.Add(onVesselChangepoint);
        }

        public void Pause() => Configuration.Paused = true;
        public void UnPause() => Configuration.Paused = false;

        public void onVesselChangepoint(BackgroundResourceProcessor proc)
        {
            double changeTime = Math.Min(Planetarium.GetUniversalTime() + 3600, proc.NextChangepoint);

            proc.Converters.Where(c => c.Behaviour is KCTransferBehaviour).ToList().ForEach(converter =>
            {
                converter.NextChangepoint = changeTime;
                Configuration.writeDebug($"KCTransferBehaviour onVesselChangepoint called for vessel {proc.Vessel.vesselName}. Setting NextChangepoint to {changeTime}. Current EC rate: {converter.Inputs.FirstOrDefault(kvp => kvp.Value.ResourceName == "ElectricCharge").Value.Ratio * converter.Rate}");
            });
        }

        private float realTime;
        public void Update()
        {
            Configuration.colonyDictionary.Values.SelectMany(x => x).ToList().ForEach(x => x.currentFrameUpdated = false);

            KCGroupEditor.selectedFacility?.WhileBuildingPlaced(KCGroupEditor.selectedGroup);

            if (Planetarium.GetUniversalTime() - lastTime >= 10 || UpdateNextFrame || Time.time - realTime >= 10)
            {
                UpdateNextFrame = false;
                lastTime = Planetarium.GetUniversalTime();
                realTime = Time.time;
                Configuration.colonyDictionary.Values.SelectMany(x => x).ToList().ForEach(x => x.UpdateColony());
            }

            if (ColonyBuilding.placedGroup)
            {
                if (!ColonyBuilding.nextFrame)
                {
                    ColonyBuilding.nextFrame = true;
                }
                else
                {
                    ColonyBuilding.QueuePlacer();
                }
            }


            if (waitCounter < 2)
            {
                waitCounter++;
                return;
            }
            else
            {
                if ((Configuration.loadedSaveVersion.Major == 3 || Configuration.loadedSaveVersion > Configuration.saveVersion) && !despawned && !KCLegacySaveWarning.Instance.IsOpen())
                {
                    Configuration.writeDebug("Despawning all statics and launchsites for legacy save.");
                    despawned = true;
                    if (!KCLegacySaveWarning.LoadedSaves.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
                    {
                        Configuration.writeDebug("Deleting all statics and launchsites for legacy save.");
                        Configuration.loadedSaveVersion = Configuration.saveVersion;

                        Configuration.KCgroups.Where(kvp => kvp.Key == HighLogic.CurrentGame.Seed.ToString()).ToDictionary(x => x.Key, x => x.Value).ToList().ForEach(kvp => kvp.Value.ToList().ForEach(bodyKVP =>
                        {
                            string bodyName = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == bodyKVP.Key).name;

                            bodyKVP.Value.ToList().ForEach(center =>
                            {
                                RemoveGroup(center.Key, bodyName);
                            });
                            Configuration.KCgroups.Remove(kvp.Key);
                        }));

                        Configuration.KCgroups.Where(kvp => kvp.Key != HighLogic.CurrentGame.Seed.ToString())
                        .ToDictionary(x => x.Key, x => x.Value).ToList()
                        .ForEach(kvp =>
                        kvp.Value.ToList().ForEach(bodyKVP =>
                        {
                            string bodyName = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == bodyKVP.Key).name;
                            bodyKVP.Value.ToList().ForEach(KKgroup =>
                            {
                                GetGroupStatics(KKgroup.Key, bodyName).ForEach(s =>
                                DeactivateStatic(s.UUID));

                                if (KKgroup.Value != null)
                                {
                                    if (KKgroup.Value.name == "launchpadNode" && KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(KKgroup.Value.GetValue("launchSiteName")) != null) KerbalKonstructs.Core.LaunchSiteManager.CloseLaunchSite(KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(KKgroup.Value.GetValue("launchSiteName")));
                                }
                            });
                        }));
                    }
                }
            }
        }

        //public void LateUpdate()
        //{
        //}

        protected void OnDestroy()
        {
            toolbarControl.OnDestroy();
            Destroy(toolbarControl);

            Configuration.KCgroups.SelectMany(x => x.Value.Values.SelectMany(y => y.Keys)).ToList()
                .ForEach(x => KerbalKonstructs.API.GetGroupStatics(x).ForEach(uuid => KerbalKonstructs.API.ActivateStatic(uuid.UUID)));

            KerbalKonstructs.API.UnRegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
            KerbalKonstructs.API.UnRegisterOnStaticMouseEnter(KCFacilityBase.OnBuildingHoverHandler);
            KerbalKonstructs.API.UnRegisterOnStaticMouseExit(KCFacilityBase.OnBuildingHoverExitHandler);

            //GameEvents.OnRevertToLaunchFlightState.Remove(saveGroupDataFromRevert);
            //GameEvents.OnRevertToPrelaunchFlightState.Remove(saveGroupDataFromRevert);

            GameEvents.onGamePause.Remove(Pause);
            GameEvents.onGameUnpause.Remove(UnPause);

            BackgroundResourceProcessing.BackgroundResourceProcessor.onAfterVesselChangepoint.Remove(onVesselChangepoint);
        }
    }
}
