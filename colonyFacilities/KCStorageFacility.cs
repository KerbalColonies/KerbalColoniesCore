using Contracts.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCStorageFacilityCost : KCFacilityCostClass
    {
        public KCStorageFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, double>> {
                { 0, new Dictionary<PartResourceDefinition, double> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500 } } },
                { 1, new Dictionary<PartResourceDefinition, double> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500 } } }
            };
        }
    }

    internal class KCStorageFacilityWindow : KCWindowBase
    {
        KCStorageFacility storageFacility;
        private static HashSet<PartResourceDefinition> allResources = new HashSet<PartResourceDefinition>();
        private static HashSet<string> blackListedResources = new HashSet<string> { "ElectricCharge", "IntakeAir" };
        private Vector2 scrollPos;

        internal static void GetVesselResources()
        {
            if (FlightGlobals.ActiveVessel == null) { return; }

            double amount = 0;
            double maxAmount = 0;
            foreach (PartResourceDefinition availableResource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (blackListedResources.Contains(availableResource.name)) { continue; }
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
            if (storageFacility.getMaxVolume() - storageFacility.getCurrentVolume() >= storageFacility.getVolumeForAmount(resource, amount))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void CustomWindow()
        {
            storageFacility.Update();
            //int maxVolume = (int)Math.Round(KCStorageFacility.maxVolume, 0);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MaxVolume: {storageFacility.maxVolume}", LabelGreen, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"UsedVolume: {storageFacility.getCurrentVolume()}", LabelGreen, GUILayout.Height(18));
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
                GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value}", GUILayout.Height(18));

                if (FlightGlobals.ActiveVessel == null) { GUI.enabled = false; }
                GUILayout.BeginHorizontal();
                foreach (int i in valueList)
                {
                    if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                    {
                        if (i < 0)
                        {
                            if (vesselHasSpace(FlightGlobals.ActiveVessel, kvp.Key, -i))
                            {
                                if (facilityHasRessources(kvp.Key, -i))
                                {
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)i);
                                    storageFacility.changeAmount(kvp.Key, i);
                                    Configuration.saveColonies = true;
                                }
                                else
                                {
                                    double amount = getFacilityResource(kvp.Key);
                                    storageFacility.changeAmount(kvp.Key, (float)-amount);
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount);
                                    Configuration.saveColonies = true;
                                }
                            }
                            else
                            {
                                double amount = getVesselSpace(FlightGlobals.ActiveVessel, kvp.Key);

                                if (facilityHasRessources(kvp.Key, (float)-amount))
                                {
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)amount);
                                    storageFacility.changeAmount(kvp.Key, (float)-amount);
                                    Configuration.saveColonies = true;
                                }
                                else
                                {
                                    amount = getFacilityResource(kvp.Key);
                                    storageFacility.changeAmount(kvp.Key, (float)-amount);
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount);
                                    Configuration.saveColonies = true;
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
                                    Configuration.saveColonies = true;
                                }
                                else
                                {
                                    double amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount);
                                    storageFacility.changeAmount(kvp.Key, (float)amount);
                                    Configuration.saveColonies = true;
                                }
                            }
                            else
                            {
                                double amount = getFacilitySpace(kvp.Key);
                                if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, (float)amount))
                                {
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, (double)amount);
                                    storageFacility.changeAmount(kvp.Key, (float)amount);
                                    Configuration.saveColonies = true;
                                }
                                else
                                {
                                    amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount);
                                    storageFacility.changeAmount(kvp.Key, (float)amount);
                                    Configuration.saveColonies = true;
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
        }

        public KCStorageFacilityWindow(KCStorageFacility storageFacility) : base(Configuration.createWindowID(storageFacility), "Storagefacility")
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

    [System.Serializable]
    internal class KCStorageFacility : KCFacilityBase
    {
        public static double colonyResources(PartResourceDefinition resource, string saveGame, int bodyIndex, string colonyName)
        {
            double amount = 0;
            List<KCStorageFacility> storages = findFacilityWithResourceType(resource, saveGame, bodyIndex, colonyName);
            foreach (KCStorageFacility storage in storages)
            {
                amount += storage.getRessources()[resource];
            }
            return amount;
        }

        public static double maxColonySpace(PartResourceDefinition resource, double amount, string saveGame, int bodyIndex, string colonyName)
        {
            List<KCStorageFacility> storages = findEmptyStorageFacilities(saveGame, bodyIndex, colonyName);
            double space = 0;
            foreach (KCStorageFacility storage in storages)
            {
                space += storage.getEmptyAmount(resource);
            }
            return space;
        }

        public static bool colonyHasSpace(PartResourceDefinition resource, double amount, string saveGame, int bodyIndex, string colonyName)
        {
            List<KCStorageFacility> storages = findEmptyStorageFacilities(saveGame, bodyIndex, colonyName);
            foreach (KCStorageFacility storage in storages)
            {
                if (amount <= storage.getEmptyAmount(resource))
                {
                    return true;
                }
                else
                {
                    amount -= storage.getEmptyAmount(resource);
                }
            }
            return false;
        }

        // TODO: make it compatible for negative amounts
        public static double addResourceToColony(PartResourceDefinition resource, double amount, string saveGame, int bodyIndex, string colonyName)
        {
            List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(resource, saveGame, bodyIndex, colonyName);

            if (storages.Count == 0)
            {
                storages = KCStorageFacility.findEmptyStorageFacilities(saveGame, bodyIndex, colonyName);
            }

            foreach (KCStorageFacility storage in storages)
            {
                if (amount <= storage.getEmptyAmount(resource))
                {
                    storage.changeAmount(resource, (float)amount);
                    return 0;
                }
                else
                {
                    double tempAmount = storage.getEmptyAmount(resource);
                    storage.changeAmount(resource, (float)tempAmount);
                    amount -= tempAmount;
                }
            }

            return amount;
        }

        public static List<KCStorageFacility> findFacilityWithResourceType(PartResourceDefinition resource, string saveGame, int bodyIndex, string colonyName)
        {
            if (!Configuration.coloniesPerBody.ContainsKey(saveGame)) { return new List<KCStorageFacility> { }; }
            else if (!Configuration.coloniesPerBody[saveGame].ContainsKey(bodyIndex)) { return new List<KCStorageFacility> { }; }
            else if (!Configuration.coloniesPerBody[saveGame][bodyIndex].ContainsKey(colonyName)) { return new List<KCStorageFacility> { }; }

            List<KCStorageFacility> storages = new List<KCStorageFacility>();

            Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Values.ToList().ForEach(UUIDdict =>
            {
                UUIDdict.Values.ToList().ForEach(colonyFacilitys =>
                {
                    colonyFacilitys.ForEach(colonyFacility =>
                    {
                        if (typeof(KCStorageFacility).IsAssignableFrom(colonyFacility.GetType()))
                        {
                            KCStorageFacility fac = (KCStorageFacility)colonyFacility;
                            if (fac.getRessources().ContainsKey(resource))
                            {
                                if (!storages.Contains((KCStorageFacility)colonyFacility))
                                {
                                    storages.Add((KCStorageFacility)colonyFacility);
                                }
                            }
                        }
                    });
                });
            });
            return storages;
        }

        /// <summary>
        /// returns a list of all storage facilities that are not full
        /// </summary>
        public static List<KCStorageFacility> findEmptyStorageFacilities(string saveGame, int bodyIndex, string colonyName)
        {
            if (!Configuration.coloniesPerBody.ContainsKey(saveGame)) { return new List<KCStorageFacility> { }; }
            else if (!Configuration.coloniesPerBody[saveGame].ContainsKey(bodyIndex)) { return new List<KCStorageFacility> { }; }
            else if (!Configuration.coloniesPerBody[saveGame][bodyIndex].ContainsKey(colonyName)) { return new List<KCStorageFacility> { }; }

            List<KCStorageFacility> storages = new List<KCStorageFacility>();

            Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Values.ToList().ForEach(UUIDdict =>
            {
                UUIDdict.Values.ToList().ForEach(colonyFacilitys =>
                {
                    colonyFacilitys.ForEach(colonyFacility =>
                    {
                        if (typeof(KCStorageFacility).IsAssignableFrom(colonyFacility.GetType()))
                        {
                            KCStorageFacility fac = (KCStorageFacility)colonyFacility;
                            if (fac.currentVolume < fac.maxVolume)
                            {
                                if (!storages.Contains((KCStorageFacility)colonyFacility))
                                {
                                    storages.Add((KCStorageFacility)colonyFacility);
                                }
                            }
                        }
                    });
                });
            });
            return storages;
        }

        [NonSerialized]
        public Dictionary<PartResourceDefinition, double> resources;

        public double maxVolume;
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
            return maxVolume;
        }

        public double getVolumeForAmount(PartResourceDefinition resource, double amount) { return amount * resource.volume; }

        public double getEmptyAmount(PartResourceDefinition resource)
        {
            return (maxVolume - currentVolume) / resource.volume;
        }

        private KCStorageFacilityWindow StorageWindow;

        public override int GetUpgradeTime(int level)
        {
            // 1 Kerbin day = 0.25 days
            // 100 per day * 5 engineers = 500 per day
            // 500 per day * 4 kerbin days = 500

            // 1 Kerbin day = 0.25 days
            // 100 per day * 5 engineers = 500 per day
            // 500 per day * 2 kerbin days = 250
            int[] buildTimes = { 500, 250 };
            return buildTimes[level];
        }

        public override ConfigNode getCustomNode()
        {
            ConfigNode node = new ConfigNode("resources");

            node.AddValue("maxVolume", maxVolume);

            foreach (KeyValuePair<PartResourceDefinition, double> entry in resources)
            {
                node.AddValue(entry.Key.name, entry.Value);
            }

            return node;
        }

        public override void loadCustomNode(ConfigNode customNode)
        {
            if (customNode != null)
            {
                maxVolume = double.Parse(customNode.GetValue("maxVolume"));
                customNode.RemoveValue("maxVolume");


                foreach (ConfigNode.Value value in customNode.values)
                {
                    PartResourceDefinition prd = PartResourceLibrary.Instance.GetDefinition(value.name);
                    if (!resources.ContainsKey(prd))
                    {
                        resources.Add(prd, double.Parse(value.value));
                    }
                    else
                    {
                        resources[prd] = double.Parse(value.value);
                    }
                }
            }
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
                if (this.currentVolume + getVolumeForAmount(resource, amount) <= this.maxVolume)
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
            KSPLog.print("KCStorageWindow: " + StorageWindow.ToString());
            StorageWindow.Toggle();
        }

        public override void Initialize()
        {
            resources = new Dictionary<PartResourceDefinition, double>();
            base.Initialize();
            this.StorageWindow = new KCStorageFacilityWindow(this);

            this.upgradeType = UpgradeType.withAdditionalGroup;

            float[] maxVolumes = { 80000f, 100000f };
            maxVolume = maxVolumes[level];

            string[] baseGroupNames = { "KC_SF_0", "KC_SF_1" };
            baseGroupName = baseGroupNames[level];
        }

        public override void UpdateBaseGroupName()
        {
            switch (this.level)
            {
                default:
                case 0:
                    this.baseGroupName = "KC_SF_0";
                    break;
                case 1:
                    this.baseGroupName = "KC_SF_1";
                    break;
            }
        }


        public override bool UpgradeFacility(int level)
        {
            float[] maxVolumes = { 80000f, 100000f };
            maxVolume = maxVolumes[level];
            return base.UpgradeFacility(level);
        }

        public KCStorageFacility(bool enabled) : base("KCStorageFacility", enabled, 0, 1)
        {
            maxVolume = 2000f;
        }
    }
}
