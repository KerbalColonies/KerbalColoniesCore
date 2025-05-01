using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

/// This file contains parts from the Kerbal Konstructs mod Hangar.cs file which is licensed under the MIT License.
/// The general idea on how to store vessels is also taken from the Kerbal Konstructs mod

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.colonyFacilities
{
    public class KCHangarFacilityWindow : KCWindowBase
    {
        KCHangarFacility hangar;
        private Vector2 scrollPos;

        protected override void CustomWindow()
        {
            hangar.Update();
            List<StoredVessel> vesselList = hangar.storedVessels.ToList();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            vesselList.ForEach(vessel =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(vessel.vesselName);

                if (vessel.vesselBuildTime == null)
                {
                    if (GUILayout.Button("Load"))
                    {
                        Vessel v = hangar.RollOutVessel(vessel).vesselRef;
                    }
                }
                else
                {
                    GUILayout.Label($"Build time: {(vessel.entireVesselBuildTime - vessel.vesselBuildTime):f2}/{vessel.entireVesselBuildTime:f2}");
                }
                GUILayout.EndHorizontal();
            });
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (hangar.Colony.CAB.PlayerInColony)
            {
                GUILayout.Space(10);
                if (hangar.CanStoreVessel(FlightGlobals.ActiveVessel))
                {
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = false;
                }

                if (GUILayout.Button("Store vessel"))
                {
                    hangar.StoreVessel(FlightGlobals.ActiveVessel, null);
                }
            }

            GUI.enabled = true;
        }

        public KCHangarFacilityWindow(KCHangarFacility hangar) : base(Configuration.createWindowID(), "Hangar")
        {
            this.hangar = hangar;
            toolRect = new Rect(100, 100, 400, 800);
        }
    }

    public class StoredVessel
    {
        public string vesselName;
        public Guid uuid;
        public ConfigNode vesselNode;

        public double vesselVolume;
        public double? vesselBuildTime;
        public double? entireVesselBuildTime;
        public double? vesselDryMass;

        public StoredVessel(string vesselName, Guid uuid, double vesselVolume, ConfigNode vesselNode = null, double? vesselBuildTime = null, double? entireVesselBuildTime = null, double? vesselDryMass = null)
        {
            this.vesselName = vesselName;
            this.uuid = uuid;
            this.vesselNode = vesselNode;
            this.vesselVolume = vesselVolume;
            this.vesselBuildTime = vesselBuildTime;
            this.entireVesselBuildTime = entireVesselBuildTime;
            this.vesselDryMass = vesselDryMass;
        }
    }

    public class KCHangarFacility : KCFacilityBase
    {
        public static List<KCHangarFacility> GetHangarsInColony(colonyClass colony) => colony.Facilities.Where(f => f is KCHangarFacility).Select(f => (KCHangarFacility)f).ToList();

        public static List<StoredVessel> GetConstructingVessels(colonyClass colony) => GetHangarsInColony(colony).SelectMany(h => h.storedVessels.Where(v => v.vesselBuildTime != null)).ToList();

        public static bool CanBuildVessel(double vesselMass, colonyClass colony)
        {
            Configuration.writeDebug($"CanBuildVessel: {vesselMass} in {colony.DisplayName}");
            ConfigNode vesselBuildInfoNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
            if (vesselBuildInfoNode == null) return false;
            KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(vesselBuildInfoNode.GetValue("facilityConfig"));
            if (info == null) return false;

            int level = int.Parse(vesselBuildInfoNode.GetValue("facilityLevel"));
            List<KCProductionFacility> productionFacilitiesInColony = colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f).Where(f => info.HasSameRecipt(level, f)).ToList();

            if (productionFacilitiesInColony.Count == 0) return false;
            else if (info.vesselResourceCost[level].Count == 0) return true;
            else
            {
                bool canBuild = true;
                foreach (KeyValuePair<PartResourceDefinition, double> res in info.vesselResourceCost[level])
                {
                    double colonyAmount = KCStorageFacility.colonyResources(res.Key, colony);
                    Configuration.writeDebug($"resource: {res.Key.name}, amount: {res.Value}, stored in colony: {colonyAmount}");
                    if (res.Value * vesselMass > colonyAmount)
                    {
                        Configuration.writeDebug($"Insufficient resource: {res.Key.name}");
                        canBuild = false;
                    }
                }
                return canBuild;
            }
        }

        public static void BuildVessel(double vesselMass, colonyClass colony)
        {
            if (CanBuildVessel(vesselMass, colony))
            {
                KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(colony.sharedColonyNodes.First(n => n.name == "vesselBuildInfo").GetValue("facilityConfig"));

                int level = int.Parse(colony.sharedColonyNodes.First(n => n.name == "vesselBuildInfo").GetValue("facilityLevel"));
                List<KCProductionFacility> productionFacilitiesInColony = colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f).Where(f => info.HasSameRecipt(level, f)).ToList();

                foreach (KeyValuePair<PartResourceDefinition, double> res in info.vesselResourceCost[level])
                {
                    KCStorageFacility.addResourceToColony(res.Key, -res.Value * vesselMass, colony);
                }
            }
        }


        KCHangarFacilityWindow hangarWindow;

        public Dictionary<int, float> x { get; private set; } = new Dictionary<int, float> { };
        public Dictionary<int, float> y { get; private set; } = new Dictionary<int, float> { };
        public Dictionary<int, float> z { get; private set; } = new Dictionary<int, float> { };
        public float Volume { get { return x[level] * y[level] * z[level]; } }

        public Dictionary<int, int> vesselCapacity { get; private set; } = new Dictionary<int, int> { };

        internal List<StoredVessel> storedVessels = new List<StoredVessel>();

        public double getStoredVolume()
        {
            double volume = 0;

            foreach (StoredVessel vessel in storedVessels)
            {
                volume += vessel.vesselVolume;
            }

            return volume;
        }

        // TODO: make it so the vessel oriantation doesn't matter, e.g. if the hangar has dimensions of 10, 5, 5 and a vessel with 4, 8, 4 (x, y, z in meters) it should work
        public bool CanStoreVessel(Vessel vessel)
        {
            if (vesselCapacity[level] <= storedVessels.Count)
            {
                return false;
            }

            Vector3 vesselSize = vessel.vesselSize;
            if (vesselSize.x > x[level] || vesselSize.y > y[level] || vesselSize.z > z[level])
            {
                return false;
            }

            double vesselVolume = (vesselSize.x * vesselSize.y * vesselSize.z) * 0.8;
            if (vesselVolume > Volume - getStoredVolume())
            {
                return false;
            }

            if (KCCrewQuarters.ColonyKerbalCapacity(Colony) - KCCrewQuarters.GetAllKerbalsInColony(Colony).Count < vessel.GetCrewCount())
            {
                return false;
            }

            return true;
        }

        public bool CanStoreShipConstruct(ShipConstruct ship)
        {
            if (ship == null) return false;
            if (ship.Parts.Count == 0) return false;
            if (vesselCapacity[level] <= storedVessels.Count) return false;

            Vector3 vesselSize = ShipConstruction.CalculateCraftSize(ship);
            if (vesselSize.x > x[level] || vesselSize.y > y[level] || vesselSize.z > z[level])
            {
                return false;
            }

            double vesselVolume = (vesselSize.x * vesselSize.y * vesselSize.z) * 0.8;
            if (vesselVolume > Volume - getStoredVolume())
            {
                return false;
            }

            List<ProtoCrewMember> nullList = ShipConstruction.ShipManifest.GetAllCrew(true);
            List<ProtoCrewMember> noNullList = ShipConstruction.ShipManifest.GetAllCrew(false);
            if (KCCrewQuarters.ColonyKerbalCapacity(Colony) - KCCrewQuarters.GetAllKerbalsInColony(Colony).Count < nullList.Count)
            {
                return false;
            }

            return true;
        }

        public bool StoreVessel(Vessel vessel, double? vesselDryMass)
        {
            if (CanStoreVessel(vessel))
            {
                Configuration.writeDebug($"Storing vessel {vessel.GetDisplayName()} in {this.name}");

                Vector3 vesselSize = vessel.vesselSize;

                StoredVessel storedVessel = new StoredVessel(vessel.GetDisplayName(), vessel.protoVessel.vesselID, (vesselSize.x * vesselSize.y * vesselSize.z) * 0.8);

                if (vesselDryMass == null)
                {
                    storedVessel.vesselBuildTime = null;
                    storedVessel.entireVesselBuildTime = null;
                }
                else
                {
                    Configuration.writeDebug($"vessel part counts: {vessel.Parts.Count}, mass: {vesselDryMass}");
                    storedVessel.vesselBuildTime = (vessel.Parts.Count + vesselDryMass) * 10;
                    storedVessel.entireVesselBuildTime = storedVessel.vesselBuildTime;
                    storedVessel.vesselDryMass = vesselDryMass;
                }

                //get the experience and assign the crew to the rooster
                foreach (Part part in vessel.parts)
                {
                    int count = part.protoModuleCrew.Count;

                    if (count != 0)
                    {
                        ProtoCrewMember[] crewList = part.protoModuleCrew.ToArray();

                        for (int i = 0; i < count; i++)
                        {
                            crewList[i].flightLog.AddEntryUnique(FlightLog.EntryType.Recover);
                            crewList[i].flightLog.AddEntryUnique(FlightLog.EntryType.Land, FlightGlobals.currentMainBody.name);
                            crewList[i].ArchiveFlightLog();

                            // remove the crew from the ship
                            part.RemoveCrewmember(crewList[i]);
                            KCCrewQuarters.AddKerbalToColony(Colony, crewList[i]);
                        }
                    }
                }

                // save the ship
                storedVessel.vesselNode = new ConfigNode("VESSEL");

                //create a backup of the current state, then save that state
                ProtoVessel backup = vessel.BackupVessel();
                backup.Save(storedVessel.vesselNode);

                // save the stored information in the hangar
                storedVessels.Add(storedVessel);

                // remove the stored vessel from the game
                vessel.MakeInactive();
                vessel.Unload();

                FlightGlobals.RemoveVessel(vessel);
                if (vessel != null)
                {
                    vessel.protoVessel.Clean();
                }

                KerbalKonstructs.KerbalKonstructs.instance.UpdateCache();

                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                HighLogic.LoadScene(GameScenes.SPACECENTER);

                return true;
            }

            return false;
        }


        public ProtoVessel RollOutVessel(StoredVessel storedVessel)
        {
            if (!storedVessels.Contains(storedVessel))
            {
                Configuration.writeLog("no Stored Vessel found:" + storedVessel.vesselName);
                return null;
            }

            Configuration.writeDebug($"Rolling out vessel {storedVessel.vesselName} from {this.name}");
            ProtoVessel protoVessel = new ProtoVessel(storedVessel.vesselNode, HighLogic.CurrentGame);
            protoVessel.Load(HighLogic.CurrentGame.flightState);


            List<KCLaunchpadFacility> launchpads = KCLaunchpadFacility.GetLaunchPadsInColony(Colony);
            if (launchpads.Count > 0)
            {
                storedVessels.Remove(storedVessel);
                launchpads[0].LaunchVessel(protoVessel);
            }

            return protoVessel;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            foreach (StoredVessel vessel in storedVessels)
            {
                ConfigNode vesselNode = new ConfigNode("vessel");

                vesselNode.AddValue("VesselID", vessel.uuid.ToString());
                vesselNode.AddValue("VesselName", vessel.vesselName);
                vesselNode.AddNode(vessel.vesselNode);
                vesselNode.AddValue("VesselVolume", vessel.vesselVolume);
                if (vessel.vesselBuildTime != null)
                {
                    vesselNode.AddValue("VesselBuildTime", vessel.vesselBuildTime);
                    vesselNode.AddValue("VesselEntireBuildTime", vessel.entireVesselBuildTime);
                    vesselNode.AddValue("VesselDryMass", vessel.vesselDryMass);
                }

                node.AddNode(vesselNode);
            }

            return node;
        }

        public override void OnBuildingClicked()
        {
            hangarWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            hangarWindow.Toggle();
        }

        private void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());

                if (iLevel.HasValue("x")) x[i] = float.Parse(iLevel.GetValue("x"));
                else if (i > 0) x[i] = x[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no x value (at least for level 0).");

                if (iLevel.HasValue("y")) y[i] = float.Parse(iLevel.GetValue("y"));
                else if (i > 0) y[i] = y[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no y value (at least for level 0).");

                if (iLevel.HasValue("z")) z[i] = float.Parse(iLevel.GetValue("z"));
                else if (i > 0) z[i] = z[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no z value (at least for level 0).");

                if (iLevel.HasValue("capacity")) vesselCapacity[i] = int.Parse(iLevel.GetValue("capacity"));
                else if (i > 0) vesselCapacity[i] = vesselCapacity[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no capacity value (at least for level 0).");
            }
        }

        public KCHangarFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);

            storedVessels = new List<StoredVessel> { };

            foreach (ConfigNode vesselNode in node.GetNodes("vessel"))
            {
                StoredVessel vessel = new StoredVessel(vesselNode.GetValue("VesselName"), Guid.Parse(vesselNode.GetValue("VesselID")), double.Parse(vesselNode.GetValue("VesselVolume")), vesselNode.GetNode("VESSEL"));

                if (vesselNode.HasValue("VesselBuildTime"))
                {
                    vessel.vesselBuildTime = double.Parse(vesselNode.GetValue("VesselBuildTime"));
                    vessel.entireVesselBuildTime = double.Parse(vesselNode.GetValue("VesselEntireBuildTime"));
                    vessel.vesselDryMass = double.Parse(vesselNode.GetValue("VesselDryMass"));
                }

                storedVessels.Add(vessel);
            }
            hangarWindow = new KCHangarFacilityWindow(this);
        }

        public KCHangarFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            hangarWindow = new KCHangarFacilityWindow(this);
        }
    }
}
