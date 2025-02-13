using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCStorageFacilityCost : KCFacilityCostClass
    {
        public override bool VesselHasRessources(Vessel vessel, int level)
        {
            for (int i = 0; i < resourceCost[level].Count; i++)
            {
                vessel.GetConnectedResourceTotals(resourceCost[level].ElementAt(i).Key.id, false, out double amount, out double maxAmount);

                if (amount < resourceCost[level].ElementAt(i).Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool RemoveVesselRessources(Vessel vessel, int level)
        {
            if (VesselHasRessources(vessel, 0))
            {
                for (int i = 0; i < resourceCost[level].Count; i++)
                {
                    vessel.RequestResource(vessel.rootPart, resourceCost[level].ElementAt(i).Key.id, resourceCost[level].ElementAt(i).Value, true);
                }
                return true;
            }
            return false;
        }

        public KCStorageFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, float>> {
                { 0, new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500f } } },
                { 1, new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500f } } }
            };
        }
    }

    internal class KCStorageFacilityWindow : KCWindowBase
    {
        KCStorageFacility storageFacility;
        private static HashSet<PartResourceDefinition> allResources = new HashSet<PartResourceDefinition>();
        private static HashSet<string> blackListedResources = new HashSet<string> { "ElectricCharge", "IntakeAir" };


        internal static void GetVesselResources()
        {
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

            GUILayout.BeginScrollView(new Vector2());
            Dictionary<PartResourceDefinition, double> resourceCopy = storageFacility.getRessources();
            for (int r = 0; r < resourceCopy.Count; r++)
            {
                KeyValuePair<PartResourceDefinition, double> kvp = resourceCopy.ElementAt(r);
                GUILayout.BeginVertical();
                GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value}", GUILayout.Height(18));

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

        public override void Update()
        {
            base.Update();

            if (maxVolume == 0f)
            {
                GameObject instace = KerbalKonstructs.API.GetGameObject(KCFacilityBase.GetUUIDbyFacility(this));
                if (instace != null)
                {
                    Vector3 size = instace.GetRendererBounds().extents;
                    maxVolume = (size.x * size.y * size.z);
                }
            }
        }

        public override void OnBuildingClicked()
        {
            KSPLog.print("KCStorageWindow: " + StorageWindow.ToString());
            StorageWindow.Toggle();
        }

        public override void Initialize(string facilityData)
        {
            resources = new Dictionary<PartResourceDefinition, double>();
            base.Initialize(facilityData);
            this.StorageWindow = new KCStorageFacilityWindow(this);

            this.upgradeType = UpgradeType.withAdditionalGroup;

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

        public KCStorageFacility(bool enabled, string facilityData = "", float maxVolume = 0f) : base("KCStorageFacility", enabled, facilityData, 0, 1)
        {
            this.maxVolume = maxVolume;
        }

        public KCStorageFacility(bool enabled, string facilityData = "") : base("KCStorageFacility", enabled, facilityData, 0, 1)
        {
            maxVolume = 2000f;
        }
    }
}
