using Expansions.Missions.Editor;
using KerbalColonies.colonyFacilities;
using KerbalKonstructs.Core;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies
{
    public class KCHangarPreFlightCheck : PreFlightTests.IPreFlightTest
    {
        public static string launchSiteName { get; set; }
        bool allowLaunch = false;
        private colonyClass colony;

        public bool Test()
        {
            Configuration.writeDebug($"[KCHangarPreFlightCheck] Hangar test for {launchSiteName}");
            if (allowLaunch) return true;

            KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchSiteName);
            if (kCLaunchpad == null) return true;

            colony = kCLaunchpad.Colony;
            if (!KCHangarFacility.GetHangarsInColony(kCLaunchpad.Colony).Any(h => h.CanStoreShipConstruct(EditorLogic.fetch.ship)))
            {
                Configuration.writeDebug($"[KCLaunchpadPreflightCheck] No hangars found for {launchSiteName}");
                return false;
            }
            Configuration.writeDebug("[KCHangarPreFlightCheck] No issues found");
            return true;
        }

        public string GetWarningTitle()
        {
            return ("KC Hangar prelaunch check");
        }

        public string GetWarningDescription() => $"The colony {colony.DisplayName} has no suitable Hangar to build the craft.";
        public string GetProceedOption() => null;
        public string GetAbortOption() => "Abort launch.";

        public static PreFlightTests.IPreFlightTest GetKCHangarTest(string launchSiteName)
        {
            return new KCHangarPreFlightCheck(launchSiteName);
        }

        public KCHangarPreFlightCheck(string launchSiteName)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                KCHangarPreFlightCheck.launchSiteName = launchSiteName;
            }
            else
            {
                allowLaunch = true;
            }
        }
    }

    public class KCCrewPreFlightCheck : PreFlightTests.IPreFlightTest
    {
        string launchSiteName;
        bool allowLaunch = false;

        public bool Test()
        {
            Configuration.writeDebug($"[KCCrewPreFlightCheck] crew test for {launchSiteName}");
            if (allowLaunch) return true;

            KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchSiteName);
            if (kCLaunchpad == null) return true;

            if (ShipConstruction.ShipManifest.CrewCount > 0) return false;

            Configuration.writeDebug("[KCCrewPreFlightCheck] No issues found");
            return true;
        }

        public string GetWarningTitle()
        {
            return ("KC crew prelaunch check");
        }

        public string GetWarningDescription() => $"Due to current limitations of the vessel storage there must be no crew assigned to the vessel while launching.";
        public string GetProceedOption() => null;
        public string GetAbortOption() => "Abort launch.";

        public static PreFlightTests.IPreFlightTest GetKCCrewTest(string launchSiteName)
        {
            return new KCCrewPreFlightCheck(launchSiteName);
        }

        public KCCrewPreFlightCheck(string launchSiteName)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                this.launchSiteName = launchSiteName;
            }
            else
            {
                allowLaunch = true;
            }
        }
    }

    public class KCResourcePreFlightCheck : PreFlightTests.IPreFlightTest
    {
        public static int funds { get; set; } = 0; // funds needed to launch
        string launchSiteName;
        bool allowLaunch = false;
        private colonyClass colony;

        public bool Test()
        {
            Configuration.writeDebug($"[KCResourcePreFlightCheck] resource test for {launchSiteName}");
            if (allowLaunch) return true;

            KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchSiteName);
            if (kCLaunchpad == null) return true;

            //EditorLogic.fetch.ship.Parts.ForEach(p =>
            //{
            //    p.Resources.ToList().ForEach(r => r.amount = 0);
            //});
            //GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);

            EditorLogic.fetch.ship.GetShipMass(out float mass, out float wetMass);
            Configuration.writeDebug($"[KCResourcePreFlightCheck] Ship mass: {mass}, wet mass: {wetMass}");



            Configuration.writeDebug("[KCResourcePreFlightCheck] No issues found");
            return true;
        }

        public string GetWarningTitle()
        {
            return ("KC crew prelaunch check");
        }

        public string GetWarningDescription() => $"Due to current limitations of the vessel storage there must be no crew assigned to the vessel while launching.";
        public string GetProceedOption() => null;
        public string GetAbortOption() => "Abort launch.";

        public static PreFlightTests.IPreFlightTest GetKCCrewTest(string launchSiteName)
        {
            return new KCResourcePreFlightCheck(launchSiteName);
        }

        public KCResourcePreFlightCheck(string launchSiteName)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                this.launchSiteName = launchSiteName;
            }
            else
            {
                allowLaunch = true;
            }
        }
    }
}
