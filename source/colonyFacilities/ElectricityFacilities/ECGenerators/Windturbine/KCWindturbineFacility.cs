using KerbalColonies.Electricity;
using KerbalKonstructs.Core;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Windturbine
{
    public class KCWindturbineFacility : KCFacilityBase, KCECProducer
    {
        public Dictionary<string, double> densityList { get; protected set; } = new Dictionary<string, double>();


        public override void WhileBuildingPlaced(GroupCenter kkGroupname)
        {
            CelestialBody body = FlightGlobals.GetBodyByName(Colony.BodyName);

            double pressure = FlightGlobals.getStaticPressure(kkGroupname.RadiusOffset, body);
            double density = FlightGlobals.getAtmDensity(pressure, FlightGlobals.getExternalTemperature(kkGroupname.RadiusOffset, body));

            double ecPerSecond = facilityInfo.ECperSecond[level] * density;
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

                if (KKgroups.Count >= i -offset + 1 && densityList.ContainsKey(KKgroups[i - offset]))
                    ECPerSecond += facilityInfo.ECperSecond[i] * densityList[KKgroups[i - offset]];
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

                if (KKgroups.Count >= i - offset + 1 && densityList.ContainsKey(KKgroups[i - offset]))
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
