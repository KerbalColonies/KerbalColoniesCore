using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCMiningFacilityCost : KCFacilityCostClass
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

        public KCMiningFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, float>> {
                { 0, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500f } } },
                { 1, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1000f } }
                }
            };
        }
    }

    internal class KCMiningFacilityWindow : KCWindowBase
    {
        KCMiningFacility miningFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            miningFacility.Update();

            if (kerbalGUI == null)
            {
                KCFacilityBase.GetInformationByFacilty(miningFacility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
                kerbalGUI = new KerbalGUI(miningFacility, saveGame, bodyIndex, colonyName);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stored ore: " + miningFacility.Ore);
            GUILayout.Label("Max ore: " + miningFacility.MaxOre);
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve Ore"))
            {
                miningFacility.RetrieveResources();
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            kerbalGUI.ksg.Close();
            kerbalGUI.transferWindow = false;
        }


        public KCMiningFacilityWindow(KCMiningFacility miningFacility) : base(Configuration.createWindowID(miningFacility), "Miningfacility")
        {
            this.miningFacility = miningFacility;
            toolRect = new Rect(100, 100, 400, 800);
            this.kerbalGUI = null;
        }
    }

    [System.Serializable]
    internal class KCMiningFacility : KCKerbalFacilityBase
    {
        private KCMiningFacilityWindow miningFacilityWindow;

        double ore;
        double metalOre;

        public double Ore { get { return ore; } }
        public double MetalOre { get { return metalOre; } }
        public double MaxOre { get { return maxOretList[level]; } }
        public double MaxMetalOre { get { return maxMetalOretList[level]; } }

        private List<float> maxOretList = new List<float> { 2000f, 4000f };
        private List<float> maxMetalOretList = new List<float> { 400f, 800f };
        private List<float> OrePerDayperEngineer = new List<float> { 20f, 20f };
        private List<float> MetalOrePerDayperEngineer = new List<float> { 6f, 8f };
        private List<int> maxKerbalsPerLevel = new List<int> { 8, 12 };

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Engineer").ToList();
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            ore = Math.Min(maxOretList[level], ore + (float)((OrePerDayperEngineer[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            metalOre = Math.Min(maxMetalOretList[level], metalOre + (float)((MetalOrePerDayperEngineer[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            Configuration.SaveColonies();
        }

        public override void OnBuildingClicked()
        {
            if (miningFacilityWindow.IsOpen())
            {
                miningFacilityWindow.Close();
                miningFacilityWindow.kerbalGUI.ksg.Close();
                miningFacilityWindow.kerbalGUI.transferWindow = false;
            }
            else
            {
                miningFacilityWindow.Open();
            }
        }

        // TODO: test this function
        public bool RetrieveResources()
        {
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
            if (ore > 0)
            {
                List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(PartResourceLibrary.Instance.GetDefinition("Ore"), saveGame, bodyIndex, colonyName);

                foreach (KCStorageFacility storage in storages)
                {
                    double tempAmount = ore - storage.getEmptyAmount();
                    storage.changeAmount((float) tempAmount);
                    ore -= tempAmount;
                }
            }

            if (metalOre > 0)
            {
                List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(PartResourceLibrary.Instance.GetDefinition("MetalOre"), saveGame, bodyIndex, colonyName);
                foreach (KCStorageFacility storage in storages)
                {
                    double tempAmount = metalOre - storage.getEmptyAmount();
                    storage.changeAmount((float)tempAmount);
                    metalOre -= tempAmount;
                }
            }
            return true;
        }

        public override bool UpgradeFacility(int level)
        {
            base.UpgradeFacility(level);
            maxKerbals = maxKerbalsPerLevel[level];
            return true;
        }

        public override void EncodeString()
        {
            string kerbalString = CreateKerbalString(kerbals);
            facilityData = $"ore&{ore}|metalOre&{metalOre}|maxKerbals&{maxKerbals}{((kerbalString != "") ? $"|{kerbalString}" : "")}";
        }

        public override void DecodeString()
        {
            if (facilityData != "")
            {
                string[] facilityDatas = facilityData.Split(new[] { '|' }, 4);
                ore = float.Parse(facilityDatas[0].Split('&')[1]);
                ore = float.Parse(facilityDatas[1].Split('&')[1]);
                maxKerbals = Convert.ToInt32(facilityDatas[2].Split('&')[1]);
                if (facilityDatas.Length > 2)
                {
                    kerbals = CreateKerbalList(facilityDatas[2]);
                }
            }
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            miningFacilityWindow = new KCMiningFacilityWindow(this);
            this.baseGroupName = "KC_CAB";

            maxOretList = new List<float> { 200f, 400f };
            OrePerDayperEngineer = new List<float> { 10f, 12f };
            maxKerbalsPerLevel = new List<int> { 8, 12 };

            this.maxKerbals = maxKerbalsPerLevel[level];
        }

        public KCMiningFacility(bool enabled, string facilityData = "") : base("KCMiningFacility", enabled, 8, "", 0, 1)
        {
            ore = 0;
        }
    }
}
