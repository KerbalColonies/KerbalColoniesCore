using KerbalColonies.ResourceManagment;
using KerbalKonstructs;
using Smooth.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

namespace KerbalColonies.colonyFacilities.LaunchPadFacility
{
    //public class KCLaunchpadFacilityWindow : KCWindowBase
    //{
    //    KCLaunchpadFacility launchpad;

    //    protected override void CustomWindow()
    //    {
    //        if (launchpad.Colony.CAB.PlayerInColony)
    //        {
    //            if (GUILayout.Button("Teleport to Launchpad"))
    //            {
    //                KerbalKonstructs.Core.StaticInstance instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchpad.launchSiteUUID);

    //                PSystemSetup.SpaceCenterFacility s = instance.launchSite.spaceCenterFacility;
    //                s.GetSpawnPoint(instance.launchSite.LaunchSiteName).GetSpawnPointLatLonAlt(out double lat, out double lon, out double alt);

    //                Vector3 bounds = instance.mesh.transform.localScale;
    //                FlightGlobals.fetch.SetVesselPosition(FlightGlobals.GetBodyIndex(instance.launchSite.body), lat, lon, FlightGlobals.ActiveVessel.vesselSize.y / 2 + bounds.y, FlightGlobals.ActiveVessel.ReferenceTransform.eulerAngles, asl: true, easeToSurface: true, 0.05);
    //                FloatingOrigin.ResetTerrainShaderOffset();
    //            }
    //        }
    //    }

    //    public KCLaunchpadFacilityWindow(KCLaunchpadFacility launchpad) : base(Configuration.createWindowID(), "Launchpad")
    //    {
    //        toolRect = new Rect(100, 100, 400, 200);
    //        this.launchpad = launchpad;
    //    }
    //}

    public class KCLaunchpadFacility : KCFacilityBase, IKCResourceConsumer
    {
        private KCLaunchpadFacilityWindow launchpadWindow;

        public SortedDictionary<int, string> launchSiteUUID { get; protected set; } = [];
        public SortedDictionary<int, string> launchSiteName { get; protected set; } = [];
        public SortedDictionary<int, KerbalKonstructs.Core.StaticInstance> instance { get; protected set; } = [];
        public SortedDictionary<int, bool> customName { get; protected set; } = [];

