using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace KerbalColonies.colonyFacilities
{
    public class KCStorageFacilityInfo : KCFacilityInfoClass
    {
        public SortedDictionary<int, double> maxVolume { get; protected set; } = new SortedDictionary<int, double> { };

        public SortedDictionary<int, List<PartResourceDefinition>> resourceWhitelist { get; protected set; } = new SortedDictionary<int, List<PartResourceDefinition>> { };
        public SortedDictionary<int, List<PartResourceDefinition>> resourceBlacklist { get; protected set; } = new SortedDictionary<int, List<PartResourceDefinition>> { };

        public KCStorageFacilityInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                if (n.Value.HasValue("maxVolume")) maxVolume[n.Key] = double.Parse(n.Value.GetValue("maxVolume"));
                else if (n.Key > 0) maxVolume[n.Key] = maxVolume[n.Key - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no maxVolume (at least for level 0).");

                if (n.Value.HasValue("resourceWhitelist"))
                {
                    n.Value.GetValue("resourceWhitelist").Split(',').Select(s => s.Trim()).ToList().ForEach(r =>
                    {
                        PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(r);
                        if (resource != null)
                        {
                            Configuration.writeDebug($"KCStorageFacilityInfo: Adding resource {r} to whitelist for facility {name} (type: {type}) at level {n.Key}.");
                            if (!resourceWhitelist.ContainsKey(n.Key)) resourceWhitelist.Add(n.Key, new List<PartResourceDefinition> { resource });
                            else resourceWhitelist[n.Key].Add(resource);
                        }
                        else throw new Exception($"KCStorageFacilityInfo: Resource {r} not found in PartResourceLibrary for facility {name} (type: {type}) at level {n.Key}.");
                    });
                }
                else if (n.Key > 0) resourceWhitelist[n.Key] = resourceWhitelist[n.Key - 1].ToList();
                else resourceWhitelist[n.Key] = new List<PartResourceDefinition>();

                if (n.Value.HasValue("resourceBlacklist"))
                {
                    n.Value.GetValue("resourceBlacklist").Split(',').Select(s => s.Trim()).ToList().ForEach(r =>
                    {
                        PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(r);
                        if (resource != null)
                        {
                            Configuration.writeDebug($"KCStorageFacilityInfo: Adding resource {r} to blacklist for facility {name} (type: {type}) at level {n.Key}.");
                            if (!resourceBlacklist.ContainsKey(n.Key)) resourceBlacklist.Add(n.Key, new List<PartResourceDefinition> { resource });
                            else resourceBlacklist[n.Key].Add(resource);
                        }
                        else throw new Exception($"KCStorageFacilityInfo: Resource {r} not found in PartResourceLibrary for facility {name} (type: {type}) at level {n.Key}.");
                    });
                }
                else if (n.Key > 0) resourceBlacklist[n.Key] = resourceBlacklist[n.Key - 1].ToList();
                else resourceBlacklist[n.Key] = new List<PartResourceDefinition>();
            });
        }
    }

    public class KCStorageFacilityWindow : KCFacilityWindowBase
    {
        KCStorageFacility storageFacility;
        public HashSet<PartResourceDefinition> allResources = new HashSet<PartResourceDefinition>();
        protected Vector2 scrollPos;

        public void GetVesselResources()
        {
            if (FlightGlobals.ActiveVessel == null) { return; }

            double amount = 0;
            double maxAmount = 0;
            foreach (PartResourceDefinition availableResource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (KCStorageFacility.blackListedResources.Contains(availableResource.name)) { continue; }
                foreach (var partSet in FlightGlobals.ActiveVessel.crossfeedSets)
                {
                    partSet.GetConnectedResourceTotals(availableResource.id, out amount, out maxAmount, true);
                    if (maxAmount > 0)
                    {
                        allResources.Add(availableResource);
                        break;
                    }

                }
            }
        }

        public static bool vesselHasRessources(Vessel v, PartResourceDefinition resource, double amount)
        {
            v.GetConnectedResourceTotals(resource.id, false, out double vesselAmount, out double vesselMaxAmount);
            if (vesselAmount >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static double getVesselRessources(Vessel v, PartResourceDefinition resource)
        {
            v.GetConnectedResourceTotals(resource.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselAmount;
        }

        public bool facilityHasRessources(PartResourceDefinition resouce, double amount)
        {
            if (storageFacility.getRessources()[resouce] >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public double getFacilityResource(PartResourceDefinition resource)
        {
            return storageFacility.getRessources()[resource];
        }

        public double getFacilitySpace(PartResourceDefinition resource)
        {
            return storageFacility.getEmptyAmount(resource);
        }

        /// <summary>
        /// checks if the vessel v has enough space to add amount of r to it.
        /// </summary>
        public static bool vesselHasSpace(Vessel v, PartResourceDefinition r, double amount)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            if (vesselMaxAmount - vesselAmount >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static double getVesselSpace(Vessel v, PartResourceDefinition r)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselMaxAmount - vesselAmount;
        }

        public bool facilityHasSpace(PartResourceDefinition resource, double amount)
        {
            if (storageFacility.maxVolume - storageFacility.currentVolume >= KCStorageFacility.getVolumeForAmount(resource, amount))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool trashResources = false;

        protected override void CustomWindow()
        {
            storageFacility.Colony.UpdateColony();
            if (!storageFacility.enabled) GUI.enabled = false;
            else GUI.enabled = true;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MaxVolume: {storageFacility.storageInfo.maxVolume[storageFacility.level]:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"UsedVolume: {storageFacility.currentVolume:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            List<double> valueList = new List<double> { -10000, -1000, -100, -10, -1, 1, 10, 100, 1000, 10000 };

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            Dictionary<PartResourceDefinition, double> resourceCopy = storageFacility.getRessources();
            for (int r = 0; r < resourceCopy.Count; r++)
            {
                KeyValuePair<PartResourceDefinition, double> kvp = resourceCopy.ElementAt(r);
                GUILayout.BeginVertical();
                GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value:f2}", GUILayout.Height(18));

                if (!storageFacility.Colony.CAB.PlayerInColony && !trashResources) GUI.enabled = false;
                GUILayout.BeginHorizontal();
                foreach (double i in valueList)
                {
                    if (i < 0 && trashResources || !trashResources)
                    {
                        if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                        {
                            Configuration.writeLog($"Transfering {i} {kvp.Key.displayName} from storage facility {storageFacility.DisplayName} to vessel {FlightGlobals.ActiveVessel.vesselName}.");

                            if (i < 0)
                            {
                                if (!trashResources)
                                {
                                    if (vesselHasSpace(FlightGlobals.ActiveVessel, kvp.Key, -i))
                                    {
                                        if (facilityHasRessources(kvp.Key, -i))
                                        {
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, i, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                            storageFacility.changeAmount(kvp.Key, i);
                                        }
                                        else
                                        {
                                            double amount = getFacilityResource(kvp.Key);
                                            storageFacility.changeAmount(kvp.Key, -amount);
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        }
                                    }
                                    else
                                    {
                                        double amount = getVesselSpace(FlightGlobals.ActiveVessel, kvp.Key);

                                        if (facilityHasRessources(kvp.Key, amount))
                                        {
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                            storageFacility.changeAmount(kvp.Key, -amount);
                                        }
                                        else
                                        {
                                            amount = getFacilityResource(kvp.Key);
                                            storageFacility.changeAmount(kvp.Key, -amount);
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        }
                                    }
                                }
                                else
                                {
                                    if (facilityHasRessources(kvp.Key, -i))
                                    {
                                        storageFacility.changeAmount(kvp.Key, i);
                                    }
                                    else
                                    {
                                        double amount = getFacilityResource(kvp.Key);
                                        storageFacility.changeAmount(kvp.Key, -amount);
                                    }
                                }
                            }
                            else
                            {
                                if (facilityHasSpace(kvp.Key, i))
                                {
                                    if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, i))
                                    {
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, i, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, i);
                                    }
                                    else
                                    {
                                        double amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, amount);
                                    }
                                }
                                else
                                {
                                    double amount = getFacilitySpace(kvp.Key);
                                    if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, amount))
                                    {
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, amount);
                                    }
                                    else
                                    {
                                        amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, amount);
                                    }
                                }
                            }
                        }
                    }
                }
                if (!storageFacility.enabled) GUI.enabled = false;
                else GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(2);
            GUI.enabled = true;

            storageFacility.locked = GUILayout.Toggle(storageFacility.locked, "Lock storage", GUILayout.Height(18));
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"EC Consumption Priority: {storageFacility.ECConsumptionPriority}", GUILayout.Height(18));
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) storageFacility.ECConsumptionPriority--;
                if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) storageFacility.ECConsumptionPriority++;
            }
            GUILayout.EndHorizontal();

            trashResources = GUILayout.Toggle(trashResources, "Trash resources", GUILayout.Height(18));
            GUILayout.Label("Warning: enabling the trash resources option will delete the resource instead of transferring it to the vessel.");
        }

        protected override void OnOpen()
        {

            if (FlightGlobals.ActiveVessel == null) return;
            storageFacility.addVesselResourceTypes(FlightGlobals.ActiveVessel);
        }

        protected override void OnClose()
        {
            storageFacility.cleanUpResources();
        }

        public KCStorageFacilityWindow(KCStorageFacility storageFacility) : base(storageFacility, Configuration.createWindowID())
        {
            this.storageFacility = storageFacility;
            //GetVesselResources();
            //foreach (PartResourceDefinition resource in allResources)
            //{
            //    storageFacility.addRessource(resource);
            //}
            toolRect = new Rect(100, 100, 400, 600);
        }
    }

    public class KCStorageFacility : KCFacilityBase, KCECConsumer
    {
        public static HashSet<string> blackListedResources = new HashSet<string> { "ElectricCharge", "IntakeAir" };


        // TODO: add shared colony storage
        public static double colonyResources(PartResourceDefinition resource, colonyClass colony)
        {
            return findFacilityWithResourceType(resource, colony).Sum(s => s.getRessources()[resource]);
        }

        // TODO: make it compatible for negative amounts
        public static double addResourceToColony(PartResourceDefinition resource, double amount, colonyClass colony)
        {
            List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(resource, colony);

            KCStorageFacility.findEmptyStorageFacilities(colony).ForEach(s => storages.Add(s));

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
            return amount;
        }

        public static List<KCStorageFacility> GetStoragesInColony(colonyClass colony)
        {
            return colony.Facilities.Where(f => f is KCStorageFacility).Select(f => (KCStorageFacility)f).ToList();
        }

        public static List<KCStorageFacility> findFacilityWithResourceType(PartResourceDefinition resource, colonyClass colony) => GetStoragesInColony(colony).Where(f => f.enabled && f.getRessources().ContainsKey(resource)).ToList();

        /// <summary>
        /// returns a list of all storage facilities that are not full
        /// </summary>
        public static List<KCStorageFacility> findEmptyStorageFacilities(colonyClass colony) => GetStoragesInColony(colony).Where(f => f.currentVolume < f.storageInfo.maxVolume[f.level]).ToList();

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

        public override string GetFacilityProductionDisplay() => $"{currentVolume:f2}/{maxVolume:f2}m³ used\n{resources.Count} resources stored {(facilityInfo.ECperSecond[level] > 0 ? $"\n{(locked ? 0 : facilityInfo.ECperSecond[level])} EC/s" : "")}";


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
