using KerbalKonstructs.Modules;
using System;
using System.Linq;

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
    public class KCCommNetFacility : KCFacilityBase
    {
        public string groundstationUUID = "";
        public ConfigNode sharedNode = null;

        public override void OnGroupPlaced()
        {
            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(GetBaseGroupName(0), "Kerbin").Where(s => s.facilityType == KerbalKonstructs.Modules.KKFacilityType.GroundStation).FirstOrDefault();
            if (baseInstance != null)
            {
                groundstationUUID = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).FirstOrDefault() ?? throw new Exception("No matching static found in the group");

                sharedNode.AddValue("uuid", groundstationUUID);
                KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID);

                targetInstance.facilityType = KKFacilityType.GroundStation;
                targetInstance.myFacilities.Add(targetInstance.gameObject.AddComponent<GroundStation>().ParseConfig(new ConfigNode()));

                GroundStation baseStation = baseInstance.myFacilities[0] as GroundStation;
                GroundStation targetStation = targetInstance.myFacilities[0] as GroundStation;

                targetStation.TrackingShort = baseStation.TrackingShort;
            }
            else
            {
                groundstationUUID = GetUUIDbyFacility(this).FirstOrDefault() ?? throw new Exception($"Launchpadfacility: No statics in group {GetBaseGroupName(level)}");

                Configuration.writeLog($"Launchpadfacility: using default configs for {GetBaseGroupName(level)}");
                sharedNode.AddValue("uuid", groundstationUUID);
                KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID);

                targetInstance.facilityType = KKFacilityType.GroundStation;
                targetInstance.myFacilities.Add(targetInstance.gameObject.AddComponent<GroundStation>().ParseConfig(new ConfigNode()));

                GroundStation targetStation = targetInstance.myFacilities[0] as GroundStation;

                targetStation.TrackingShort = 100000;
            }
        }

        public override ConfigNode GetSharedNode()
        {
            return sharedNode;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode baseNode = base.getConfigNode();
            baseNode.AddValue("groundstationUUID", groundstationUUID);

            return baseNode;
        }

        public KCCommNetFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            groundstationUUID = node.GetValue("groundstationUUID");
            AllowClick = false;
            AllowRemote = false;
        }

        public KCCommNetFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            sharedNode = new ConfigNode("commnetNode");
            AllowClick = false;
            AllowRemote = false;
        }
    }
}