        public override void OnGroupPlaced(KerbalKonstructs.Core.GroupCenter kkgroup)
        {
            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(GetBaseGroupName(level), Configuration.baseBody).Where(s => s.hasLauchSites).FirstOrDefault();
            if (baseInstance != null)
            {
                string uuid = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).FirstOrDefault();
                if (uuid == null) GetUUIDbyFacility(this).FirstOrDefault();
                if (uuid == null) throw new System.Exception("KC Launchpadfacility: unable to find any KK static for the launchpad.");

                KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(uuid);

                launchSiteName.Add(level, $"KC {Colony.DisplayName} {DisplayName} {level}");
                customName.Add(level, false);

                KerbalKonstructs.Core.KKLaunchSite existingLaunchsite = KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(launchSiteName[level]);
                if (existingLaunchsite != null)
                {
                    Configuration.writeLog($"Launchsite {launchSiteName[level]} already exists, deleting it.");
                    KerbalKonstructs.Core.LaunchSiteManager.DeleteLaunchSite(existingLaunchsite);
                }

                if (targetInstance.launchSite == null)
                {
                    targetInstance.launchSite = new KerbalKonstructs.Core.KKLaunchSite();
                    targetInstance.hasLauchSites = true;

                    targetInstance.launchSite.staticInstance = targetInstance;
                    targetInstance.launchSite.body = targetInstance.CelestialBody;
                }
                else
                {
                    KerbalKonstructs.Core.LaunchSiteManager.DeleteLaunchSite(targetInstance.launchSite);
                    targetInstance.launchSite = new KerbalKonstructs.Core.KKLaunchSite();
                    targetInstance.hasLauchSites = true;

                    targetInstance.launchSite.staticInstance = targetInstance;
                    targetInstance.launchSite.body = targetInstance.CelestialBody;
                }

                bool oldState = baseInstance.launchSite.ILSIsActive;

                targetInstance.launchSite.LaunchSiteName = launchSiteName[level];
                targetInstance.launchSite.LaunchSiteLength = baseInstance.launchSite.LaunchSiteLength;
                targetInstance.launchSite.LaunchSiteWidth = baseInstance.launchSite.LaunchSiteWidth;
                targetInstance.launchSite.LaunchSiteHeight = baseInstance.launchSite.LaunchSiteHeight;

                targetInstance.launchSite.MaxCraftMass = baseInstance.launchSite.MaxCraftMass;
                targetInstance.launchSite.MaxCraftParts = baseInstance.launchSite.MaxCraftParts;

                targetInstance.launchSite.LaunchSiteType = baseInstance.launchSite.LaunchSiteType;
                targetInstance.launchSite.LaunchPadTransform = baseInstance.launchSite.LaunchPadTransform;
                targetInstance.launchSite.LaunchSiteDescription = baseInstance.launchSite.LaunchSiteDescription;
                targetInstance.launchSite.OpenCost = 0;
                targetInstance.launchSite.CloseValue = 0;
                targetInstance.launchSite.LaunchSiteIsHidden = false;
                targetInstance.launchSite.ILSIsActive = baseInstance.launchSite.ILSIsActive;
                targetInstance.launchSite.LaunchSiteAuthor = baseInstance.launchSite.LaunchSiteAuthor;
                targetInstance.launchSite.refLat = (float)targetInstance.RefLatitude;
                targetInstance.launchSite.refLon = (float)targetInstance.RefLongitude;
                targetInstance.launchSite.refAlt = targetInstance.RadiusOffset;
                targetInstance.launchSite.sitecategory = baseInstance.launchSite.sitecategory;
                targetInstance.launchSite.InitialCameraRotation = baseInstance.launchSite.InitialCameraRotation;

                targetInstance.launchSite.ToggleLaunchPositioning = baseInstance.launchSite.ToggleLaunchPositioning;

                targetInstance.launchSite.isOpen = true;
                targetInstance.launchSite.OpenCloseState = "open";


                if (ILSConfig.DetectNavUtils())
                {
                    ILSConfig.RenameSite(targetInstance.launchSite.LaunchSiteName, name);

                    bool state = baseInstance.launchSite.ILSIsActive;
                    if (state)
                        ILSConfig.GenerateFullILSConfig(targetInstance);
                    else
                        ILSConfig.DropILSConfig(targetInstance.launchSite.LaunchSiteName, true);
                }


                targetInstance.launchSite.ParseLSConfig(targetInstance, null);
                targetInstance.SaveConfig();
                KerbalKonstructs.UI.EditorGUI.instance.enableColliders = true;
                targetInstance.ToggleAllColliders(true);
                KerbalKonstructs.Core.LaunchSiteManager.RegisterLaunchSite(targetInstance.launchSite);

                targetInstance.SaveConfig();

                launchSiteUUID.Add(level, uuid);
                instance.Add(level, targetInstance);
            }
            else
            {
                Configuration.writeLog($"{GetBaseGroupName(level)} contains no launchsite, unable to create the launchsite");
                ScreenMessages.PostScreenMessage($"KC: the launchpad basegroup {GetBaseGroupName(level)} contains no launchsite", 20f, ScreenMessageStyle.KERBAL_EVA, color: UnityEngine.Color.red);
                /*
                // Intended default config for launchpad incase no launchsite is found
                // The launchsite transform can't be set, this would require changes to KK which I don't wanna do before the release
                Configuration.writeLog($"{GetBaseGroupName(level)} contains no launchsite, using default configs for the first static instead");
                string uuid = GetUUIDbyFacility(this).FirstOrDefault() ?? throw new System.Exception("KC Launchpadfacility: unable to find any KK static for the launchpad.");

                sharedNode.AddValue("uuid", uuid);
                KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(uuid);
                instance = targetInstance;
                if (targetInstance.launchSite == null)
                {
                    targetInstance.launchSite = new KerbalKonstructs.Core.KKLaunchSite();
                    targetInstance.hasLauchSites = true;

                    targetInstance.launchSite.staticInstance = targetInstance;
                    targetInstance.launchSite.body = targetInstance.CelestialBody;
                }

                string oldName = name;
                bool oldState = false;

                targetInstance.launchSite.LaunchSiteName = launchSiteName;
                sharedNode.AddValue("launchSiteName", launchSiteName);
                targetInstance.launchSite.LaunchSiteLength = 0;
                targetInstance.launchSite.LaunchSiteWidth = 0;
                targetInstance.launchSite.LaunchSiteHeight = 0;

                targetInstance.launchSite.MaxCraftMass = 0;
                targetInstance.launchSite.MaxCraftParts = 0;

                targetInstance.launchSite.LaunchSiteType = KerbalKonstructs.Core.SiteType.Any;
                targetInstance.launchSite.LaunchPadTransform = ;
                targetInstance.launchSite.LaunchSiteDescription = "KC default launchpad config";
                targetInstance.launchSite.OpenCost = 0;
                targetInstance.launchSite.CloseValue = 0;
                targetInstance.launchSite.LaunchSiteIsHidden = false;
                targetInstance.launchSite.ILSIsActive = false;
                targetInstance.launchSite.LaunchSiteAuthor = "AMPW";
                targetInstance.launchSite.refLat = (float)targetInstance.RefLatitude;
                targetInstance.launchSite.refLon = (float)targetInstance.RefLongitude;
                targetInstance.launchSite.refAlt = (float)targetInstance.CelestialBody.GetAltitude(targetInstance.position);
                targetInstance.launchSite.sitecategory = KerbalKonstructs.Core.LaunchSiteCategory.Other;
                targetInstance.launchSite.InitialCameraRotation = 0;

                targetInstance.launchSite.ToggleLaunchPositioning = false;

                targetInstance.launchSite.isOpen = true;
                targetInstance.launchSite.OpenCloseState = "open";


                if (ILSConfig.DetectNavUtils())
                {
                    bool regenerateILSConfig = false;

                    if (oldName != null && !oldName.Equals(name))
                    {
                        ILSConfig.RenameSite(targetInstance.launchSite.LaunchSiteName, name);
                        regenerateILSConfig = true;
                    }

                    if (regenerateILSConfig)
                    {
                            ILSConfig.GenerateFullILSConfig(targetInstance);
                    }
                }


                targetInstance.launchSite.ParseLSConfig(targetInstance, null);
                targetInstance.SaveConfig();
                KerbalKonstructs.UI.EditorGUI.instance.enableColliders = true;
                targetInstance.ToggleAllColliders(true);
                KerbalKonstructs.Core.LaunchSiteManager.RegisterLaunchSite(targetInstance.launchSite);

                targetInstance.SaveConfig();

                launchSiteUUID = uuid;
                instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID);
                */
            }
        }

