using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class KCMiningFacilityInfo : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, double> maxOre = new SortedDictionary<int, double>();
        public SortedDictionary<int, double> maxMetalOre = new SortedDictionary<int, double>();
        public SortedDictionary<int, double> orePerDayperEngineer = new SortedDictionary<int, double>();
        public SortedDictionary<int, double> metalOrePerDayperEngineer = new SortedDictionary<int, double>();

        public KCMiningFacilityInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                if (n.Value.HasValue("maxOre")) maxOre[n.Key] = double.Parse(n.Value.GetValue("maxOre"));
                else if (n.Key > 0) maxOre[n.Key] = maxOre[n.Key - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no maxOre (at least for level 0).");

                if (n.Value.HasValue("maxMetalOre")) maxMetalOre[n.Key] = double.Parse(n.Value.GetValue("maxMetalOre"));
                else if (n.Key > 0) maxMetalOre[n.Key] = maxMetalOre[n.Key - 1];
                else throw new MissingFieldException($"The facility {name}  (type:  {type}) has no maxMetalOre (at least for level 0).");

                if (n.Value.HasValue("oreRate")) orePerDayperEngineer[n.Key] = double.Parse(n.Value.GetValue("oreRate"));
                else if (n.Key > 0) orePerDayperEngineer[n.Key] = orePerDayperEngineer[n.Key - 1];
                else throw new MissingFieldException($"The facility {name}  (type:  {type}) has no oreRate (at least for level 0).");

                if (n.Value.HasValue("metalOreRate")) metalOrePerDayperEngineer[n.Key] = double.Parse(n.Value.GetValue("metalOreRate"));
                else if (n.Key > 0) metalOrePerDayperEngineer[n.Key] = metalOrePerDayperEngineer[n.Key - 1];
                else throw new MissingFieldException($"The facility {name}   (type:   {type}) has no metalOreRate (at least for level 0).");
            });
        }
    }

    public class KCMiningFacilityWindow : KCWindowBase
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

    public class KCMiningFacility : KCKerbalFacilityBase
    {
        protected KCMiningFacilityWindow miningFacilityWindow;

        public KCMiningFacilityInfo miningFacilityInfo { get { return (KCMiningFacilityInfo)facilityInfo; } }


        protected double ore;
        protected double metalOre;

        public double Ore { get { return ore; } }
        public double MetalOre { get { return metalOre; } }
        public double MaxOre { get { return miningFacilityInfo.maxOre[level]; } }
        public double MaxMetalOre { get { return miningFacilityInfo.maxMetalOre[level]; } }
        public double OrePerDayPerEngineer { get { return miningFacilityInfo.orePerDayperEngineer[level]; } }
        public double MetalOrePerDayPerEngineer { get { return miningFacilityInfo.metalOrePerDayperEngineer[level]; } }

        public override void Update()
        {
            //ResourceMap

            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            ore = Math.Min(MaxOre, ore + ((OrePerDayPerEngineer / 6 / 60 / 60) * deltaTime) * kerbals.Count);
            metalOre = Math.Min(MaxMetalOre, metalOre + ((MetalOrePerDayPerEngineer / 6 / 60 / 60) * deltaTime) * kerbals.Count);
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

        public KCMiningFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            ore = double.Parse(node.GetValue("ore"));
            metalOre = double.Parse(node.GetValue("metalOre"));
            miningFacilityWindow = new KCMiningFacilityWindow(this);
        }

        public KCMiningFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            miningFacilityWindow = new KCMiningFacilityWindow(this);

            ore = 0;
            metalOre = 0;
        }
    }
}
