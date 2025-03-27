using KerbalKonstructs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    public class KCLaunchPadCost : KCFacilityCostClass
    {
        public KCLaunchPadCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, double>> {
                { 0, new Dictionary<PartResourceDefinition, double> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500 } } },
                { 1, new Dictionary<PartResourceDefinition, double> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500 } } }
            };
        }
    }

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
                    

                    FlightGlobals.fetch.SetVesselPosition(FlightGlobals.GetBodyIndex(instance.launchSite.body), instance.launchSite.refLat, instance.launchSite.refLon, instance.launchSite.refAlt + 6, FlightGlobals.ActiveVessel.ReferenceTransform.eulerAngles, true, easeToSurface: true, 0.1);
                    FloatingOrigin.ResetTerrainShaderOffset();
                }
            }
        }

        public KCLaunchpadFacilityWindow(KCLaunchpadFacility launchpad) : base(Configuration.createWindowID(launchpad), "Launchpad")
        {
            toolRect = new Rect(100, 100, 400, 200);
            this.launchpad = launchpad;
        }
    }

    public class KCLaunchpadFacility : KCFacilityBase
    {
        KCLaunchpadFacilityWindow launchpadWindow;

        public string launchSiteUUID;

        public override void OnGroupPlaced()
        {
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);

            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(baseGroupName, "Kerbin").Where(s => s.hasLauchSites).First();
            string uuid = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).First();

            KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(uuid);
            targetInstance.launchSite = new KerbalKonstructs.Core.KKLaunchSite();
            targetInstance.hasLauchSites = true;

            targetInstance.launchSite.staticInstance = targetInstance;
            targetInstance.launchSite.body = targetInstance.CelestialBody;

            string oldName = name;
            bool oldState = baseInstance.launchSite.ILSIsActive;

            targetInstance.launchSite.LaunchSiteName = gph[0].GroupName;
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

        public static List<KCLaunchpadFacility> getLaunchPadsInColony(string saveGame, int bodyIndex, string colonyName)
        {
            List<KCFacilityBase> facilities = GetFacilitiesInColony(saveGame, bodyIndex, colonyName);
            List<KCLaunchpadFacility> launchPads = new List<KCLaunchpadFacility>();
            facilities.ForEach(facility =>
            {
                if (typeof(KCLaunchpadFacility).IsAssignableFrom(facility.GetType()))
                {
                    launchPads.Add((KCLaunchpadFacility)facility);
                }
            });
            return launchPads;
        }

        public override ConfigNode getCustomNode()
        {
            ConfigNode node = new ConfigNode("LAUNCHSITE");
            node.AddValue("launchSiteUUID", launchSiteUUID);
            return node;
        }

        public override void loadCustomNode(ConfigNode customNode)
        {
            if (customNode != null)
            {
                launchSiteUUID = customNode.GetValue("launchSiteUUID");
            }
        }

        public override void OnBuildingClicked()
        {
            launchpadWindow.Toggle();
        }

        public override void Initialize()
        {
            base.Initialize();
            upgradeType = UpgradeType.withGroupChange;

            launchpadWindow = new KCLaunchpadFacilityWindow(this);
        }

        public KCLaunchpadFacility(bool enabled) : base("KCLaunchpadFacility", enabled, 0, 0)
        {
            baseGroupName = "KC_Launchpad_0";
        }
    }
}