        public void LaunchVessel(ProtoVessel vessel)
        {
            //KerbalKonstructs.Core.StaticInstance instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID);

            ////vessel.vesselRef.SetPosition(new Vector3(instance.launchSite.refLat, instance.launchSite.refLon, instance.launchSite.refAlt));
            //vessel.position = new Vector3(instance.launchSite.refLat, instance.launchSite.refLon, instance.launchSite.refAlt);
            //vessel.latitude = instance.launchSite.refLat;
            //vessel.longitude = instance.launchSite.refLon;
            //vessel.altitude = instance.launchSite.refAlt + 5;
            //vessel.vesselRef.latitude = instance.launchSite.refLat;
            //vessel.vesselRef.longitude = instance.launchSite.refLon;
            //vessel.vesselRef.altitude = instance.launchSite.refAlt;

            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);


            if (!HighLogic.LoadedSceneIsFlight)
            {
                FlightDriver.StartAndFocusVessel("persistent", FlightGlobals.Vessels.IndexOf(vessel.vesselRef));
            }
            else
            {
                vessel.vesselRef.Load();
                vessel.vesselRef.MakeActive();
                FlightGlobals.SetActiveVessel(vessel.vesselRef);
            }

            InputLockManager.ClearControlLocks();
        }

        public static KCLaunchpadFacility GetLaunchpadFacility(string launchSiteName)
        {
            return Configuration.colonyDictionary.SelectMany(x => x.Value).SelectMany(c => KCFacilityBase.GetAllTInColony<KCLaunchpadFacility>(c)).FirstOrDefault(l =>
                l.launchSiteName.ContainsValue(launchSiteName)
            );
        }

        // DeleteLaunchsite currently internal, waiting till next KK update
        public override void OnColonyNameChange(string name)
        {
            launchSiteName.ToList().ForEach(kvp =>
            {
                if (!customName[kvp.Key])
                {
                    KerbalKonstructs.Core.KKLaunchSite launchSite = instance[kvp.Key].launchSite;
                    KerbalKonstructs.Core.LaunchSiteManager.DeleteLaunchSite(launchSite);
                    launchSite.LaunchSiteName = $"KC {Colony.DisplayName} {DisplayName} {kvp.Key}";
                    launchSiteName[kvp.Key] = launchSite.LaunchSiteName;
                    KerbalKonstructs.Core.LaunchSiteManager.RegisterLaunchSite(launchSite);
                    instance[kvp.Key].SaveConfig();
                }
            });
        }

        public override void OnDisplayNameChange(string displayName)
        {
            launchSiteName.ToList().ForEach(kvp =>
            {
                if (!customName[kvp.Key])
                {
                    KerbalKonstructs.Core.KKLaunchSite launchSite = instance[kvp.Key].launchSite;
                    KerbalKonstructs.Core.LaunchSiteManager.DeleteLaunchSite(launchSite);
                    launchSite.LaunchSiteName = $"KC {Colony.DisplayName} {DisplayName} {kvp.Key}";
                    launchSiteName[kvp.Key] = launchSite.LaunchSiteName;
                    KerbalKonstructs.Core.LaunchSiteManager.RegisterLaunchSite(launchSite);
                    instance[kvp.Key].SaveConfig();
                }
            });
        }

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            if (built && !OutOfResources)
            {
                if (!enabled)
                {
                    enabled = true;
                    if (HighLogic.LoadedScene != GameScenes.SPACECENTER)
                        instance.Values.ToList().ForEach(kkinstance => KerbalKonstructs.Core.LaunchSiteManager.OpenLaunchSite(kkinstance.launchSite));
                }
            }
            else
            {
                if (enabled)
                {
                    enabled = false;
                    if (HighLogic.LoadedScene != GameScenes.SPACECENTER)
                        instance.Values.ToList().ForEach(kkinstance => KerbalKonstructs.Core.LaunchSiteManager.CloseLaunchSite(kkinstance.launchSite));
                }
            }
        }

        public bool OutOfResources { get; protected set; } = false;
        public int ResourceConsumptionPriority { get; set; } = 0;

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => enabled || OutOfResources ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime) : [];

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime) => OutOfResources = false;

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            OutOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => enabled ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value) : [];

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("ECConsumptionPriority", ResourceConsumptionPriority);
            ConfigNode levelNode = new("LaunchpadFacility");
            launchSiteUUID.ToList().ForEach(kvp =>
            {
                ConfigNode levelSubNode = new(kvp.Key.ToString());
                levelSubNode.AddValue("launchSiteUUID", kvp.Value);
                levelSubNode.AddValue("launchSiteName", launchSiteName[kvp.Key]);
                levelSubNode.AddValue("customName", customName[kvp.Key]);
                levelNode.AddNode(levelSubNode);
            });
            node.AddNode(levelNode);
            return node;
        }

        public override void OnBuildingClicked()
        {
            launchpadWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            launchpadWindow.Toggle();
            //if (Colony.CAB.PlayerInColony) launchpadWindow.Toggle();
            //else launchpadWindow.Close();
        }

        public override string GetFacilityProductionDisplay()
        {
            StringBuilder sb = new();
            sb.AppendLine("Available Launch Sites:");
            instance.Values.ToList().ForEach(kkInstance => sb.AppendLine($"- {kkInstance.launchSite.LaunchSiteName} ({kkInstance.launchSite.LaunchSiteType})"));

            return sb.ToString();
        }

        public KCLaunchpadFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (node.HasValue("launchSiteUUID"))
            {
                launchSiteUUID.Add(0, node.GetValue("launchSiteUUID"));
                launchSiteName.Add(0, node.GetValue("launchSiteName"));
                instance.Add(0, KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID[0]));
            }

            if (node.HasNode("LaunchpadFacility"))
            {
                node.GetNode("LaunchpadFacility").GetNodes().ToList().ForEach(n =>
                {
                    int level = int.Parse(n.name);
                    launchSiteUUID.Add(level, n.GetValue("launchSiteUUID"));
                    launchSiteName.Add(level, n.GetValue("launchSiteName"));
                    if (bool.TryParse(n.GetValue("customName"), out bool customname)) customName.Add(level, customname);
                    else customName.Add(level, false);
                    instance.Add(level, KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID[level]));
                });
            }

            instance.ToList().ForEach(kvp =>
            {
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER) KerbalKonstructs.Core.LaunchSiteManager.CloseLaunchSite(kvp.Value.launchSite);
                else KerbalKonstructs.Core.LaunchSiteManager.OpenLaunchSite(kvp.Value.launchSite);
            });

            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ResourceConsumptionPriority = ecPriority;

            //launchpadWindow = new KCLaunchpadFacilityWindow(this);

            launchpadWindow = new KCLaunchpadFacilityWindow(this);
        }

        public KCLaunchpadFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            launchpadWindow = new KCLaunchpadFacilityWindow(this);

            //launchpadWindow = new KCLaunchpadFacilityWindow(this);
        }
    }
}
