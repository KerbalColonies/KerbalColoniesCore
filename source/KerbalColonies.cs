using KerbalColonies.colonyFacilities;
using KerbalColonies.Settings;
using KerbalColonies.UI;
using KSP.UI.Screens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ToolbarControl_NS;
using UnityEngine;
using static KerbalKonstructs.API;

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
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class KerbalColonies : MonoBehaviour
    {
        double lastTime = 0;
        bool despawned = false;
        int waitCounter = 0;
        public static bool UpdateNextFrame = false;

        internal static ToolbarControl toolbarControl;

        public void saveGroupDataFromRevert(FlightState state)
        {
            string root = "KCgroups";

            ConfigNode[] nodes = new ConfigNode[1] { new ConfigNode() };

            foreach (KeyValuePair<string, Dictionary<int, Dictionary<string, ConfigNode>>> gameKVP in Configuration.KCgroups)
            {
                ConfigNode saveGameNode = new ConfigNode(gameKVP.Key, "The savegame name");
                foreach (KeyValuePair<int, Dictionary<string, ConfigNode>> bodyKVP in gameKVP.Value)
                {
                    ConfigNode bodyNode = new ConfigNode(bodyKVP.Key.ToString(), "The celestial body id");
                    foreach (KeyValuePair<string, ConfigNode> groupName in bodyKVP.Value)
                    {
                        ConfigNode groupNode = new ConfigNode(groupName.Key, "The KK group name");
                        if (groupName.Value != null) groupNode.AddNode(groupName.Value);
                        bodyNode.AddNode(groupNode);
                    }
                    saveGameNode.AddNode(bodyNode);
                }
                nodes[0].AddNode(saveGameNode);
            }

            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}ColonyDataV3.cfg";

            ConfigNode node = new ConfigNode();
            nodes[0].name = root;
            node.AddNode(nodes[0]);
            node.Save(path);
        }

        protected void Awake()
        {
            KSPLog.print("KC awake");

            KerbalKonstructs.API.RegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);

            GameEvents.OnRevertToLaunchFlightState.Add(saveGroupDataFromRevert);
            GameEvents.OnRevertToPrelaunchFlightState.Add(saveGroupDataFromRevert);

            GameEvents.onGamePause.Add(Pause);
            GameEvents.onGameUnpause.Add(UnPause);
        }

        protected void Start()
        {
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
                () => OverviewWindow.Instance.Toggle(),
                () => { 
                    Configuration.ClickToOpen = !Configuration.ClickToOpen;
                    Configuration.writeDebug($"Toggling ClickToOpen: {Configuration.ClickToOpen}");
                    ScreenMessages.PostScreenMessage($"KC: {(Configuration.ClickToOpen ? "enabled" : "disabled")} clicking on buildings.", 10f, ScreenMessageStyle.UPPER_RIGHT);
                }
            );

            realTime = Time.time;
        }

        public void Pause() => Configuration.Paused = true;
        public void UnPause() => Configuration.Paused = false;

        private float realTime;
        public void Update()
        {
            Configuration.colonyDictionary.Values.SelectMany(x => x).ToList().ForEach(x => x.currentFrameUpdated = false);

            if (KCGroupEditor.selectedFacility != null)
            {
                KCGroupEditor.selectedFacility.WhileBuildingPlaced(KCGroupEditor.selectedGroup);
            }

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
                if (!despawned)
                {
                    despawned = true;
                    Configuration.KCgroups
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

                    Configuration.colonyDictionary.ToList().ForEach(kvp =>
                    {
                        string bodyName = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == kvp.Key).name;
                        kvp.Value.ForEach(colony =>
                        {
                            colony.CAB.KKgroups.ForEach(KKgroup => GetGroupStatics(KKgroup, bodyName).ForEach(s => ActivateStatic(s.UUID)));
                            colony.Facilities.ForEach(fac =>
                            {
                                fac.KKgroups.ForEach(KKgroup => GetGroupStatics(KKgroup, bodyName).ForEach(s => ActivateStatic(s.UUID)));
                            });
                            colony.UpdateColony();
                        });
                    });
                }
            }

            //if (Input.GetKeyDown(KeyCode.U))
            //{
            //}
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

            GameEvents.OnRevertToLaunchFlightState.Remove(saveGroupDataFromRevert);
            GameEvents.OnRevertToPrelaunchFlightState.Remove(saveGroupDataFromRevert);

            GameEvents.onGamePause.Remove(Pause);
            GameEvents.onGameUnpause.Remove(UnPause);
        }
    }
}
