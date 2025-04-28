using KerbalKonstructs.Modules;
using System.Collections.Generic;
using System.Linq;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (C) 2024 AMPW, Halengar

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
    public class KCCommNetFacility : KCKerbalFacilityBase
    {
        public string groundstationUUID = "";

        public override void OnGroupPlaced()
        {
            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(GetBaseGroupName(0), "Kerbin").Where(s => s.facilityType == KerbalKonstructs.Modules.KKFacilityType.GroundStation).First();
            groundstationUUID = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).First();

            KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID);

            targetInstance.facilityType = KKFacilityType.GroundStation;
            targetInstance.myFacilities.Add(targetInstance.gameObject.AddComponent<GroundStation>().ParseConfig(new ConfigNode()));

            GroundStation baseStation = baseInstance.myFacilities[0] as GroundStation;
            GroundStation targetStation = targetInstance.myFacilities[0] as GroundStation;

            targetStation.TrackingShort = baseStation.TrackingShort;
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
        }

        public KCCommNetFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {

        }
    }
}
