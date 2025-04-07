using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCMiningFacilityCost : KCFacilityCostClass
    {
        public KCMiningFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, double>> {
                { 0, new Dictionary<PartResourceDefinition, double> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500 } } },
                { 1, new Dictionary<PartResourceDefinition, double> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1000 } }
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
                kerbalGUI = new KerbalGUI(miningFacility, true);
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
            if (ore > 0)
            {
                ore = KCStorageFacility.addResourceToColony(PartResourceLibrary.Instance.GetDefinition("Ore"), ore, Colony);
            }

            if (metalOre > 0)
            {
                metalOre = KCStorageFacility.addResourceToColony(PartResourceLibrary.Instance.GetDefinition("MetalOre"), metalOre, Colony);
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

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = new ConfigNode();
            node.AddValue("ore", ore);
            node.AddValue("metalOre", metalOre);

            ConfigNode wrapperNode = new ConfigNode("wrapper");
            wrapperNode.AddNode(base.getConfigNode());
            node.AddNode(wrapperNode);

            return node;
        }

        public override string GetBaseGroupName(int level)
        {
            return "KC_CAB";
        }

        public KCMiningFacility(colonyClass colony, ConfigNode node) : base(colony, node)
        {
            ore = double.Parse(node.GetValue("ore"));
            metalOre = double.Parse(node.GetValue("metalOre"));
            miningFacilityWindow = new KCMiningFacilityWindow(this);

            maxOreList = new List<float> { 2000f, 4000f };
            maxMetalOretList = new List<float> { 400f, 800f };
            OrePerDayperEngineer = new List<float> { 200f, 400f };
            MetalOrePerDayperEngineer = new List<float> { 40f, 80f };
            maxKerbalsPerLevel = new List<int> { 8, 12 };
            this.maxKerbals = maxKerbalsPerLevel[level];
            this.upgradeType = UpgradeType.withoutGroupChange;
        }

        public KCMiningFacility(colonyClass colony, bool enabled) : base(colony, "KCMiningFacility", enabled, 8, 0, 1)
        {
            miningFacilityWindow = new KCMiningFacilityWindow(this);

            maxOreList = new List<float> { 2000f, 4000f };
            maxMetalOretList = new List<float> { 400f, 800f };
            OrePerDayperEngineer = new List<float> { 200f, 400f };
            MetalOrePerDayperEngineer = new List<float> { 40f, 80f };
            maxKerbalsPerLevel = new List<int> { 8, 12 };

            ore = 0;
            metalOre = 0;

            this.maxKerbals = maxKerbalsPerLevel[level];

            this.upgradeType = UpgradeType.withoutGroupChange;
        }
    }
}
