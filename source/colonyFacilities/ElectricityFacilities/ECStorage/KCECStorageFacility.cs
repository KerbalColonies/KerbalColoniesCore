using KerbalColonies.Electricity;
using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using static Targeting.Sample;

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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage
{
    public class KCECStorageFacility : KCFacilityBase, KCECStorage
    {
        public static double ColonyEC(colonyClass colony) => KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony).Sum(f => f.ECStored);
        public static double ColonyECCapacity(colonyClass colony) => KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony).Sum(f => f.ECCapacity);
        public static SortedDictionary<int, KCECStorageFacility> StoragePriority(colonyClass colony)
        {
            SortedDictionary<int, KCECStorageFacility> dict = new SortedDictionary<int, KCECStorageFacility>(KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony)
.ToDictionary(f => f.ECStoragePriority, f => f));
            dict.Reverse();
            return dict;
        }

        public static double AddECToColony(colonyClass colony, double deltaEC)
        {
            StoragePriority(colony).ToList().ForEach(kvp => deltaEC = kvp.Value.ChangeECStored(deltaEC));
            return deltaEC;
        }

        private KCECStorageWindow window;
        public KCECStorageInfo StorageInfo => (KCECStorageInfo)facilityInfo;
        private double eCStored;

        public double ECStored { get => eCStored; set => eCStored = locked ? eCStored : value; }
        public double ECCapacity { get; } = 100000;
        public int ECStoragePriority { get; set; } = 0;
        public bool locked { get; set; } = false;

        public double StoredEC(double lastTime, double deltaTime, double currentTime) => locked ? 0 : ECStored;

        public double ChangeECStored(double deltaEC)
        {
            if (locked) return deltaEC;
            if (deltaEC < 0)
            {
                if (ECStored + deltaEC >= 0)
                {
                    ECStored += deltaEC;
                    deltaEC = 0;
                }
                else
                {
                    deltaEC += ECStored;
                    ECStored = 0;
                }
            }
            else
            {
                if (ECStored + deltaEC <= ECCapacity)
                {
                    ECStored += deltaEC;
                    deltaEC = 0;
                }
                else
                {
                    deltaEC -= ECCapacity - ECStored;
                    ECStored = ECCapacity;
                }
            }

            return deltaEC;
        }

        public void SetStoredEC(double storedEC) => ECStored = locked ? ECStored : Math.Max(0, Math.Min(ECCapacity, storedEC));

        public bool CanTransferToVessel(Vessel v)
        {
            KCECStorageInfo info = StorageInfo;

            CelestialBody body = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == Colony.BodyID);

            double radius = KKgroups.Average(g => KerbalKonstructs.API.GetGroupCenter(g, body.bodyName).RadiusOffset) + body.Radius;
            double squareRadius = radius * radius;
            double unMultiplier = body.gMagnitudeAtCenter / squareRadius;

            float multiplier = info.UseGravityMultiplier[level] ? Math.Max(info.MinGravity[level], Math.Min(info.MaxGravity[level], (float)unMultiplier / 9.80665f)) : 1;
            if (info.UseGravityMultiplier[level] && !Configuration.Paused) Configuration.writeDebug($"KCECStorageWindow: radius: {radius}, radius²: {squareRadius}, unMultiplier: {unMultiplier}");

            List<Type> types = info.RangeTypes[level];
            List<string> names = info.RangeFacilities[level];

            float range = info.TransferRange[level] * multiplier * Configuration.FacilityRangeMultiplier;
            bool canTranfer = Colony.Facilities.Where(f => types.Contains(f.GetType()) ^ names.Contains(f.facilityInfo.name)).Any(f => f.vesselNearFacility(v, (float)range)) || vesselNearFacility(v, (float)range);

            // types | names
            // 0 | 0 -> false
            // 0 | 1 -> true
            // 1 | 0 -> true
            // 1 | 1 -> false
            // xor

            canTranfer &= !locked;
            canTranfer &= v != null && v.LandedOrSplashed && v.srfSpeed <= 0.5; // only allow transfer if the vessel is landed or splashed
        
            return canTranfer;
        }

        public override void OnBuildingClicked() => window.Toggle();

        public override void OnRemoteClicked() => window.Toggle();

        public override void Update()
        {
            base.Update();
            locked = built && locked;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("ECStored", ECStored);
            node.AddValue("Priority", ECStoragePriority);
            node.AddValue("locked", locked);
            return node;
        }

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (node.HasValue("ECStored"))
            {
                ECStored = double.Parse(node.GetValue("ECStored"));
                ECStoragePriority = int.Parse(node.GetValue("Priority"));
            }
            if (node.HasValue("locked")) locked = bool.Parse(node.GetValue("locked"));

            window = new KCECStorageWindow(this);
        }

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCECStorageWindow(this);
        }
    }
}
