using KerbalColonies.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KSP.UI.Screens.Settings.SettingsSetup;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.FuelCell
{
    public class KCFuelCellFacility : KCFacilityBase, KCECProducer
    {
        public KCFuelCellInfo fuelCellInfo => (KCFuelCellInfo)facilityInfo;
        protected KCFuelCellWindow window;

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public bool canProduceEC(double deltaTime)
        {
            if (!built || !enabled) return false;

            foreach (KeyValuePair<PartResourceDefinition, double> item in fuelCellInfo.ResourceConsumption[level])
            {
                if (KCStorageFacility.colonyResources(item.Key, Colony) < item.Value * deltaTime)
                {
                    Configuration.writeDebug($"KCFuelCellFacility ({DisplayName}): Not enough {item.Key.name} to produce EC");
                    return false;
                }
            }
            return true;
        }

        public double ProduceEC(double deltaTime)
        {
            if (!canProduceEC(deltaTime))
            {
                Configuration.writeLog($"KCFuelCellFacility ({DisplayName}): Cannot produce EC due to insufficient resources");
                return 0.0;
            }

            double ecProduced = fuelCellInfo.ECProduction[level] * deltaTime;
            Configuration.writeLog($"KCFuelCellFacility ({DisplayName}): Produced {ecProduced} EC");
            foreach (KeyValuePair<PartResourceDefinition, double> item in fuelCellInfo.ResourceConsumption[level])
            {
                KCStorageFacility.addResourceToColony(item.Key, -item.Value * deltaTime, Colony);
            }
            return ecProduced;
        }

        public double ECProduction(double lastTime, double deltaTime, double currentTime) => ProduceEC(deltaTime);

        public double ECPerSecond() => built && enabled ? fuelCellInfo.ECProduction[level] : 0;


        public override void OnBuildingClicked()
        {
            window.Toggle();
        }
        public override void OnRemoteClicked()
        {
            window.Toggle();
        }

        public override string GetFacilityProductionDisplay() => $"Fuel cell production rate: {ECPerSecond():f2} EC/s";

        public KCFuelCellFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            window = new KCFuelCellWindow(this);
        }

        public KCFuelCellFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCFuelCellWindow(this);
        }
    }
}
