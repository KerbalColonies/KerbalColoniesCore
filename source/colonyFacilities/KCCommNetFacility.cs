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

        public override void OnGroupPlaced()
        {
            Configuration.writeLog($"KC CommNetFacility: OnGroupPlaced {GetBaseGroupName(0)}");
            KerbalKonstructs.Core.StaticInstance baseInstance = KerbalKonstructs.API.GetGroupStatics(GetBaseGroupName(0), "Kerbin").Where(s => s.facilityType == KerbalKonstructs.Modules.KKFacilityType.GroundStation).FirstOrDefault();
            if (baseInstance != null)
            {
                Configuration.writeLog($"KC CommNetFacility: Found base instance {baseInstance.UUID} in group {GetBaseGroupName(0)}");
                groundstationUUID = GetUUIDbyFacility(this).Where(s => KerbalKonstructs.API.GetModelTitel(s) == KerbalKonstructs.API.GetModelTitel(baseInstance.UUID)).FirstOrDefault() ?? throw new Exception("KC CommNetFacility: No matching static found in the group");

                Configuration.writeDebug($"KC CommNetFacility: using {groundstationUUID} for {GetBaseGroupName(0)}");
                KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID) ?? throw new Exception("KC CommNetFacility: Failed to find the staticinstance");

                targetInstance.facilityType = KKFacilityType.GroundStation;
                targetInstance.myFacilities.Clear();
                targetInstance.myFacilities.Add(targetInstance.gameObject.AddComponent<GroundStation>().ParseConfig(new ConfigNode()));

                GroundStation baseStation = baseInstance.myFacilities[0] as GroundStation ?? throw new Exception($"KC CommNetFacility: The found baseinstance has no groundstation at index 0");
                GroundStation targetStation = targetInstance.myFacilities[0] as GroundStation ?? throw new Exception($"KC CommNetFacility: The targetinstance has no groundstation at index 0"); ;

                Configuration.writeDebug($"KC CommNetFacility: TrackingShort = {baseStation.TrackingShort}");
                targetStation.TrackingShort = baseStation.TrackingShort;
            }
            else
            {
                groundstationUUID = GetUUIDbyFacility(this).FirstOrDefault() ?? throw new Exception($"Launchpadfacility: No statics in group {GetBaseGroupName(level)}");

                Configuration.writeLog($"Launchpadfacility: using default configs for {GetBaseGroupName(level)}");
                KerbalKonstructs.Core.StaticInstance targetInstance = KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID);

                targetInstance.facilityType = KKFacilityType.GroundStation;
                targetInstance.myFacilities.Add(targetInstance.gameObject.AddComponent<GroundStation>().ParseConfig(new ConfigNode()));

                GroundStation targetStation = targetInstance.myFacilities[0] as GroundStation;

                targetStation.TrackingShort = 100000;
            }
        }

        public override ConfigNode GetSharedNode()
        {
            ConfigNode sharedNode = new ConfigNode("commnetNode");
            sharedNode.AddValue("groundstationUUID", groundstationUUID);
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
            AllowClick = false;
            AllowRemote = false;
        }
    }
}
