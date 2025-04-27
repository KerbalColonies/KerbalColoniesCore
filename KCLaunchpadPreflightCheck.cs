using CustomPreLaunchChecks;
using ExtraplanetaryLaunchpads;
using KerbalColonies.colonyFacilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KCPreFlightRegistrar : MonoBehaviour
    {
        public void Awake()
        {
            CPLC.RegisterCheck(KCHangarPreFlightCheck.GetKCHangarTest);
            CPLC.RegisterCheck(KCCrewPreFlightCheck.GetKCCrewTest);
            //CPLC.RegisterCheck(KCResourcePreFlightCheck.GetKCCrewTest);
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    internal class KCPreFlightWorker : MonoBehaviour
    {
        internal static string launchPadName { get; set; } = null;
        internal static double funds { get; set; } = 0; // funds needed to launch

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight && launchPadName != null)
            {
                KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchPadName);

                    Configuration.writeDebug($"[KCPreFlightWorker] Launching from {kCLaunchpad.displayName}");

                // Doesn't account for leaving the editor
                foreach (PartResourceDefinition item in PartResourceLibrary.Instance.resourceDefinitions)
                {
                    FlightGlobals.ActiveVessel.GetConnectedResourceTotals(item.id, out double amount, out double max, true);
                    FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, item.id, amount, false);
                }

                if (Funding.Instance != null)
                {
                    Funding.Instance.AddFunds(funds, TransactionReasons.None);
                }

                KCHangarFacility.GetHangarsInColony(kCLaunchpad.Colony).First(h => h.CanStoreVessel(FlightGlobals.ActiveVessel)).StoreVessel(FlightGlobals.ActiveVessel);

                launchPadName = null;
                funds = 0;
            }
            else
            {
                launchPadName = null;
                funds = 0;
            }
        }
    }

    /// <summary>
    /// This is a workaround to fix the issue when it was tried to launch from a KC launchpad but the editor was instead left.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    internal class KCEditorExitFixer : MonoBehaviour
    {
        public void Start()
        {
            KCPreFlightWorker.launchPadName = null;
            KCPreFlightWorker.funds = 0;
        }
    }

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
            if (kCLaunchpad == null)
            {
                KCPreFlightWorker.launchPadName = null;
                return true;
            }

            KCPreFlightWorker.launchPadName = launchSiteName;
            EditorLogic.fetch.ship.GetShipCosts(out float dryCost, out float wetCost);
            KCPreFlightWorker.funds = dryCost + wetCost;


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

    // the resources don't need to be fully available when building the ship
    public class KCResourcePreFlightCheck : PreFlightTests.IPreFlightTest
    {
        public static int funds { get; set; } = 0; // funds needed to launch
        string launchSiteName;
        bool allowLaunch = false;
        private colonyClass colony;
        private Dictionary<string, double> Insufficientresources = new Dictionary<string, double>();

        public bool CanBuildVessel(double vesselMass, colonyClass colony)
        {
            // TODO: Find a way to do this level save (facilties in the colony with the same vessel cost can work toghether)
            KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(colony.sharedColonyNodes.First(n => n.name == "vesselBuildInfo").GetValue("facilityConfig"));
            if (info == null) return false;

            int level = int.Parse(colony.sharedColonyNodes.First(n => n.name == "vesselBuildInfo").GetValue("facilityLevel"));
            List<KCProductionFacility> productionFacilitiesInColony = colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f).Where(f => info.HasSameRecipt(level, f)).ToList();

            if (productionFacilitiesInColony.Count == 0) return false;
            else if (info.vesselResourceCost[level].Count == 0) return true;
            else
            {
                bool canBuild = true;
                foreach (KeyValuePair<PartResourceDefinition, double> res in info.vesselResourceCost[level])
                {
                    if (res.Value * vesselMass > KCStorageFacility.colonyResources(res.Key, colony))
                    {
                        canBuild = false;
                        if (!Insufficientresources.ContainsKey(res.Key.name)) Insufficientresources.Add(res.Key.name, res.Value);
                        else Insufficientresources[res.Key.name] += res.Value;
                    }
                }
                return canBuild;
            }
        }

        public bool Test()
        {
            Configuration.writeDebug($"[KCResourcePreFlightCheck] resource test for {launchSiteName}");
            if (allowLaunch) return true;

            KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchSiteName);
            if (kCLaunchpad == null) return true;

            EditorLogic.fetch.ship.GetShipMass(out float mass, out float wetMass);
            Configuration.writeDebug($"[KCResourcePreFlightCheck] Ship mass: {mass}, wet mass: {wetMass}");

            if (CanBuildVessel(mass, kCLaunchpad.Colony))
            {
                Configuration.writeDebug("[KCResourcePreFlightCheck] No issues found, enough resources are available");
                return true;
            }
            else
            {
                Configuration.writeDebug("[KCResourcePreFlightCheck] Not enough resources available to build the vessel");
                StringBuilder message = new StringBuilder();
                message.AppendLine("The following resources are missing:");
                foreach (KeyValuePair<string, double> res in Insufficientresources)
                {
                    message.AppendLine($"{res.Key}: {res.Value}");
                }
                Configuration.writeDebug($"[KCResourcePreFlightCheck] {message.ToString()}");
                return false;
            }
        }

        public string GetWarningTitle()
        {
            return ("KC resources prelaunch check");
        }

        public string GetWarningDescription()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("The current resources in the colony are insufficient to build this vessel. You can proceed and start building the vessel while gathering more resources.");
            message.AppendLine("The following resources are missing:");
            foreach (KeyValuePair<string, double> res in Insufficientresources)
            {
                message.AppendLine($"-{res.Key}: {res.Value}");
            }
            return message.ToString();
        }
        public string GetProceedOption() => "Continue with the launch.";
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
