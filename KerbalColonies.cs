using KerbalColonies.colonyFacilities;
using KerbalColonies.UI;
using KSP.UI.Screens;
using System.Linq;
using ToolbarControl_NS;
using UnityEngine;

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
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies
{
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    public class KerbalColonies : MonoBehaviour
    {
        double lastTime = 0;
        bool despawned = false;
        int waitCounter = 0;

        internal static ToolbarControl toolbarControl;

        protected void Awake()
        {
            KSPLog.print("KC awake");

            KerbalKonstructs.API.RegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
        }

        protected void Start()
        {
            KSPLog.print("KC start");
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(
                OverviewWindow.ToggleWindow,
                OverviewWindow.ToggleWindow,
                ApplicationLauncher.AppScenes.ALWAYS,
                "KerbalColonies_NS",
                "KerbalColoniesButton",
                "KerbalColonies/KC",
                "KerbalColonies/KC",
                toolTip: "Kerbal Colonies overview"
            );
        }

        public void Update()
        {
            if (Planetarium.GetUniversalTime() - lastTime >= 10)
            {
                lastTime = Planetarium.GetUniversalTime();
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
                if (!despawned)
                {
                    Configuration.KCgroups.Where(x => x.Key != HighLogic.CurrentGame.Seed.ToString())
                        .ToDictionary(x => x.Key, x => x.Value).ToList()
                        .ForEach(kvp =>
                        kvp.Value.ToList().ForEach(bodyKVP =>
                        bodyKVP.Value.ToList().ForEach(KKgroup =>
                        {
                            KerbalKonstructs.API.GetGroupStatics(KKgroup.Key).ForEach(s =>
                            KerbalKonstructs.API.DeactivateStatic(s.UUID));

                            if (KKgroup.Value != null)
                            {
                                if (KKgroup.Value.name == "launchpadNode") KerbalKonstructs.Core.LaunchSiteManager.CloseLaunchSite(KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(KKgroup.Value.GetValue("launchSiteName")));
                            }
                        })));

                    Configuration.KCgroups.Where(x => x.Key == HighLogic.CurrentGame.Seed.ToString())
                        .ToDictionary(x => x.Key, x => x.Value).ToList()
                        .ForEach(kvp =>
                        kvp.Value.ToList().ForEach(bodyKVP =>
                        bodyKVP.Value.ToList().ForEach(KKgroup =>
                        {
                            KerbalKonstructs.API.GetGroupStatics(KKgroup.Key).ForEach(s =>
                            KerbalKonstructs.API.ActivateStatic(s.UUID));

                            if (KKgroup.Value != null)
                            {
                                if (KKgroup.Value.name == "launchpadNode")
                                    if (HighLogic.LoadedScene != GameScenes.SPACECENTER) KerbalKonstructs.Core.LaunchSiteManager.OpenLaunchSite(KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(KKgroup.Value.GetValue("launchSiteName")));
                                    else KerbalKonstructs.Core.LaunchSiteManager.CloseLaunchSite(KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(KKgroup.Value.GetValue("launchSiteName")));
                            }
                        })));

                    despawned = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
            }
        }

        public void LateUpdate()
        {
        }

        protected void OnDestroy()
        {

            toolbarControl.OnDestroy();
            Destroy(toolbarControl);

            Configuration.KCgroups.SelectMany(x => x.Value.Values.SelectMany(y => y.Keys)).ToList()
                .ForEach(x => KerbalKonstructs.API.GetGroupStatics(x).ForEach(uuid => KerbalKonstructs.API.ActivateStatic(uuid.UUID)));

            KerbalKonstructs.API.UnRegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
        }
    }
}
