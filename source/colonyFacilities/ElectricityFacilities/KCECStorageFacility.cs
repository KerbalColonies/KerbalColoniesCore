using KerbalColonies.Electricity;
using KerbalColonies.UI;
using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities
{
    public class KCECStorageWindow : KCFacilityWindowBase
    {
        KCECStorageFacility ecStorage => (KCECStorageFacility)facility;

        List<double> valueList = new List<double> { -10000, -1000, -100, -10, -1, 1, 10, 100, 1000, 10000 };
        List<int> ints = new List<int> { -10, -5, -1, 1, 5, 10 };

        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();
            GUILayout.Label($"Stored electric charge: {ecStorage.ECStored} / {ecStorage.ECCapacity} EC");

            GUILayout.BeginHorizontal();
            foreach (double i in valueList)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change ECStored by {i} for {ecStorage.DisplayName} facility");
                    ecStorage.ChangeECStored(i);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Current priority: {ecStorage.ECStoragePriority}");

            GUILayout.BeginHorizontal();
            foreach (int i in ints)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change priority by {i} for {ecStorage.DisplayName} facility");
                    ecStorage.ECStoragePriority += i;
                }
            }
            GUILayout.EndHorizontal();
        }

        public KCECStorageWindow(KCECStorageFacility facility) : base(facility, Configuration.createWindowID())
        {
            toolRect = new Rect(100, 100, 400, 800);
        }
    }

    public class KCECStorageFacility : KCFacilityBase, KCECStorage
    {
        private KCECStorageWindow window;

        public double ECStored { get; set; }
        public double ECCapacity { get; set; } = 100000;
        public int ECStoragePriority { get; set; } = 0;

        public double StoredEC(double lastTime, double deltaTime, double currentTime) => ECStored;

        public double ChangeECStored(double deltaEC)
        {
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

        public void SetStoredEC(double storedEC) => ECStored = Math.Max(0, Math.Min(ECCapacity, storedEC));

        public override void OnBuildingClicked() => window.Toggle();

        public override void OnRemoteClicked() => window.Toggle();

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            window = new KCECStorageWindow(this);
        }

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCECStorageWindow(this);
        }
    }
}
