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
    internal class KCStorageFacilityWindow : KCWindowBase
    {
        KCStorageFacility storageFacility;
        private static HashSet<PartResourceDefinition> allResources = new HashSet<PartResourceDefinition>();
        private Vector2 scrollPos;

        internal static void GetVesselResources()
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

        private bool vesselHasRessources(Vessel v, PartResourceDefinition resource, float amount)
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

        private double getVesselRessources(Vessel v, PartResourceDefinition resource)
        {
            v.GetConnectedResourceTotals(resource.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselAmount;
        }

        private bool facilityHasRessources(PartResourceDefinition resouce, float amount)
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

        private double getFacilityResource(PartResourceDefinition resource)
        {
            return storageFacility.getRessources()[resource];
        }

        private double getFacilitySpace(PartResourceDefinition resource)
        {
            return storageFacility.getEmptyAmount(resource);
        }

        /// <summary>
        /// checks if the vessel v has enough space to add amount of r to it.
        /// </summary>
        private bool vesselHasSpace(Vessel v, PartResourceDefinition r, float amount)
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
        private double getVesselSpace(Vessel v, PartResourceDefinition r)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselMaxAmount - vesselAmount;
        }

        private bool facilityHasSpace(PartResourceDefinition resource, float amount)
        {
            if (storageFacility.getMaxVolume() - storageFacility.getCurrentVolume() >= KCStorageFacility.getVolumeForAmount(resource, amount))
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
            storageFacility.Update();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MaxVolume: {storageFacility.maxVolume[storageFacility.level]:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"UsedVolume: {storageFacility.getCurrentVolume():f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUI.enabled = true;
            List<int> valueList = new List<int> { -100, -10, -1, 1, 10, 100 };

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            Dictionary<PartResourceDefinition, double> resourceCopy = storageFacility.getRessources();
            for (int r = 0; r < resourceCopy.Count; r++)
            {
                KeyValuePair<PartResourceDefinition, double> kvp = resourceCopy.ElementAt(r);
                GUILayout.BeginVertical();
                GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value:f2}", GUILayout.Height(18));

                if (!storageFacility.Colony.CAB.PlayerInColony && !trashResources) { GUI.enabled = false; }
                GUILayout.BeginHorizontal();
                foreach (int i in valueList)
                {
                    if (i < 0 && trashResources || !trashResources)
                    {
                        if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                        {
                            if (i < 0)
                            {
                                if (!trashResources)
                                {
                                    if (vesselHasSpace(FlightGlobals.ActiveVessel, kvp.Key, -i))
                                    {
                                        if (facilityHasRessources(kvp.Key, -i))
                                        {
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)i);
                                            storageFacility.changeAmount(kvp.Key, i);
                                        }
                                        else
                                        {
                                            double amount = getFacilityResource(kvp.Key);
                                            storageFacility.changeAmount(kvp.Key, (float)-amount);
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount);
                                        }
                                    }
                                    else
                                    {
                                        double amount = getVesselSpace(FlightGlobals.ActiveVessel, kvp.Key);

                                        if (facilityHasRessources(kvp.Key, (float)-amount))
                                        {
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)amount);
                                            storageFacility.changeAmount(kvp.Key, (float)-amount);
                                        }
                                        else
                                        {
                                            amount = getFacilityResource(kvp.Key);
                                            storageFacility.changeAmount(kvp.Key, (float)-amount);
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount);
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
                                        storageFacility.changeAmount(kvp.Key, (float)-amount);
                                    }
                                }
                            }
                            else
                            {
                                if (facilityHasSpace(kvp.Key, i))
                                {
                                    if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, i))
                                    {
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)i);
                                        storageFacility.changeAmount(kvp.Key, i);
                                    }
                                    else
                                    {
                                        double amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount);
                                        storageFacility.changeAmount(kvp.Key, (float)amount);
                                    }
                                }
                                else
                                {
                                    double amount = getFacilitySpace(kvp.Key);
                                    if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, (float)amount))
                                    {
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)amount);
                                        storageFacility.changeAmount(kvp.Key, (float)amount);
                                    }
                                    else
                                    {
                                        amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount);
                                        storageFacility.changeAmount(kvp.Key, (float)amount);
                                    }
                                }
                            }
                        }
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(2);

            if (GUILayout.Button("Trash resources", GUILayout.Height(18))) trashResources = !trashResources;
            GUILayout.Label("Warning: enabling the trash resources option will delete the resource instead of transferring it to the vessel.");
            GUILayout.Label($"Trash resources: {trashResources}", GUILayout.Height(18));
        }

        public KCStorageFacilityWindow(KCStorageFacility storageFacility) : base(Configuration.createWindowID(), "Storagefacility")
        {
            this.storageFacility = storageFacility;
            GetVesselResources();
            foreach (PartResourceDefinition resource in allResources)
            {
                storageFacility.addRessource(resource);
            }
            toolRect = new Rect(100, 100, 330, 600);
        }
    }

    internal class KCStorageFacility : KCFacilityBase
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
                double tempAmount = storage.getEmptyAmount(resource);
                if (amount <= tempAmount)
                {
                    storage.changeAmount(resource, (float)amount);
                    return 0;
                }
                else
                {
                    storage.changeAmount(resource, (float)tempAmount);
                    amount -= tempAmount;
                }
            }

            return amount;
        }

        public static List<KCStorageFacility> GetStoragesInColony(colonyClass colony)
        {
            return colony.Facilities.Where(f => f is KCStorageFacility).Select(f => (KCStorageFacility)f).ToList();
        }

        public static List<KCStorageFacility> findFacilityWithResourceType(PartResourceDefinition resource, colonyClass colony)
        {
            return GetStoragesInColony(colony).Where(f => f.getRessources().ContainsKey(resource)).ToList();
        }

        /// <summary>
        /// returns a list of all storage facilities that are not full
        /// </summary>
        public static List<KCStorageFacility> findEmptyStorageFacilities(colonyClass colony)
        {
            return GetStoragesInColony(colony).Where(f => f.currentVolume < f.maxVolume[f.level]).ToList();
        }

        public Dictionary<PartResourceDefinition, double> resources;

        public Dictionary<int, float> maxVolume { get; private set; } = new Dictionary<int, float> { };
        public double currentVolume
        {
            get
            {
                double amount = 0;
                foreach (KeyValuePair<PartResourceDefinition, double> entry in resources)
                {
                    amount += entry.Key.volume * entry.Value;
                }
                return amount;
            }
        }

        public Dictionary<PartResourceDefinition, double> getRessources() { return resources; }
        public void addRessource(PartResourceDefinition r) { resources.TryAdd(r, 0); }

        public void setAmount(PartResourceDefinition resource, float amount)
        {
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
            return maxVolume[level];
        }

        public static double getVolumeForAmount(PartResourceDefinition resource, double amount) { return amount * resource.volume; }

        public double getEmptyAmount(PartResourceDefinition resource)
        {
            return (maxVolume[level] - currentVolume) / resource.volume;
        }

        private KCStorageFacilityWindow StorageWindow;

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

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
        internal bool changeAmount(PartResourceDefinition resource, double amount)
        {
            if (amount < 0)
            {
                if (this.resources.ContainsKey(resource))
                {
                    if (this.resources[resource] >= amount)
                    {
                        this.resources[resource] += amount;
                        return true;
                    }
                }
            }
            else
            {
                if (this.currentVolume + getVolumeForAmount(resource, amount) <= this.maxVolume[level])
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

        public override void OnBuildingClicked()
        {
            StorageWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            StorageWindow.Toggle();
        }

        private void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = node.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());
                if (iLevel.HasValue("maxVolume")) maxVolume[i] = float.Parse(iLevel.GetValue("maxVolume"));
                else if (i > 0) maxVolume[i] = maxVolume[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxVolume (at least for level 0).");
            }
        }

        public KCStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            resources = new Dictionary<PartResourceDefinition, double>();
            StorageWindow = new KCStorageFacilityWindow(this);

            foreach (ConfigNode.Value value in node.GetNode("resources").values)
            {
                PartResourceDefinition prd = PartResourceLibrary.Instance.GetDefinition(value.name);

                if (resources.ContainsKey(prd)) resources[prd] = double.Parse(value.value);
                else resources.Add(prd, double.Parse(value.value));
            }
            foreach (PartResourceDefinition resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (blackListedResources.Contains(resource.name)) { continue; }
                if (!resources.ContainsKey(resource))
                {
                    resources.Add(resource, 0);
                }
            }
        }

        public KCStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            resources = new Dictionary<PartResourceDefinition, double>();
            StorageWindow = new KCStorageFacilityWindow(this);

            foreach (PartResourceDefinition resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (blackListedResources.Contains(resource.name)) { continue; }
                if (!resources.ContainsKey(resource))
                {
                    resources.Add(resource, 0);
                }
            }
        }
    }
}
