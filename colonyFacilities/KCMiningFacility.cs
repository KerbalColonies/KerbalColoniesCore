using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities
{
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
            GUILayout.Label($"Stored ore: {miningFacility.Ore:f2}");
            GUILayout.Label($"Max ore: {miningFacility.MaxOre:f2}");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Stored metalore: {miningFacility.MetalOre:f2}");
            GUILayout.Label($"Max metalore: {miningFacility.MaxMetalOre:f2}");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            kerbalGUI.StaffingInterface();
            if (GUILayout.Button("Retrieve ore")) miningFacility.RetrieveOre();
            if (GUILayout.Button("Retrieve metalore")) miningFacility.RetrieveMetalOre();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }


        public KCMiningFacilityWindow(KCMiningFacility miningFacility) : base(Configuration.createWindowID(), "Miningfacility")
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

        public Dictionary<int, float> maxOreList { get; private set; } = new Dictionary<int, float> { };
        public Dictionary<int, float> maxMetalOretList { get; private set; } = new Dictionary<int, float> { };
        public Dictionary<int, float> OrePerDayperEngineer { get; private set; } = new Dictionary<int, float> { };
        public Dictionary<int, float> MetalOrePerDayperEngineer { get; private set; } = new Dictionary<int, float> { };

        public override void Update()
        {
            //ResourceMap

            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            ore = Math.Min(maxOreList[level], ore + (float)((OrePerDayperEngineer[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            metalOre = Math.Min(maxMetalOretList[level], metalOre + (float)((MetalOrePerDayperEngineer[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
        }

        public override void OnBuildingClicked()
        {
            miningFacilityWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            miningFacilityWindow.Toggle();
        }

        public bool RetrieveMetalOre()
        {
            if (metalOre > 0)
            {
                metalOre = KCStorageFacility.addResourceToColony(PartResourceLibrary.Instance.GetDefinition("MetalOre"), metalOre, Colony);
            }
            return true;
        }

        public bool RetrieveOre()
        {
            if (ore > 0)
            {
                ore = KCStorageFacility.addResourceToColony(PartResourceLibrary.Instance.GetDefinition("Ore"), ore, Colony);
            }

            return true;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("ore", ore);
            node.AddValue("metalOre", metalOre);

            ConfigNode wrapperNode = new ConfigNode("wrapper");
            wrapperNode.AddNode(base.getConfigNode());
            node.AddNode(wrapperNode);

            return node;
        }

        private void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());

                if (iLevel.HasValue("maxOre")) maxOreList[i] = int.Parse(iLevel.GetValue("maxOre"));
                else if (i > 0) maxOreList[i] = maxOreList[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxOre (at least for level 0).");

                if (iLevel.HasValue("maxMetalOre")) maxMetalOretList[i] = int.Parse(iLevel.GetValue("maxMetalOre"));
                else if (i > 0) maxMetalOretList[i] = maxMetalOretList[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxMetalOre (at least for level 0).");

                if (iLevel.HasValue("oreRate")) OrePerDayperEngineer[i] = float.Parse(iLevel.GetValue("oreRate"));
                else if (i > 0) OrePerDayperEngineer[i] = OrePerDayperEngineer[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no oreRate (at least for level 0).");

                if (iLevel.HasValue("metalOreRate")) MetalOrePerDayperEngineer[i] = float.Parse(iLevel.GetValue("metalOreRate"));
                else if (i > 0) MetalOrePerDayperEngineer[i] = MetalOrePerDayperEngineer[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no metalOreRate (at least for level 0).");
            }
        }

        public KCMiningFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);

            ore = double.Parse(node.GetValue("ore"));
            metalOre = double.Parse(node.GetValue("metalOre"));
            miningFacilityWindow = new KCMiningFacilityWindow(this);
        }

        public KCMiningFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            miningFacilityWindow = new KCMiningFacilityWindow(this);

            ore = 0;
            metalOre = 0;
        }
    }
}
