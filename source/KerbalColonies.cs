using KerbalColonies.colonyFacilities;
using KerbalColonies.UI;
using KSP.UI.Screens;
using KSP.UI.Screens.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.UI;
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
        }

        public void Pause() => Configuration.Paused = true;
        public void UnPause() => Configuration.Paused = false;

        private float realTime;
        private bool initialized = false;
        private bool initialized2 = false;
        private Sprite defaultBackground = null;
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

            string kcIconPath = "C:\\Users\\AMPW\\source\\repos\\KerbalColonies\\GameData\\KerbalColonies\\KC.jpg";
            byte[] fileData = File.ReadAllBytes(kcIconPath);

            // Create new texture and load image
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(fileData))  // LoadImage auto-resizes texture
            {
                Debug.LogError("Failed to load image from: " + kcIconPath);
                return;
            }

            // Create sprite from texture
            Sprite newSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            List<RDPartListItem> partList = Resources.FindObjectsOfTypeAll<RDPartListItem>().ToList();

            foreach (RDPartListItem item in partList)
            {
                if (item == null) continue;
                if (item.gameObject == null) continue;

                if (item.AvailPart == null) continue;
                if (string.IsNullOrEmpty(item.AvailPart.name)) continue;

                if (item.AvailPart.name != "kerbalColoniesFakePart") continue;

                //Image img = item.gameObject.GetComponent<Image>();

                //if (img == null) continue;
                //else
                //{
                //    img.sprite = newSprite;
                //}

                foreach (Transform child in item.gameObject.transform)
                {
                    if (child.name.Contains("Image"))
                    {
                        child.gameObject.SetActive(true);

                        Image img = child.gameObject.GetComponent<Image>();

                        if (img == null) continue;
                        else
                        {
                            img.sprite = newSprite;
                        }
                    }
                    else if (child.name.Contains("icon"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            List<PartListTooltip> toolTipList = Resources.FindObjectsOfTypeAll<PartListTooltip>().ToList();

            foreach (PartListTooltip item in toolTipList)
            {
                if (item == null) continue;
                if (item.gameObject == null) continue;

                //Image img = item.gameObject.GetComponent<Image>();

                //if (img == null) continue;
                //else
                //{
                //    img.sprite = newSprite;
                //}

                GameObject standard = item.gameObject.GetChild("StandardInfo");

                GameObject partName = standard.GetChild("PartName").GetChild("PartNameField");
                TextMeshProUGUI partNameMesh = partName.GetComponent<TextMeshProUGUI>();

                GameObject ThumbPrimary = standard.GetChild("ThumbAndPrimaryInfo");
                GameObject container = ThumbPrimary.GetChild("ThumbContainer");

                Image img = container.GetComponent<Image>();

                if (partNameMesh.text == "Kerbal Colonies Fake Part")
                {
                    if (img == null) continue;

                    if (defaultBackground == null) defaultBackground = img.sprite;

                    img.sprite = newSprite;

                    container.GetChild("ThumbMask").SetActive(false);
                }
                else
                {
                    container.GetChild("ThumbMask").SetActive(true);

                    if (img == null || defaultBackground == null) continue;

                    img.sprite = defaultBackground;
                }

            }



            if (waitCounter < 2)
            {
                waitCounter++;
                return;
            }
            else
            {
                RDTech[] RDTechs = UnityEngine.Object.FindObjectsOfType<RDTech>();
                if (RDTechs.Length > 0)
                {
                    if (!initialized2)
                    {
                        initialized2 = true;
                        Configuration.writeDebug($"Modifing RDTech");
                        foreach (RDTech tech in RDTechs)
                        {
                            if (tech.techID == "flightControl")
                            {
                                AvailablePart fakePart = new AvailablePart();
                                fakePart.name = "kerbalColoniesFakePart";
                                fakePart.title = "Kerbal Colonies Fake Part";
                                AvailablePart baseIcon = tech.partsAssigned[1];
                                fakePart.iconPrefab = baseIcon.iconPrefab;
                                fakePart.iconScale = baseIcon.iconScale;
                                fakePart.partPrefab = baseIcon.partPrefab;

                                tech.partsAssigned.Add(fakePart);

                                //tech.partsAssigned.RemoveRange(1, tech.partsAssigned.Count - 1);
                                //tech.partsPurchased.RemoveRange(1, tech.partsPurchased.Count - 1);
                            }
                        }
                    }

                }


                if (!initialized)
                {
                    //AssetBase.RnDTechTree.SpawnTechTreeNodes();
                    //RDTech[] objectsOfType = UnityEngine.Object.FindObjectsOfType<RDTech>();
                    //Configuration.writeDebug($"Found {objectsOfType.Length} nodes");

                    //foreach (RDTech node in objectsOfType)
                    //{
                    //    Configuration.writeDebug($"ID: {node.techID}");
                    //    foreach (AvailablePart part in node.partsAssigned)
                    //    {
                    //        Configuration.writeDebug($"part: {part.name}");
                    //    }
                    //}


                    initialized = true;


                    //ProtoTechNode[] nodes = AssetBase.RnDTechTree.GetTreeTechs();
                    //foreach (ProtoTechNode node in nodes)
                    //{
                    //    if (node.techID == "flightControl")
                    //    {
                    //        node.partsPurchased.RemoveRange(1, node.partsPurchased.Count - 1);
                    //    }
                    //}

                    //Type type = typeof(ResearchAndDevelopment);
                    //FieldInfo field = type.GetField("protoTechNodes", BindingFlags.NonPublic | BindingFlags.Instance);

                    //Dictionary<string, ProtoTechNode> protoTechNodes = (Dictionary<string, ProtoTechNode>)field.GetValue(ResearchAndDevelopment.Instance);

                    //Configuration.writeDebug($"Found {protoTechNodes.Count} protoTechNodes in ResearchAndDevelopment.");
                    //protoTechNodes.ToList().ForEach(kvp => Configuration.writeDebug($"protoTechNode: {kvp.Key}, id: {kvp.Value.techID}, state: {kvp.Value.state}"));

                    //List<ProtoTechNode> protoTechNodes = AssetBase.RnDTechTree.GetTreeTechs().ToList();

                    //Configuration.writeDebug($"Found {protoTechNodes.Count} protoTechNodes in ResearchAndDevelopment.");
                    //protoTechNodes.ToList().ForEach(kvp => Configuration.writeDebug($"protoTechNode: id: {kvp.techID}, state: {kvp.state}"));


                    //RDTechTree[] objectsOfType = UnityEngine.Object.FindObjectsOfType<RDTechTree>();
                    //Configuration.writeDebug($"Found {objectsOfType.Length} RDTechTree objects.");
                    //foreach (RDTechTree rDTechTree in objectsOfType)
                    //{
                    //    Configuration.writeDebug($"RDTechTree object: {rDTechTree}");
                    //    if (rDTechTree != null)
                    //    {
                    //        Configuration.writeDebug($"nodes: {rDTechTree.GetTreeTechs().Length}");
                    //        foreach (ProtoTechNode tech in rDTechTree.GetTreeTechs())
                    //        {
                    //            Configuration.writeDebug($"tech: {tech.techID}, state: {tech.state}");
                    //        }
                    //    }
                    //}
                }

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

                /*
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
                */
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

            //GameEvents.OnRevertToLaunchFlightState.Remove(saveGroupDataFromRevert);
            //GameEvents.OnRevertToPrelaunchFlightState.Remove(saveGroupDataFromRevert);

            GameEvents.onGamePause.Remove(Pause);
            GameEvents.onGameUnpause.Remove(UnPause);
        }
    }
}
