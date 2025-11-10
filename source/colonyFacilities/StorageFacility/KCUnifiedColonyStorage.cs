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
        public double UsedVolume => Resources.Sum(kvp => kvp.Key.volume * kvp.Value);
        // Difference between total volume and resource volume is subtracted from used volume becaues the other resources can be stored in other facilities
        // Although this can give false results for some specific cases I think it's good enough for now
        public double UsedResourceVolume(PartResourceDefinition resource) => UsedVolume - Volume + ResourceVolume(resource);
        public double FreeVolume => Volume - UsedVolume;
        public SortedDictionary<PartResourceDefinition, double> Resources { get; protected set; } = new SortedDictionary<PartResourceDefinition, double>(Comparer<PartResourceDefinition>.Create((x, y) => x.displayName.CompareTo(y.displayName)));
        public double MaxStorable(PartResourceDefinition resource) => UsedResourceVolume(resource) * resource.volume;

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

        public bool PlayerInRange()
        {
            if (FlightGlobals.ActiveVessel == null) return false;

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
                bool canTranfer = fac.Colony.Facilities.Where(f => types.Contains(f.GetType()) ^ names.Contains(f.facilityInfo.name)).Any(f => f.playerNearFacility((float)range)) || fac.playerNearFacility((float)range);
                canTranfer &= fac.enabled;
                canTranfer &= FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.LandedOrSplashed && FlightGlobals.ship_srfSpeed <= 0.5; // only allow transfer if the vessel is landed or splashed

                return canTranfer;
            });
        }


        public static void LoadColony(colonyClass colony)
        {
            throw new NotImplementedException();
        }

        public static void SaveColony(colonyClass colony)
        {
            throw new NotImplementedException();
        }


        public static KCUnifiedColonyStorage GetOrCreateColonyStorage(colonyClass colony, KCStorageFacility facility)
        {
            KCUnifiedColonyStorage storage = colonyStorages.GetValueOrDefault(colony, new KCUnifiedColonyStorage(colony));
            storage.storageFacilities.Add(facility);
            return storage;
        }


        protected KCUnifiedColonyStorage(colonyClass colony)
        {
            colonyStorages[colony] = this;
        }
    }
}
