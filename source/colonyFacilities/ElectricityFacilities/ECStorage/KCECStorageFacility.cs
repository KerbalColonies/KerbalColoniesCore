using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
