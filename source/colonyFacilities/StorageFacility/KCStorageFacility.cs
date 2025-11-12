using KerbalColonies.Electricity;
using KerbalColonies.ResourceManagment;
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
    public class KCStorageFacility : KCFacilityBase
    {
        public static HashSet<string> blackListedResources = new HashSet<string> { "IntakeAir" };

        public KCStorageFacilityInfo storageInfo { get { return (KCStorageFacilityInfo)facilityInfo; } }
        public KCUnifiedColonyStorage unifiedColonyStorage;
        public bool outOfEC { get; protected set; } = false;
        public bool locked { get; set; } = false;

        public double maxVolume => storageInfo.maxVolume[level];

        public bool CanStoreResource(PartResourceDefinition resource) => !blackListedResources.Contains(resource.name) && (!storageInfo.resourceBlacklist[level].Contains(resource) || storageInfo.resourceWhitelist[level].Contains(resource));

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            enabled = !outOfEC && !locked && built;
        }

        private KCStorageFacilityWindow StorageWindow;

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            node.AddValue("locked", locked);

            return node;
        }

        public override string GetFacilityProductionDisplay() => $"{unifiedColonyStorage.UsedVolume:f2}/{unifiedColonyStorage.Volume:f2}m³ used\n{unifiedColonyStorage.Resources.Count} resources stored {(facilityInfo.ECperSecond[level] > 0 ? $"\n{(locked ? 0 : facilityInfo.ECperSecond[level]):f2} EC/s" : "")}";

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
            unifiedColonyStorage = KCUnifiedColonyStorage.GetOrCreateColonyStorage(colony, this);
            StorageWindow = new KCStorageFacilityWindow(this);

            if (node.HasNode("resources"))
            {
                foreach (ConfigNode.Value value in node.GetNode("resources").values)
                {
                    PartResourceDefinition prd = PartResourceLibrary.Instance.GetDefinition(value.name);
                    if (prd == null) continue;

                    unifiedColonyStorage.ChangeResourceStored(prd, double.Parse(value.value));
                }
            }

            if (bool.TryParse(node.GetValue("locked"), out bool isLocked)) locked = isLocked;
        }

        public KCStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            unifiedColonyStorage = KCUnifiedColonyStorage.GetOrCreateColonyStorage(colony, this);
            StorageWindow = new KCStorageFacilityWindow(this);
        }
    }
}
