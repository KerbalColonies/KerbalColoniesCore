using KerbalKonstructs;
using System;
using System.Collections.Generic;
using System.Linq;

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

namespace KerbalColonies.colonyFacilities
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

    public class KCLaunchpadFacility : KCFacilityBase
    {
        //KCLaunchpadFacilityWindow launchpadWindow;

        public string launchSiteUUID { get; private set; } = null;
        public string launchSiteName { get; private set; } = "";
        public KerbalKonstructs.Core.StaticInstance instance;
        public ConfigNode sharedNode = null;

        public override void OnGroupPlaced()
        {
            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(GetBaseGroupName(level), "Kerbin").Where(s => s.hasLauchSites).FirstOrDefault();
            if (baseInstance != null)
            {
                string uuid = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).FirstOrDefault();
                if (uuid == null) GetUUIDbyFacility(this).FirstOrDefault();
                if (uuid == null) throw new System.Exception("KC Launchpadfacility: unable to find any KK static for the launchpad.");

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
                bool oldState = baseInstance.launchSite.ILSIsActive;

                targetInstance.launchSite.LaunchSiteName = launchSiteName;
                sharedNode.AddValue("launchSiteName", launchSiteName);
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
                targetInstance.launchSite.refAlt = (float)targetInstance.CelestialBody.GetAltitude(targetInstance.position);
                targetInstance.launchSite.sitecategory = baseInstance.launchSite.sitecategory;
                targetInstance.launchSite.InitialCameraRotation = baseInstance.launchSite.InitialCameraRotation;

                targetInstance.launchSite.ToggleLaunchPositioning = baseInstance.launchSite.ToggleLaunchPositioning;

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


                    bool state = baseInstance.launchSite.ILSIsActive;
                    if (oldState != state || regenerateILSConfig)
                    {
                        if (state)
                            ILSConfig.GenerateFullILSConfig(targetInstance);
                        else
                            ILSConfig.DropILSConfig(targetInstance.launchSite.LaunchSiteName, true);
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
            }
            else
            {
                // Untested and should not be used but just in case there's no launchsite in a base group
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
            return Configuration.colonyDictionary.SelectMany(x => x.Value).SelectMany(c => KCLaunchpadFacility.GetLaunchPadsInColony(c)).FirstOrDefault(l =>
                l.launchSiteName == launchSiteName
            );
        }

        public static List<KCLaunchpadFacility> GetLaunchPadsInColony(colonyClass colony)
        {
            return colony.Facilities.Where(x => x is KCLaunchpadFacility).Select(x => (KCLaunchpadFacility)x).ToList();
        }

        public override ConfigNode GetSharedNode()
        {
            return sharedNode;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("launchSiteUUID", launchSiteUUID);
            node.AddValue("launchSiteName", launchSiteName);
            return node;
        }

        public override void OnBuildingClicked()
        {
            //launchpadWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            //if (Colony.CAB.PlayerInColony) launchpadWindow.Toggle();
            //else launchpadWindow.Close();
        }

        public KCLaunchpadFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            launchSiteUUID = node.GetValue("launchSiteUUID");
            launchSiteName = node.GetValue("launchSiteName");
            instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID);
            //launchpadWindow = new KCLaunchpadFacilityWindow(this);
            AllowClick = false;
            AllowRemote = false;
        }

        public KCLaunchpadFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            AllowClick = false;
            AllowRemote = false;
            launchSiteName = $"KC {HighLogic.CurrentGame.Seed.ToString()} {colony.DisplayName} {displayName}";
            sharedNode = new ConfigNode("launchpadNode");
            //launchpadWindow = new KCLaunchpadFacilityWindow(this);
        }
    }
}
