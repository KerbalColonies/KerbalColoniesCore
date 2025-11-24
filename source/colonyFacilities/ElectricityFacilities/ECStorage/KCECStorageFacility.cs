using KerbalColonies.colonyFacilities.StorageFacility;
using System;
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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage
{
    public class KCECStorageFacility : KCStorageFacility
    {
        public static double ColonyEC(colonyClass colony) => KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony).Sum(f => f.ECStored);
        public static double ColonyECCapacity(colonyClass colony) => KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony).Sum(f => f.ECCapacity);
        public static SortedDictionary<int, KCECStorageFacility> StoragePriority(colonyClass colony)
        {
            SortedDictionary<int, KCECStorageFacility> dict = new(KCFacilityBase.GetAllTInColony<KCECStorageFacility>(colony)
.ToDictionary(f => f.ECStoragePriority, f => f));
            dict.Reverse();
            return dict;
        }

        public KCECStorageInfo StorageInfo => (KCECStorageInfo)facilityInfo;
        private double eCStored;

        public double ECStored { get => eCStored; set => eCStored = locked ? eCStored : value; }
        public double ECCapacity { get; } = 100000;
        public int ECStoragePriority { get; set; } = 0;

        public SortedDictionary<PartResourceDefinition, double> Resources => throw new NotImplementedException();

        public double Volume => ECCapacity;

        public double UsedVolume => ECStored;

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (node.HasValue("ECStored"))
            {
                unifiedColonyStorage.ChangeResourceStored(PartResourceLibrary.Instance.GetDefinition("ElectricCharge"), double.Parse(node.GetValue("ECStored")));
            }

            locked = bool.Parse(node.GetValue("locked"));
        }

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
        }
    }
}
