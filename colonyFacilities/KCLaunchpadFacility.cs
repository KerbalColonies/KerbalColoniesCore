using CommNet.Network;
using KerbalKonstructs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalColonies.UI;

namespace KerbalColonies.colonyFacilities
{
    public class KCLaunchpadFacilityWindow : KCWindowBase
    {
        KCLaunchpadFacility launchpad;

        protected override void CustomWindow()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                if (GUILayout.Button("Teleport to Launchpad"))
                {
                    KerbalKonstructs.Core.StaticInstance instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchpad.launchSiteUUID);

                    PSystemSetup.SpaceCenterFacility s = instance.launchSite.spaceCenterFacility;
                    s.GetSpawnPoint(instance.launchSite.LaunchSiteName).GetSpawnPointLatLonAlt(out double lat, out double lon, out double alt);

                    FlightGlobals.fetch.SetVesselPosition(FlightGlobals.GetBodyIndex(instance.launchSite.body), lat, lon, alt + FlightGlobals.ActiveVessel.vesselSize.y, FlightGlobals.ActiveVessel.ReferenceTransform.eulerAngles, true, easeToSurface: true, 10);
                    FloatingOrigin.ResetTerrainShaderOffset();
                }
            }
        }

        public KCLaunchpadFacilityWindow(KCLaunchpadFacility launchpad) : base(Configuration.createWindowID(), "Launchpad")
        {
            toolRect = new Rect(100, 100, 400, 200);
            this.launchpad = launchpad;
        }
    }

    public class KCLaunchpadFacility : KCFacilityBase
    {
        KCLaunchpadFacilityWindow launchpadWindow;

        public string launchSiteUUID;
        public KerbalKonstructs.Core.StaticInstance instance;

        public override void OnGroupPlaced()
        {
            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(GetBaseGroupName(level), "Kerbin").Where(s => s.hasLauchSites).First();
            string uuid = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).First();

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

            targetInstance.launchSite.LaunchSiteName = KKgroups[0];
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

        public void LaunchVessel(ProtoVessel vessel)
        {
            KerbalKonstructs.Core.StaticInstance instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID);


            vessel.vesselRef.SetPosition(new Vector3(instance.launchSite.refLat, instance.launchSite.refLon, instance.launchSite.refAlt + 5));
            vessel.position = new Vector3(instance.launchSite.refLat, instance.launchSite.refLon, instance.launchSite.refAlt + 5);
            vessel.latitude = instance.launchSite.refLat;
            vessel.longitude = instance.launchSite.refLon;
            vessel.altitude = instance.launchSite.refAlt + 5;
            vessel.vesselRef.latitude = instance.launchSite.refLat;
            vessel.vesselRef.longitude = instance.launchSite.refLon;
            vessel.vesselRef.altitude = instance.launchSite.refAlt + 5;

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

        public static List<KCLaunchpadFacility> GetLaunchPadsInColony(colonyClass colony)
        {
            return colony.Facilities.Where(x => x is KCLaunchpadFacility).Select(x => (KCLaunchpadFacility)x).ToList();
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("launchSiteUUID", launchSiteUUID);
            return node;
        }

        public override void OnBuildingClicked()
        {
            launchpadWindow.Toggle();
        }

        public KCLaunchpadFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            launchSiteUUID = node.GetValue("launchSiteUUID");
            instance = KerbalKonstructs.API.getStaticInstanceByUUID(launchSiteUUID);
            launchpadWindow = new KCLaunchpadFacilityWindow(this);
        }

        public KCLaunchpadFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            launchpadWindow = new KCLaunchpadFacilityWindow(this);
        }
    }
}
