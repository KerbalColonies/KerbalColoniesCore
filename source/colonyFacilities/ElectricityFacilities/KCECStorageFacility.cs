using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

            #region colonyWide
            GUILayout.Label($"Stored electric charge in this colony: {KCECStorageFacility.ColonyEC(ecStorage.Colony)} / {KCECStorageFacility.ColonyECCapacity(ecStorage.Colony)} EC");
            GUILayout.BeginHorizontal();
            foreach (double i in valueList)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change ECStored by {i} for {ecStorage.DisplayName} facility");
                    KCECStorageFacility.AddECToColony(ecStorage.Colony, i);
                }
            }
            GUILayout.EndHorizontal();
            #endregion

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

            if (FlightGlobals.ActiveVessel != null)
            {
                PartResourceDefinition ec = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

                GUILayout.Label($"Vessel transfer:");
                GUILayout.BeginHorizontal();
                foreach (double i in valueList)
                {
                    FlightGlobals.ActiveVessel.GetConnectedResourceTotals(ec.id, false, out double vesselAmount, out double vesselMaxAmount);
                    //if (i < 0 && vesselMaxAmount - vesselAmount < -i) GUI.enabled = false;

                    if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                    {
                        Configuration.writeLog($"Transfering {i} EC from EC storage facility {ecStorage.DisplayName} to vessel {FlightGlobals.ActiveVessel.vesselName}.");

                        if (i < 0)
                        {
                            if (KCStorageFacilityWindow.vesselHasSpace(FlightGlobals.ActiveVessel, ec, -i))
                            {
                                double left = ecStorage.ChangeECStored(i);

                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, i - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                            else
                            {
                                double amount = KCStorageFacilityWindow.getVesselSpace(FlightGlobals.ActiveVessel, ec);
                                double left = ecStorage.ChangeECStored(-amount);

                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, -amount - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }

                        }
                        else
                        {
                            if (KCStorageFacilityWindow.vesselHasRessources(FlightGlobals.ActiveVessel, ec, i))
                            {
                                double left = ecStorage.ChangeECStored(i);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, i - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                            else
                            {
                                double amount = KCStorageFacilityWindow.getVesselRessources(FlightGlobals.ActiveVessel, ec);
                                double left = ecStorage.ChangeECStored(amount);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, amount - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                        }
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                ecStorage.locked = GUILayout.Toggle(ecStorage.locked, "Lock storage", GUILayout.Height(18), GUILayout.Width(100));
            }
        }

        public KCECStorageWindow(KCECStorageFacility facility) : base(facility, Configuration.createWindowID())
        {
            toolRect = new Rect(100, 100, 400, 800);
        }
    }

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
        private double eCStored;

        public double ECStored { get => eCStored; set => eCStored = locked ? eCStored : value; }
        public double ECCapacity { get; set; } = 100000;
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

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("ECStored", ECStored);
            node.AddValue("Priority", ECStoragePriority);
            return node;
        }

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (node.HasValue("ECStored"))
            {
                ECStored = double.Parse(node.GetValue("ECStored"));
                ECStoragePriority = int.Parse(node.GetValue("Priority"));
            }

            window = new KCECStorageWindow(this);
        }

        public KCECStorageFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCECStorageWindow(this);
        }
    }
}
