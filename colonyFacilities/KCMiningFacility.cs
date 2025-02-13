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

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stored metalore: " + miningFacility.MetalOre);
            GUILayout.Label("Max metalore: " + miningFacility.MaxMetalOre);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            kerbalGUI.StaffingInterface();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve resources"))
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
        public double MaxOre { get { return maxOreList[level]; } }
        public double MaxMetalOre { get { return maxMetalOretList[level]; } }

        private List<float> maxOreList = new List<float> { 10000f, 12000f };
        private List<float> maxMetalOretList = new List<float> { 4000f, 5000f };
        private List<float> OrePerDayperEngineer = new List<float> { 1000f, 1200f };
        private List<float> MetalOrePerDayperEngineer = new List<float> { 400f, 500f };
        private List<int> maxKerbalsPerLevel = new List<int> { 8, 12 };

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Engineer").ToList();
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            ore = Math.Min(maxOreList[level], ore + (float)((OrePerDayperEngineer[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            metalOre = Math.Min(maxMetalOretList[level], metalOre + (float)((MetalOrePerDayperEngineer[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            Configuration.saveColonies = true;
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

        public bool RetrieveResources()
        {
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
            if (ore > 0)
            {
                List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(PartResourceLibrary.Instance.GetDefinition("Ore"), saveGame, bodyIndex, colonyName);

                foreach (KCStorageFacility storage in storages)
                {
                    if (ore <= storage.getEmptyAmount(PartResourceLibrary.Instance.GetDefinition("Ore")))
                    {
                        storage.changeAmount(PartResourceLibrary.Instance.GetDefinition("Ore"), (float)ore);
                        ore = 0;
                        break;
                    }
                    else
                    {
                        double tempAmount = storage.getEmptyAmount(PartResourceLibrary.Instance.GetDefinition("Ore"));
                        storage.changeAmount(PartResourceLibrary.Instance.GetDefinition("Ore"), (float)tempAmount);
                        ore -= tempAmount;
                    }
                }
            }

            if (metalOre > 0)
            {
                List<KCStorageFacility> storages = KCStorageFacility.findFacilityWithResourceType(PartResourceLibrary.Instance.GetDefinition("MetalOre"), saveGame, bodyIndex, colonyName);
                if (storages.Count == 0)
                {
                    List<KCFacilityBase> facilities = KCFacilityBase.GetFacilitiesInColony(saveGame, bodyIndex, colonyName).Where(obj => typeof(KCStorageFacility).IsAssignableFrom(obj.GetType())).ToList();

                    foreach (KCFacilityBase facility in facilities)
                    {
                        KCStorageFacility storageFacility = (KCStorageFacility)facility;
                        storageFacility.addRessource(PartResourceLibrary.Instance.GetDefinition("MetalOre"));
                        storages.Add(storageFacility);
                    }
                }

                foreach (KCStorageFacility storage in storages)
                {
                    if (metalOre <= storage.getEmptyAmount(PartResourceLibrary.Instance.GetDefinition("MetalOre")))
                    {
                        storage.changeAmount(PartResourceLibrary.Instance.GetDefinition("MetalOre"), (float)metalOre);
                        metalOre = 0;
                        break;
                    }
                    else
                    {
                        double tempAmount = storage.getEmptyAmount(PartResourceLibrary.Instance.GetDefinition("MetalOre"));
                        storage.changeAmount(PartResourceLibrary.Instance.GetDefinition("MetalOre"), (float)tempAmount);
                        metalOre -= tempAmount;
                    }
                }
            }
            return true;
        }

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

        public override bool UpgradeFacility(int level)
        {
            base.UpgradeFacility(level);
            maxKerbals = maxKerbalsPerLevel[level];
            return true;
        }

        public override ConfigNode getCustomNode()
        {
            ConfigNode node = new ConfigNode("test");

            node.AddValue("testValue", 200);

            return node;
        }

        public override void loadCustomNode(ConfigNode customNode)
        {
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
                metalOre = float.Parse(facilityDatas[1].Split('&')[1]);
                maxKerbals = Convert.ToInt32(facilityDatas[2].Split('&')[1]);
                if (facilityDatas.Length > 3)
                {
                    kerbals = CreateKerbalList(facilityDatas[3]);
                }
            }
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            miningFacilityWindow = new KCMiningFacilityWindow(this);
            this.baseGroupName = "KC_CAB";

            maxOreList = new List<float> { 2000f, 4000f };
            maxMetalOretList = new List<float> { 400f, 800f };
            OrePerDayperEngineer = new List<float> { 200f, 400f };
            MetalOrePerDayperEngineer = new List<float> { 40f, 80f };
            maxKerbalsPerLevel = new List<int> { 8, 12 };

            this.maxKerbals = maxKerbalsPerLevel[level];

            this.upgradeType = UpgradeType.withoutGroupChange;
        }

        public KCMiningFacility(bool enabled, string facilityData = "") : base("KCMiningFacility", enabled, 8, facilityData, 0, 1)
        {
            ore = 0;
        }
    }
}
