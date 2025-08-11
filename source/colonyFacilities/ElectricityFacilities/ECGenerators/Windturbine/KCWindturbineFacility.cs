using KerbalColonies.Electricity;
using KerbalKonstructs.Core;
using System.Collections.Generic;
using System.Linq;
using System;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Windturbine
{
    public class KCWindturbineFacility : KCFacilityBase, KCECProducer
    {
        public KCWindturbineInfo info => (KCWindturbineInfo)facilityInfo;

        public Dictionary<string, double> densityList { get; protected set; } = new Dictionary<string, double>();


        public override void WhileBuildingPlaced(GroupCenter kkGroupname)
        {
            KCWindturbineInfo facilityInfo = (KCWindturbineInfo)this.facilityInfo;

            CelestialBody body = FlightGlobals.GetBodyByName(Colony.BodyName);

            double pressure = FlightGlobals.getStaticPressure(kkGroupname.RadiusOffset, body);
            double density = FlightGlobals.getAtmDensity(pressure, FlightGlobals.getExternalTemperature(kkGroupname.RadiusOffset, body));

            double ecPerSecond = density > 0 ? Math.Max(facilityInfo.Minproduction[level], Math.Min(facilityInfo.Maxproduction[level], facilityInfo.ECperSecond[level] * density)) : 0;
            Configuration.writeDebug($"KCWindTurbineFacility ({DisplayName}): EC per second: {ecPerSecond}, pressure: {pressure}, density: {density}");

            KCWindturbinePlacementWindow.Instance.ECProductionRate = ecPerSecond;
            KCWindturbinePlacementWindow.Instance.Pressure = pressure;
            KCWindturbinePlacementWindow.Instance.Density = density;

            if (!KCWindturbinePlacementWindow.Instance.IsOpen()) KCWindturbinePlacementWindow.Instance.Open();
        }

        public override void OnGroupPlaced(GroupCenter kkgroup)
        {
            KCWindturbinePlacementWindow.Instance.Close();

            CelestialBody body = FlightGlobals.GetBodyByName(Colony.BodyName);

            double pressure = FlightGlobals.getStaticPressure(kkgroup.RadiusOffset, body);
            double density = FlightGlobals.getAtmDensity(pressure, FlightGlobals.getExternalTemperature(kkgroup.RadiusOffset, body));

            if (densityList.ContainsKey(kkgroup.Group)) densityList[kkgroup.Group] = density;
            else densityList.Add(kkgroup.Group, density);

            Colony.UpdateColony();
        }

        public double ECProduction(double lastTime, double deltaTime, double currentTime)
        {
            if (!built) return 0.0;

            KCWindturbineInfo facilityInfo = (KCWindturbineInfo)this.facilityInfo;
            CelestialBody body = FlightGlobals.GetBodyByName(Colony.BodyName);

            double ECPerSecond = 0.0;

            int offset = 0;
            for (int i = 0; i <= level; i++)
            {
                if (facilityInfo.UpgradeTypes[i] != UpgradeType.withAdditionalGroup && i < level)
                {
                    offset++;
                    continue;
                }

                if (KKgroups.Count >= i - offset + 1 && densityList.ContainsKey(KKgroups[i - offset]) && densityList[KKgroups[i - offset]] > 0)
                    ECPerSecond += Math.Max(facilityInfo.Minproduction[i], Math.Min(facilityInfo.Maxproduction[i], facilityInfo.ECperSecond[i] * densityList[KKgroups[i - offset]]));
            }

            Configuration.writeDebug($"KCWindTurbineFacility ({DisplayName}): EC per second: {ECPerSecond}");
            return ECPerSecond * deltaTime;
        }

        public double ECPerSecond()
        {
            if (!built) return 0.0;

            CelestialBody body = FlightGlobals.GetBodyByName(Colony.BodyName);

            double ECPerSecond = 0.0;

            int offset = 0;
            for (int i = 0; i <= level; i++)
            {
                if (facilityInfo.UpgradeTypes[i] != UpgradeType.withAdditionalGroup && i < level)
                {
                    offset++;
                    continue;
                }

                if (KKgroups.Count >= i - offset + 1 && densityList.ContainsKey(KKgroups[i - offset]) && densityList[KKgroups[i - offset]] > 0)
                    ECPerSecond += facilityInfo.ECperSecond[i] * densityList[KKgroups[i - offset]];
            }

            Configuration.writeDebug($"KCWindTurbineFacility ({DisplayName}): EC per second: {ECPerSecond}");
            return ECPerSecond;
        }

        public override string GetFacilityProductionDisplay() => $"Wind turbine production rate: {ECPerSecond():F2} EC/s";

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            ConfigNode windturbineNode = new ConfigNode("KCWindturbineFacility");
            densityList.ToList().ForEach(kvp => windturbineNode.AddValue(kvp.Key, kvp.Value));

            node.AddNode(windturbineNode);
            return node;
        }

        public KCWindturbineFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            ConfigNode windturbineNode = node.GetNode("KCWindturbineFacility");
            densityList = new Dictionary<string, double>();
            foreach (ConfigNode.Value value in windturbineNode.values)
            {
                densityList.Add(value.name, double.Parse(value.value));
            }
        }

        public KCWindturbineFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
        }
    }
}
