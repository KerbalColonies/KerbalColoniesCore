using KerbalColonies.ResourceManagment;
using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Targeting.Sample;

namespace KerbalColonies.colonyFacilities.StorageFacility
{
    public class KCUnifiedColonyStorage : IKCResourceStorage
    {
        public static Dictionary<colonyClass, KCUnifiedColonyStorage> colonyStorages = new Dictionary<colonyClass, KCUnifiedColonyStorage>();

        public List<KCStorageFacility> storageFacilities = new List<KCStorageFacility>();

        public int Priority { get; set; } = 0;
        public double Volume => storageFacilities.Sum(facility => facility.locked ? 0 : facility.maxVolume);
        public double ResourceVolume(PartResourceDefinition resource) => storageFacilities.Where(fac => fac.CanStoreResource(resource) && !fac.locked).Sum(fac => fac.maxVolume);
        public double UsedResourceVolume(PartResourceDefinition resource) => Resources.GetValueOrDefault(resource, 0) * (resource.volume == 0 ? 1 : resource.volume);
        public double UsedVolume => Resources.Sum(kvp => (kvp.Key.volume == 0 ? 1 : kvp.Key.volume) * kvp.Value);
        // Difference between total volume and resource volume is subtracted from used volume becaues the other resources can be stored in other facilities
        // Although this can give false results for some specific cases I think it's good enough for now
        public double FreeResourceVolume(PartResourceDefinition resource)
        {
            // Free Volume for a particular resource
            // Volume - ResourceVolume -> Volume useable by other resources
            // - UsedVolume -> Volume already used by all resources
            // -> Math.Min(0, Volume - ResourceVolume - UsedVolume) + ResourceVolume
            // 
            // Total: 1000, Used: 0, ResourceVolume: 400 -> (1000 - 400 - 0) + 400 = 400
            // Total: 1000, Used: 800, ResourceVolume: 400 -> (1000 - 400 - 800) + 400 = 200


            // Total: 3205, Used: 163, ResourceVolume: 25 -> (3205 - 25 - 163) + 25 = 25

            double ResourceVol = ResourceVolume(resource);
            double UsedResourceVol = UsedResourceVolume(resource);
            double UsedVol = UsedVolume;
            double TotalVol = Volume;

            return Math.Min(0, TotalVol - ResourceVol - UsedVol) + ResourceVol - UsedResourceVol;
        }

        public double FreeVolume => Volume - UsedVolume;
        public SortedDictionary<PartResourceDefinition, double> Resources { get; protected set; } = new SortedDictionary<PartResourceDefinition, double>(Comparer<PartResourceDefinition>.Create((x, y) => x.displayName.CompareTo(y.displayName)));
        public double MaxStorable(PartResourceDefinition resource) => FreeResourceVolume(resource) / resource.volume;

        public SortedDictionary<PartResourceDefinition, double> StoredResources(double lastTime, double deltaTime, double currentTime) => Resources;


        public double ChangeResourceStored(PartResourceDefinition resource, double Amount)
        {
            if (Amount > 0)
            {
                double freeAmount = MaxStorable(resource);
                double storeAmount = Math.Min(Amount, freeAmount);
                Resources[resource] = Resources.GetValueOrDefault(resource) + storeAmount;
                return Amount - storeAmount;
            }
            else
            {
                double storedAmount = Resources.GetValueOrDefault(resource);
                double removeAmount = Math.Min(-Amount, storedAmount);
                Resources[resource] = storedAmount - removeAmount;
                return Amount + removeAmount;
            }
        }

        public bool VesselInRange(Vessel v)
        {
            return storageFacilities.Any(fac =>
            {
                KCStorageFacilityInfo info = fac.storageInfo;

                CelestialBody body = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == fac.Colony.BodyID);

                double radius = fac.KKgroups.Average(g => KerbalKonstructs.API.GetGroupCenter(g, body.bodyName).RadiusOffset) + body.Radius;
                double squareRadius = radius * radius;
                double unMultiplier = body.gMagnitudeAtCenter / squareRadius;

                float multiplier = info.UseGravityMultiplier[fac.level] ? Math.Max(info.MinGravity[fac.level], Math.Min(info.MaxGravity[fac.level], (float)unMultiplier / 9.80665f)) : 1;
                if (info.UseGravityMultiplier[fac.level] && !Configuration.Paused) Configuration.writeDebug($"KCECStorageWindow: radius: {radius}, radius²: {squareRadius}, unMultiplier: {unMultiplier}");


                List<Type> types = info.RangeTypes[fac.level];
                List<string> names = info.RangeFacilities[fac.level];

                float range = info.TransferRange[fac.level] * multiplier * Configuration.FacilityRangeMultiplier;
                bool canTranfer = fac.Colony.Facilities.Where(f => types.Contains(f.GetType()) ^ names.Contains(f.facilityInfo.name)).Any(f => f.vesselNearFacility(v, (float)range)) || fac.vesselNearFacility(v, (float)range);
                canTranfer &= fac.enabled;
                canTranfer &= v.LandedOrSplashed && v.srfSpeed <= 0.5; // only allow transfer if the vessel is landed or splashed

                return canTranfer;
            });
        }

        public static void SaveColony(colonyClass colony)
        {
            if (!colonyStorages.ContainsKey(colony)) return;

            ConfigNode storageNode = new ConfigNode("KCUnifiedColonyStorage");
            KCUnifiedColonyStorage colonyStorage = colonyStorages[colony];

            storageNode.AddValue("Priority", colonyStorage.Priority);

            ConfigNode resourceNode = new ConfigNode("Resources");
            colonyStorage.Resources.ToList().ForEach(kvp =>
            {
                resourceNode.AddValue(kvp.Key.name, kvp.Value);
            });
            storageNode.AddNode(resourceNode);

            colony.sharedColonyNodes.RemoveAll(n => n.name == "KCUnifiedColonyStorage");
            colony.sharedColonyNodes.Add(storageNode);
        }


        public static void KCColonyLoad(colonyClass colony)
        {
            colonyStorages.Remove(colony);
            colonyStorages[colony] = new KCUnifiedColonyStorage(colony);
        }

        public static KCUnifiedColonyStorage GetOrCreateColonyStorage(colonyClass colony, KCStorageFacility facility)
        {
            KCUnifiedColonyStorage storage;
            if (colonyStorages.ContainsKey(colony)) storage = colonyStorages[colony];
            else storage = new KCUnifiedColonyStorage(colony);
            storage.storageFacilities.Add(facility);
            return storage;
        }


        protected KCUnifiedColonyStorage(colonyClass colony)
        {
            colonyStorages[colony] = this;

            KCResourceManager.otherStorages.TryAdd(colony, new SortedDictionary<int, List<IKCResourceStorage>>());
            KCResourceManager.otherStorages[colony].TryAdd(Priority, new List<IKCResourceStorage>());
            KCResourceManager.otherStorages[colony].ToList().ForEach(kvp => kvp.Value.RemoveAll(s => s is KCUnifiedColonyStorage));
            KCResourceManager.otherStorages[colony][Priority].Add(this);

            ConfigNode storageNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "KCUnifiedColonyStorage");

            if (storageNode != null)
            {
                Priority = int.Parse(storageNode.GetValue("Priority") ?? "0");
                ConfigNode resourceNode = storageNode.GetNode("Resources");
                if (resourceNode != null)
                {
                    foreach (ConfigNode.Value v in resourceNode.values)
                    {
                        Resources.Add(PartResourceLibrary.Instance.GetDefinition(v.name), double.Parse(v.value));
                    }
                }
            }
        }
    }
}
