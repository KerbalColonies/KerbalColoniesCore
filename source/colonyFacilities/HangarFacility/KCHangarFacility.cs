using KerbalColonies.colonyFacilities.CrewQuarters;
using KerbalColonies.colonyFacilities.LaunchPadFacility;
using KerbalColonies.colonyFacilities.ProductionFacility;
using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.ResourceManagment;
using KerbalColonies.Settings;
using NDTester;
using Smooth.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

/// This file contains parts from the Kerbal Konstructs mod Hangar.cs file which is licensed under the MIT License.
/// The general idea on how to store vessels is also taken from the Kerbal Konstructs mod

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.colonyFacilities.HangarFacility
{
    public class KCHangarFacility : KCFacilityBase, IKCResourceConsumer
    {
        /// <summary>
        /// Required for Dimensions > 3
        /// </summary>
        public enum NewDimCalculationMode
        {
            Fixed, // Size = 1
            Sum,
            Product,
            RootProduct, // nth root of the product where n is the current dimension
        }

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
            List<KCProductionFacility> productionFacilitiesInColony = colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f).Where(f => info.HasSameRecipe(level, f)).ToList();

            if (productionFacilitiesInColony.Count == 0) return false;
            else if (info.vesselResourceCost[level].Count == 0) return true;
            else
            {
                bool canBuild = true;
                foreach (KeyValuePair<PartResourceDefinition, double> res in info.vesselResourceCost[level])
                {
                    double colonyAmount = KCUnifiedColonyStorage.colonyStorages[colony].Resources.GetValueOrDefault(res.Key);
                    Configuration.writeDebug($"resource: {res.Key.name}, amount: {res.Value * Configuration.VesselCostMultiplier}, stored in colony: {colonyAmount}");
                    if (res.Value * Configuration.VesselCostMultiplier * vesselMass > colonyAmount)
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
                ConfigNode vesselBuildInfoNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
                KCProductionInfo info = (KCProductionInfo)Configuration.GetInfoClass(vesselBuildInfoNode.GetValue("facilityConfig"));
                int level = int.Parse(vesselBuildInfoNode.GetValue("facilityLevel"));

                List<KCProductionFacility> productionFacilitiesInColony = colony.Facilities.Where(f => f is KCProductionFacility).Select(f => (KCProductionFacility)f).Where(f => info.HasSameRecipe(level, f)).ToList();

                foreach (KeyValuePair<PartResourceDefinition, double> res in info.vesselResourceCost[level])
                {
                    KCUnifiedColonyStorage.colonyStorages[colony].ChangeResourceStored(res.Key, -res.Value * Configuration.VesselCostMultiplier * vesselMass);
                }
            }
        }


        private KCHangarFacilityWindow hangarWindow;
        public KCHangarInfo hangarInfo => (KCHangarInfo)facilityInfo;

        public int ResourceConsumptionPriority { get; set; } = 0;
        public bool OutOfResources { get; set; } = false;

        internal List<StoredVessel> storedVessels = [];

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
        public bool CanStoreVessel(Vessel vessel, int MaxPermutations = 8192)
        {
            Configuration.writeLog($"CanStoreVessel: {name}");
            if (hangarInfo.VesselCapacity[level] <= storedVessels.Count) return false;
            if (!enabled || OutOfResources) return false;

            vessel.UpdateVesselSize();
            Vector3 vesselSize = vessel.vesselSize;
            if (!CanStoreSize(new double[] { vesselSize.x, vesselSize.y, vesselSize.z }, MaxPermutations))
            {
                Configuration.writeLog($"Vessel size: {vesselSize.x}, {vesselSize.y}, {vesselSize.z} is too big for the hangar");
                return false;
            }

            if (KCCrewQuarters.ColonyKerbalCapacity(Colony) - KCCrewQuarters.GetAllKerbalsInColony(Colony).Count < vessel.GetCrewCount())
            {
                Configuration.writeLog($"Not enough space for the crew: {vessel.GetCrewCount()} in the colony: {KCCrewQuarters.ColonyKerbalCapacity(Colony)}");
                return false;
            }

            Configuration.writeLog($"CanStoreVessel: {name} is ok for the vessel: {vessel.GetDisplayName()}");
            return true;
        }

        public bool CanStoreShipConstruct(ShipConstruct ship)
        {
            Configuration.writeLog($"CanStoreShipConstruct: {name}");
            if (ship == null) return false;
            if (ship.Parts.Count == 0) return false;
            if (!enabled || OutOfResources) return false;
            KCHangarInfo info = hangarInfo;
            if (info.VesselCapacity[level] <= storedVessels.Count) return false;

            Vector3 vesselSize = ship.shipSize;
            Configuration.writeDebug($"vessel size: {vesselSize.x}, {vesselSize.y}, {vesselSize.z}");
            if (!CanStoreSize(new double[] { vesselSize.x, vesselSize.y, vesselSize.z }))
            {
                Configuration.writeLog($"Vessel size: {vesselSize.x}, {vesselSize.y}, {vesselSize.z} is too big for the hangar");
                return false;
            }

            Configuration.writeLog($"CanStoreShipConstruct: {name} is ok for the ship: {ship.shipName}");
            return true;
        }

        public bool CanStoreSize(double[] Size, int maxPermutations = 8192, int maxProcessors = 0)
        {
            int dimensions = hangarInfo.Dimension;

            int vesselCount = 0;
            OrthogonalObject[] testObjects = new OrthogonalObject[storedVessels.Count + 1]; // All stored vessels + the new one
            storedVessels.ForEach(vessel =>
            {
                double[] ShipSize = new double[dimensions];

                ShipSize[0] = vessel.x;
                ShipSize[1] = vessel.y;
                ShipSize[2] = vessel.z;

                for (int i = 3; i < dimensions; i++)
                {
                    switch (hangarInfo.NewDimCalculations[i])
                    {
                        case NewDimCalculationMode.Fixed:
                            ShipSize[i] = 1;
                            break;
                        case NewDimCalculationMode.Sum:
                            ShipSize[i] = ShipSize.Sum();
                            break;
                        case NewDimCalculationMode.Product:
                            ShipSize[i] = ShipSize.Aggregate(1.0, (acc, val) => acc * val);
                            break;
                        case NewDimCalculationMode.RootProduct:
                            double product = ShipSize.Aggregate(1.0, (acc, val) => acc * val);
                            ShipSize[i] = Math.Pow(product, 1.0 / dimensions);
                            break;
                    }
                }

                testObjects[vesselCount] = new NDTester.OrthogonalObject(ShipSize, 1);
                vesselCount++;
            });

            double[] ActiveShipSize = new double[dimensions];

            ActiveShipSize[0] = Size[0];
            ActiveShipSize[1] = Size[1];
            ActiveShipSize[2] = Size[2];

            for (int i = 3; i < dimensions; i++)
            {
                switch (hangarInfo.NewDimCalculations[i])
                {
                    case NewDimCalculationMode.Fixed:
                        ActiveShipSize[i] = 1;
                        break;
                    case NewDimCalculationMode.Sum:
                        ActiveShipSize[i] = ActiveShipSize.Sum();
                        break;
                    case NewDimCalculationMode.Product:
                        ActiveShipSize[i] = ActiveShipSize.Aggregate(1.0, (acc, val) => acc * val);
                        break;
                    case NewDimCalculationMode.RootProduct:
                        double product = ActiveShipSize.Aggregate(1.0, (acc, val) => acc * val);
                        ActiveShipSize[i] = Math.Pow(product, 1.0 / dimensions);
                        break;
                }
            }

            testObjects[vesselCount] = new NDTester.OrthogonalObject(ActiveShipSize, 1);

            List<Container> Hangars = [];

            for (int i = 0; i <= level; i++)
            {
                if (facilityInfo.UpgradeTypes[i] != UpgradeType.withAdditionalGroup && i != level) continue;

                Hangars.Add(new Container(hangarInfo.Sizes[i].Values.ToArray()));
            }

            Solver solver = new(hangarInfo.Dimension, testObjects, Hangars.ToArray());

            return solver.solve(maxPermutations: maxPermutations, maxProcessors: maxProcessors, stopEarly: true);
        }

        public bool StoreVessel(Vessel vessel, double? vesselDryMass)
        {

            Configuration.writeLog($"Storing vessel {vessel.GetDisplayName()} in {name}");

            Vector3 vesselSize = vessel.vesselSize;

            StoredVessel storedVessel = new(vessel.GetDisplayName(), vessel.protoVessel.vesselID, vesselSize.x, vesselSize.y, vesselSize.z);

            if (vesselDryMass == null)
            {
                storedVessel.vesselBuildTime = null;
                storedVessel.entireVesselBuildTime = null;
            }
            else
            {
                Configuration.writeDebug($"vessel part counts: {vessel.Parts.Count}, mass: {vesselDryMass}");
                storedVessel.vesselBuildTime = (vessel.Parts.Count + vesselDryMass) * 10 * Configuration.VesselTimeMultiplier;
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
            vessel?.protoVessel.Clean();

            KerbalKonstructs.KerbalKonstructs.instance.UpdateCache();

            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            HighLogic.LoadScene(GameScenes.SPACECENTER);

            return true;
        }

        /// <summary>
        /// Force stores the vessel in the hangar ignoring the restrictions
        /// </summary>
        public void StoreVesselOverride(Vessel vessel, Vector3? vesselsize, double? vesselDryMass)
        {
            Configuration.writeLog($"Force storing vessel {vessel.GetDisplayName()} in {name}");

            if (vesselsize == null) vesselsize = vessel.vesselSize;
            Vector3 vesselSize = (Vector3)vesselsize;

            StoredVessel storedVessel = new(vessel.GetDisplayName(), vessel.protoVessel.vesselID, vesselSize.x, vesselSize.y, vesselSize.z);

            if (vesselDryMass == null)
            {
                storedVessel.vesselBuildTime = null;
                storedVessel.entireVesselBuildTime = null;
            }
            else
            {
                Configuration.writeDebug($"vessel part counts: {vessel.Parts.Count}, mass: {vesselDryMass}");
                storedVessel.vesselBuildTime = (vessel.Parts.Count + vesselDryMass) * 10 * Configuration.VesselTimeMultiplier;
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
            vessel?.protoVessel.Clean();

            KerbalKonstructs.KerbalKonstructs.instance.UpdateCache();

            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            HighLogic.LoadScene(GameScenes.SPACECENTER);
        }

        public ProtoVessel RollOutVessel(StoredVessel storedVessel)
        {
            if (!storedVessels.Contains(storedVessel))
            {
                Configuration.writeLog("no Stored Vessel found:" + storedVessel.vesselName);
                return null;
            }

            Configuration.writeDebug($"Rolling out vessel {storedVessel.vesselName} from {name}");
            ProtoVessel protoVessel = new(storedVessel.vesselNode, HighLogic.CurrentGame);
            protoVessel.Load(HighLogic.CurrentGame.flightState);


            List<KCLaunchpadFacility> launchpads = KCFacilityBase.GetAllTInColony<KCLaunchpadFacility>(Colony);
            if (launchpads.Count > 0)
            {
                storedVessels.Remove(storedVessel);
                launchpads[0].LaunchVessel(protoVessel);
            }

            return protoVessel;
        }

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime)
        {
            enabled = enabled || storedVessels.Count > 0;

            if (enabled)
            {
                Dictionary<PartResourceDefinition, double> resourceConsumption = new();

                facilityInfo.ResourceUsage[level].ToList().ForEach(kvp => resourceConsumption.Add(kvp.Key, kvp.Value * deltaTime));

                hangarInfo.ResourceUsagePerVessel[level].ToList().ForEach(kvp => resourceConsumption.Add(kvp.Key, kvp.Value * deltaTime * storedVessels.Count));

                return resourceConsumption;
            }
            return new();
        }

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime)
        {
            OutOfResources = false;
        }

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            OutOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond()
        {
            enabled = enabled || storedVessels.Count > 0;

            if (enabled)
            {
                Dictionary<PartResourceDefinition, double> resourceConsumption = new();

                facilityInfo.ResourceUsage[level].ToList().ForEach(kvp => resourceConsumption.Add(kvp.Key, kvp.Value));

                hangarInfo.ResourceUsagePerVessel[level].ToList().ForEach(kvp => resourceConsumption.Add(kvp.Key, kvp.Value * storedVessels.Count));

                return resourceConsumption;
            }
            return new();
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            foreach (StoredVessel vessel in storedVessels)
            {
                ConfigNode vesselNode = new("vessel");

                vesselNode.AddValue("VesselID", vessel.uuid.ToString());
                vesselNode.AddValue("VesselName", vessel.vesselName);
                vesselNode.AddNode(vessel.vesselNode);
                vesselNode.AddValue("x", vessel.x);
                vesselNode.AddValue("y", vessel.y);
                vesselNode.AddValue("z", vessel.z);
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

        public override string GetFacilityProductionDisplay() => $"{storedVessels.Count}/{hangarInfo.VesselCapacity[level]} vessels stored\n{getStoredVolume():F1}m³/{hangarInfo.Volume(level):F1}m³ used\nSize: {(string.Concat("\n", string.Join(", ", hangarInfo.Sizes[level].Select(kvp => $"{kvp.Key}: {kvp.Value:f2}"))))}";

        public KCHangarFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            storedVessels = [];

            foreach (ConfigNode vesselNode in node.GetNodes("vessel"))
            {
                if (!vesselNode.HasValue("x")) continue;

                StoredVessel vessel = new(vesselNode.GetValue("VesselName"), Guid.Parse(vesselNode.GetValue("VesselID")), double.Parse(vesselNode.GetValue("x")), double.Parse(vesselNode.GetValue("y")), double.Parse(vesselNode.GetValue("z")), vesselNode.GetNode("VESSEL"));

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
            hangarWindow = new KCHangarFacilityWindow(this);
        }
    }
}
