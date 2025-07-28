using CustomPreLaunchChecks;
using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.StorageFacility;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KCPreFlightRegistrar : MonoBehaviour
    {
        public void Awake()
        {
            CPLC.RegisterCheck(KCHangarPreFlightCheck.GetKCHangarTest);
            CPLC.RegisterCheck(KCCrewPreFlightCheck.GetKCCrewTest);
            CPLC.RegisterCheck(KCResourcePreFlightCheck.GetKCResourceTest);
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    internal class KCPreFlightWorker : MonoBehaviour
    {
        internal static string launchPadName { get; set; } = null;
        internal static double funds { get; set; } = 0; // funds needed to launch
        internal static double vesselMass { get; set; } = 0; // mass of the vessel to be launched
        //internal static string lastColonyName { get; set; } = null; // last colony used to launch the vessel
        //private bool isFlightScene = false; // used to check if the scene is flight or editor
        private int frameCount = 0;

        internal static int? hangarId;
        internal static Vector3? vesselSize;

        public void Start()
        {
            //isFlightScene = HighLogic.LoadedSceneIsFlight;

            if (!HighLogic.LoadedSceneIsFlight)
            {

                launchPadName = null;
                funds = 0;
                vesselMass = 0;
            }
        }

        public void Update()
        {
            if (frameCount <= 20)
            {
                frameCount++;
                return;
            }

            if (HighLogic.LoadedSceneIsFlight && frameCount == 21)
            {
                frameCount++;
                if (launchPadName != null)
                {
                    KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchPadName);

                    Configuration.writeLog($"[KCPreFlightWorker] Launching from {kCLaunchpad.DisplayName}");

                    // Doesn't account for leaving the editor
                    foreach (PartResourceDefinition item in PartResourceLibrary.Instance.resourceDefinitions)
                    {
                        if (KCStorageFacility.blackListedResources.Contains(item.name)) continue;

                        FlightGlobals.ActiveVessel.GetConnectedResourceTotals(item.id, out double amount, out double max, true);
                        FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, item.id, amount, false);
                    }

                    if (Funding.Instance != null)
                    {
                        Funding.Instance.AddFunds(funds, TransactionReasons.None);
                    }

                    KCHangarFacility destinationHangar = KCHangarFacility.GetHangarsInColony(kCLaunchpad.Colony).FirstOrDefault(h => h.CanStoreVessel(FlightGlobals.ActiveVessel));
                    if (destinationHangar != null) destinationHangar.StoreVessel(FlightGlobals.ActiveVessel, vesselMass);
                    else if (hangarId != null)
                    {
                        Configuration.writeLog($"[KCPreFlightWorker] Force storing vessel in hangar {hangarId} with size {vesselSize}");
                        KCHangarFacility hangar = KCFacilityBase.GetFacilityByID((int)hangarId) as KCHangarFacility;
                        hangar.StoreVesselOverride(FlightGlobals.ActiveVessel, vesselSize, vesselMass);
                        hangarId = null;
                        vesselSize = null;
                    }

                    launchPadName = null;
                    funds = 0;
                    vesselMass = 0;
                }
                else
                {
                    List<ProtoCrewMember> kerbalsInColonies = Configuration.colonyDictionary.Values.SelectMany(c => c).SelectMany(c => KCCrewQuarters.GetAllKerbalsInColony(c).Keys).ToList();
                    FlightGlobals.Vessels.ForEach(v =>
                    {
                        List<ProtoCrewMember> pcmInVessel = v.GetVesselCrew().Intersect(kerbalsInColonies, new KCProtoCrewMemberComparer()).ToList();
                        Configuration.writeDebug($"[KCPreFlightWorker] Found {pcmInVessel.Count} kerbals in {v.name} that are in colonies");
                        pcmInVessel.ForEach(pcm =>
                        {
                            Configuration.writeDebug($"[KCPreFlightWorker] Found {pcm.name} in {v.name} that is in a colony");
                            InternalSeat seat = pcm.seat;
                            if (pcm.seat != null)
                            {
                                seat.part.RemoveCrewmember(pcm); // Remove from seat
                                pcm.seat = null;
                            }

                            foreach (Part p in v.Parts)
                            {
                                if (p.protoModuleCrew.Contains(pcm))
                                {
                                    p.protoModuleCrew.Remove(pcm);
                                    int index = p.protoPartSnapshot.GetCrewIndex(pcm.name);
                                    p.protoPartSnapshot.RemoveCrew(pcm);
                                    p.RemoveCrewmember(pcm);
                                    p.ModulesOnUpdate();
                                    break;
                                }
                            }

                            v.RemoveCrew(pcm);
                            HighLogic.CurrentGame.CrewRoster.AddCrewMember(pcm);

                            v.SpawnCrew();
                        });
                    });
                }
            }
        }

        // Changing the selected launchsite does not change the dropdown menu in the editor but it does change the used launchsite
        // This can be confusing for the user so I won't implement this
        //public void Update()
        //{
        //    if (lastColonyName != null)
        //    {
        //        if (frameCount < 10)
        //        {
        //            frameCount++;
        //            return;
        //        }
        //        Configuration.writeDebug($"[KCPreFlightWorker] Opening editor with lastColony {lastColonyName}");
        //        colonyClass lastColony = colonyClass.GetColony(lastColonyName);
        //        if (lastColony != null)
        //        {
        //            Configuration.writeDebug($"[KCPreFlightWorker] Found lastColony {lastColony.Name}");
        //            KCLaunchpadFacility launchPad = KCLaunchpadFacility.GetLaunchPadsInColony(lastColony).FirstOrDefault();
        //            if (launchPad != null)
        //            {
        //                Configuration.writeDebug($"[KCPreFlightWorker] Found launchPad {launchPad.name} (launchsite: {launchPad.launchSiteName})");
        //                KerbalKonstructs.Core.LaunchSiteManager.setLaunchSite(KerbalKonstructs.Core.LaunchSiteManager.GetLaunchSiteByName(launchPad.launchSiteName));

        //            }
        //        }
        //        lastColonyName = null;
        //    }
        //}

        //public void OnDestroy()
        //{
        //    if (isFlightScene)
        //    {
        //        colonyClass lastColony = Configuration.colonyDictionary.Values.SelectMany(x => x).FirstOrDefault(c => c.CAB.PlayerInColony);
        //        if (lastColony != null)
        //        {
        //            lastColonyName = lastColony.Name;
        //            Configuration.writeDebug($"[KCPreFlightWorker] Leaving colony {lastColonyName}");
        //        }
        //    }

        //    isFlightScene = false;
        //}
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
            KCHangarFacility suitableHangar = KCHangarFacility.GetHangarsInColony(kCLaunchpad.Colony).FirstOrDefault(h => h.CanStoreShipConstruct(EditorLogic.fetch.ship));
            if (suitableHangar != null)
            {
                KCPreFlightWorker.hangarId = suitableHangar.id;
                KCPreFlightWorker.vesselSize = EditorLogic.fetch.ship.shipSize;
                Configuration.writeLog($"[KCHangarPreFlightCheck] Found hangar {suitableHangar.name} for {launchSiteName}");
                return true;
            }
            else
            {
                Configuration.writeLog($"[KCHangarPreFlightCheck] No suitable hangar found for {launchSiteName}");
                KCPreFlightWorker.hangarId = null;
                KCPreFlightWorker.vesselSize = null;
                return false;
            }
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
        List<ProtoCrewMember> invalidKerbals = new List<ProtoCrewMember> { };
        KCLaunchpadFacility kCLaunchpad;

        public bool Test()
        {
            Configuration.writeLog($"[KCCrewPreFlightCheck] crew test for {launchSiteName}");
            if (allowLaunch) return true;

            kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchSiteName);
            if (kCLaunchpad == null)
            {
                List<ProtoCrewMember> kerbalsInColonies = Configuration.colonyDictionary.Values.SelectMany(c => c).SelectMany(c => KCCrewQuarters.GetAllKerbalsInColony(c).Keys).ToList();
                ShipConstruction.ShipManifest.GetAllCrew(false).Intersect(kerbalsInColonies, new KCProtoCrewMemberComparer()).ToList().ForEach(pcm => invalidKerbals.Add(pcm));
                Configuration.writeLog($"[KCCrewPreFlightCheck] Found {invalidKerbals.Count} kerbals in {launchSiteName} that are in colonies");
                return invalidKerbals.Count == 0;
            }

            Configuration.writeDebug($"[KCCewPreFlightCheck] vessel crew count: {ShipConstruction.ShipManifest.CrewCount}");

            return ShipConstruction.ShipManifest.CrewCount == 0;
        }

        public string GetWarningTitle()
        {
            return ("KC crew prelaunch check");
        }

        public string GetWarningDescription()
        {
            if (invalidKerbals.Count == 0) return "Due to current limitations of the vessel storage there must be no crew assigned to the vessel while launching.";
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("The following kerbals are already in Colonies:");
                foreach (ProtoCrewMember pcm in invalidKerbals)
                {
                    sb.AppendLine($"-{pcm.name}");
                }
                sb.AppendLine("You can continue with the launch but these kerbals will be removed from the vessel.");
                return sb.ToString();
            }
        }
        public string GetProceedOption() => kCLaunchpad == null ? "Continue launch." : null;
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
        private Dictionary<string, double> Insufficientresources = new Dictionary<string, double>();
        private string message = string.Empty;
        private bool canProceed = false;

        public int CanBuildVessel(double vesselMass, colonyClass colony)
        {
            ConfigNode vesselBuildInfoNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
            if (vesselBuildInfoNode == null) return 1;
            KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(vesselBuildInfoNode.GetValue("facilityConfig"));
            if (info == null) return 1;
            Configuration.writeLog($"[KCResourcePreFlightCheck] Found vesselBuildInfo node for {info.name} in {colony.Name}");

            int level = int.Parse(vesselBuildInfoNode.GetValue("facilityLevel"));
            List<KCProductionFacility> productionFacilitiesInColony = colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f).Where(f => info.HasSameRecipe(level, f)).ToList();

            if (productionFacilitiesInColony.Count == 0) return 2;
            else if (info.vesselResourceCost[level].Count == 0) return 0;
            else
            {
                bool canBuild = true;
                foreach (KeyValuePair<PartResourceDefinition, double> res in info.vesselResourceCost[level])
                {
                    double colonyAmount = KCStorageFacility.colonyResources(res.Key, colony);
                    Configuration.writeLog($"resource: {res.Key.name}, amount: {res.Value * Configuration.VesselCostMultiplier}, stored in colony: {colonyAmount}");
                    if (res.Value * vesselMass * Configuration.VesselCostMultiplier > colonyAmount)
                    {
                        Configuration.writeDebug($"Insufficient resource: {res.Key.name}");
                        canBuild = false;
                        if (!Insufficientresources.ContainsKey(res.Key.name)) Insufficientresources.Add(res.Key.name, res.Value * vesselMass * Configuration.VesselCostMultiplier);
                        else Insufficientresources[res.Key.name] += res.Value * vesselMass * Configuration.VesselCostMultiplier;
                    }
                }
                return canBuild ? 0 : 3;
            }
        }

        public bool Test()
        {
            Configuration.writeLog($"[KCResourcePreFlightCheck] resource test for {launchSiteName}");
            if (allowLaunch) return true;

            KCLaunchpadFacility kCLaunchpad = KCLaunchpadFacility.GetLaunchpadFacility(launchSiteName);
            if (kCLaunchpad == null) return true;

            EditorLogic.fetch.ship.GetShipMass(out float mass, out float wetMass);
            Configuration.writeLog($"[KCResourcePreFlightCheck] Ship mass: {mass}, wet mass: {wetMass}");

            KCPreFlightWorker.vesselMass = mass;

            switch (CanBuildVessel(mass, kCLaunchpad.Colony))
            {
                case 0:
                    message = "[KCResourcePreFlightCheck] No issues found, enough resources are available";
                    Configuration.writeDebug($"[KCResourcePreFlightCheck] {message}");
                    return true;
                case 1:
                    message = "[KCResourcePreFlightCheck] No production facilities that can build vessels in the colony";
                    Configuration.writeDebug($"[KCResourcePreFlightCheck] {message}");
                    return false;
                case 2:
                    message = "[KCResourcePreFlightCheck] No production facilities with the selected recipe";
                    Configuration.writeDebug($"[KCResourcePreFlightCheck] {message}");
                    return false;
                case 3:
                    StringBuilder messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("The following resources are missing:");
                    foreach (KeyValuePair<string, double> res in Insufficientresources)
                    {
                        messageBuilder.AppendLine($"-{res.Key}: {res.Value}");
                    }
                    message = messageBuilder.ToString();
                    Configuration.writeDebug($"[KCResourcePreFlightCheck] {message}");
                    canProceed = true;
                    return false;
                default:
                    return false; // this should never happen
            }
        }

        public string GetWarningTitle()
        {
            return ("KC resources prelaunch check");
        }

        public string GetWarningDescription() => message;
        public string GetProceedOption() => canProceed ? "Continue with the launch" : null;
        public string GetAbortOption() => "Abort launch.";

        public static PreFlightTests.IPreFlightTest GetKCResourceTest(string launchSiteName)
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
