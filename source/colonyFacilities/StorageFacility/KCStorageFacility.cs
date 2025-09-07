using KerbalColonies.Electricity;
using System.Collections.Generic;
using System.Linq;

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

namespace KerbalColonies.colonyFacilities.StorageFacility
{
    public class KCStorageFacility : KCFacilityBase, KCECConsumer
    {
        public static HashSet<string> blackListedResources = new HashSet<string> { "ElectricCharge", "IntakeAir" };


        // TODO: add shared colony storage
        public static double colonyResources(PartResourceDefinition resource, colonyClass colony)
        {
            return findFacilityWithResourceType(resource, colony).Sum(s => s.getRessources()[resource]);
        }

        public static double colonyResourceSpace(PartResourceDefinition resource, colonyClass colony)
        {
            return findEmptyStorageFacilities(colony).Sum(s => s.getEmptyAmount(resource));
        }

        public static double addResourceToColony(PartResourceDefinition resource, double amount, colonyClass colony)
        {
            List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(resource, colony);

            foreach (KCStorageFacility storage in storages)
            {
                if (amount < 0)
                {
                    double tempAmount = storage.getRessources()[resource];
                    if (tempAmount >= -amount)
                    {
                        storage.changeAmount(resource, amount);
                        return 0;
                    }
                    else
                    {
                        storage.changeAmount(resource, -tempAmount);
                        amount += tempAmount;
                    }
                }
                else
                {
                    double tempAmount = storage.getEmptyAmount(resource);
                    if (amount <= tempAmount)
                    {
                        storage.changeAmount(resource, amount);
                        return 0;
                    }
                    else
                    {
                        storage.changeAmount(resource, tempAmount);
                        amount -= tempAmount;
                    }
                }
            }

            if (amount > 0)
            {
                storages.Clear();
                storages = KCStorageFacility.findEmptyStorageFacilities(colony);

                foreach (KCStorageFacility storage in storages)
                {
                    double tempAmount = storage.getEmptyAmount(resource);
                    if (amount <= tempAmount)
                    {
                        storage.changeAmount(resource, amount);
                        return 0;
                    }
                    else
                    {
                        storage.changeAmount(resource, tempAmount);
                        amount -= tempAmount;
                    }
                }
            }

            return amount;
        }

        public static List<KCStorageFacility> GetStoragesInColony(colonyClass colony) => KCFacilityBase.GetAllTInColony<KCStorageFacility>(colony);

        public static List<KCStorageFacility> findFacilityWithResourceType(PartResourceDefinition resource, colonyClass colony) => GetStoragesInColony(colony).Where(f => f.enabled && f.getRessources().ContainsKey(resource)).ToList();

        /// <summary>
        /// returns a list of all storage facilities that are not full
        /// </summary>
        public static List<KCStorageFacility> findEmptyStorageFacilities(colonyClass colony) => GetStoragesInColony(colony).Where(f => f.currentVolume < f.maxVolume).ToList();

        public KCStorageFacilityInfo storageInfo { get { return (KCStorageFacilityInfo)facilityInfo; } }
        public Dictionary<PartResourceDefinition, double> resources;
        public bool outOfEC { get; protected set; } = false;
        public bool locked { get; set; } = false;

        public double currentVolume => resources.Sum(entry => entry.Key.volume * entry.Value);
        public double maxVolume => storageInfo.maxVolume[level];


        public Dictionary<PartResourceDefinition, double> getRessources() => resources;

        public bool HasResourceAmount(PartResourceDefinition resource, double amount) => !locked && resources.ContainsKey(resource) && resources[resource] >= amount;
        public double GetResourceAmount(PartResourceDefinition resource) => !locked && resources.ContainsKey(resource) ? resources[resource] : 0;

        public void addRessource(PartResourceDefinition resource)
        {
            KCStorageFacilityInfo info = storageInfo;
            if (blackListedResources.Contains(resource.name)) return;
            if (info.resourceBlacklist[level].Contains(resource)) return;
            if (info.resourceWhitelist[level].Count > 0 && !info.resourceWhitelist[level].Contains(resource)) return;

            resources.TryAdd(resource, 0);
        }

        public void setAmount(PartResourceDefinition resource, double amount)
        {
            if (locked) return;
            KCStorageFacilityInfo info = storageInfo;
            if (blackListedResources.Contains(resource.name)) return;
            if (info.resourceBlacklist[level].Contains(resource)) return;
            if (info.resourceWhitelist[level].Count > 0 && !info.resourceWhitelist[level].Contains(resource)) return;

            if (resources.ContainsKey(resource))
            {
                resources[resource] = amount;
            }
            else
            {
                resources.Add(resource, amount);
            }
        }

        public double getCurrentVolume()
        {
            return currentVolume;
        }
        public double getMaxVolume()
        {
            return storageInfo.maxVolume[level];
        }

        public static double getVolumeForAmount(PartResourceDefinition resource, double amount) { return amount * resource.volume; }

        public double getEmptyAmount(PartResourceDefinition resource) => enabled ? (storageInfo.maxVolume[level] - currentVolume) / resource.volume : 0;

        /// <summary>
        /// Adds all partresourcedefinitions that are present in a vessel if they aren't in the resources dictionary yet.
        /// </summary>
        public void addVesselResourceTypes(Vessel v)
        {
            if (v == null) return;

            KCStorageFacilityInfo info = storageInfo;

            foreach (PartResourceDefinition resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (blackListedResources.Contains(resource.name)) continue;
                if (info.resourceBlacklist[level].Contains(resource)) continue;
                if (info.resourceWhitelist[level].Count > 0 && !info.resourceWhitelist[level].Contains(resource)) continue;

                v.GetConnectedResourceTotals(resource.id, true, out double amount, out double max, true);
                if (max > 0) resources.TryAdd(resource, 0);
            }
        }

        /// <summary>
        /// Removes all resources with amount 0 from the list
        /// </summary>
        public void cleanUpResources()
        {
            resources.ToList().Where(kvp => kvp.Value == 0).ToList().ForEach(kvp => resources.Remove(kvp.Key));
        }

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            enabled = !outOfEC && !locked && built;
        }

        private KCStorageFacilityWindow StorageWindow;

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            node.AddValue("ECConsumptionPriority", ECConsumptionPriority);
            node.AddValue("locked", locked);

            ConfigNode resourceNode = new ConfigNode("resources");
            foreach (KeyValuePair<PartResourceDefinition, double> entry in resources)
            {
                resourceNode.AddValue(entry.Key.name, entry.Value);
            }
            node.AddNode(resourceNode);

            return node;
        }

        /// <summary>
        /// changes the stored amount by a given value. Returns false if more is pulled out than stored.
        /// </summary>
        public bool changeAmount(PartResourceDefinition resource, double amount)
        {
            if (locked) return false;
            KCStorageFacilityInfo info = storageInfo;
            if (blackListedResources.Contains(resource.name)) return false;
            if (info.resourceBlacklist[level].Contains(resource)) return false;
            if (info.resourceWhitelist[level].Count > 0 && !info.resourceWhitelist[level].Contains(resource)) return false;

            if (amount < 0)
            {
                if (this.resources.ContainsKey(resource))
                {
                    if (this.resources[resource] >= -amount)
                    {
                        this.resources[resource] += amount;
                        return true;
                    }
                }
            }
            else
            {
                if (this.currentVolume + getVolumeForAmount(resource, amount) <= this.storageInfo.maxVolume[level])
                {
                    if (this.resources.ContainsKey(resource))
                    {
                        this.resources[resource] += amount;
                        return true;
                    }
                    else
                    {
                        this.resources.Add(resource, amount);
                        return true;
                    }
                }
            }
            return false;
        }

        public override string GetFacilityProductionDisplay() => $"{currentVolume:f2}/{maxVolume:f2}m³ used\n{resources.Count} resources stored {(facilityInfo.ECperSecond[level] > 0 ? $"\n{(locked ? 0 : facilityInfo.ECperSecond[level]):f2} EC/s" : "")}";


        public int ECConsumptionPriority { get; set; } = 0;
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => locked ? 0 : facilityInfo.ECperSecond[level] * deltaTime;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => outOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => outOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;


        public override void OnBuildingClicked()
        {
            StorageWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            StorageWindow.Toggle();
        }

        public KCStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            resources = new Dictionary<PartResourceDefinition, double>();
            StorageWindow = new KCStorageFacilityWindow(this);

            foreach (ConfigNode.Value value in node.GetNode("resources").values)
            {
                PartResourceDefinition prd = PartResourceLibrary.Instance.GetDefinition(value.name);
                if (prd == null) continue;

                if (resources.ContainsKey(prd)) resources[prd] = double.Parse(value.value);
                else resources.Add(prd, double.Parse(value.value));
            }

            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ECConsumptionPriority = ecPriority;
            if (bool.TryParse(node.GetValue("locked"), out bool isLocked)) locked = isLocked;
        }

        public KCStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            resources = new Dictionary<PartResourceDefinition, double>();
            StorageWindow = new KCStorageFacilityWindow(this);
        }
    }
}
