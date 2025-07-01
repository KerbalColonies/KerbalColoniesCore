using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Electricity
{
    public class KCColonyECData
    {
        public double lastTime { get; set; }
        public double deltaTime { get; set; }
        public double currentTime { get; set; }
        public double lastECProduced { get; set; }
        public double lastECConsumed { get; set; }
        public double lastECStored { get; set; }
        public double lastECDelta => lastECProduced - lastECConsumed;
    }

    public class KCECManager
    {
        public static Dictionary<colonyClass, KCColonyECData> colonyEC = new Dictionary<colonyClass, KCColonyECData>();

        private static void getDeltaTime(colonyClass colony, out double lastTime, out double deltaTime, out double currentTime)
        {
            currentTime = Planetarium.GetUniversalTime();
            ConfigNode timeNode = colony.sharedColonyNodes.FirstOrDefault(node => node.name == "KCELTime");
            if (timeNode == null)
            {
                ConfigNode node = new ConfigNode("KCELTime");
                node.AddValue("lastTime", Planetarium.GetUniversalTime().ToString());
                colony.sharedColonyNodes.Add(node);
                lastTime = currentTime;
                deltaTime = 0;
                return;
            }
            lastTime = double.Parse(timeNode.GetValue("lastTime"));
            deltaTime = currentTime - lastTime;
            timeNode.SetValue("lastTime", currentTime);
            return;
        }

        public static void ElectricityUpdate(colonyClass colony)
        {
            KCColonyECData colonyData;
            if (colonyEC.ContainsKey(colony)) colonyData = colonyEC[colony];
            else
            {
                colonyData = new KCColonyECData();
                colonyEC[colony] = colonyData;
            }

            getDeltaTime(colony, out double lastTime, out double deltaTime, out double currentTime);
            if (deltaTime == 0) return;

            colonyData.lastTime = lastTime;
            colonyData.deltaTime = deltaTime;
            colonyData.currentTime = currentTime;

            double ECProduced = colony.Facilities.OfType<KCECProducer>().Sum(facility => facility.ECProduction(lastTime, deltaTime, currentTime));
            colonyData.lastECProduced = ECProduced;

            SortedDictionary<int, List<KCECConsumer>> ECConsumers = new SortedDictionary<int, List<KCECConsumer>>();
            colony.Facilities.OfType<KCECConsumer>().ToList().ForEach(facility =>
            {
                if (!ECConsumers.ContainsKey(facility.ECConsumptionPriority))
                    ECConsumers[facility.ECConsumptionPriority] = new List<KCECConsumer>();
                ECConsumers[facility.ECConsumptionPriority].Add(facility);
            });
            colonyData.lastECConsumed = ECConsumers.SelectMany(kvp => kvp.Value).ToList().Sum(facility => facility.ExpectedECConsumption(lastTime, deltaTime, currentTime));

            SortedDictionary<int, List<KCECStorage>> ECStored = new SortedDictionary<int, List<KCECStorage>>();
            colony.Facilities.OfType<KCECStorage>().ToList().ForEach(facility =>
            {
                if (!ECStored.ContainsKey(facility.ECStoragePriority))
                    ECStored[facility.ECStoragePriority] = new List<KCECStorage>();
                ECStored[facility.ECStoragePriority].Add(facility);
            });
            colonyData.lastECStored = ECStored.SelectMany(kvp => kvp.Value).ToList().Sum(facility => facility.StoredEC(lastTime, deltaTime, currentTime));

            if (colonyData.lastECDelta >= 0)
            {
                ECConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(facility => facility.ConsumeEC(lastTime, deltaTime, currentTime));
                double remainingEC = colonyData.lastECDelta;
                ECStored.SelectMany(kvp => kvp.Value).ToList().TakeWhile(facility =>
                {
                    remainingEC = facility.ChangeECStored(remainingEC);
                    return remainingEC > 0;
                }).ToList();
            }
            else if (colonyData.lastECDelta + colonyData.lastECStored > 0)
            {
                ECConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(facility => facility.ConsumeEC(lastTime, deltaTime, currentTime));
                double missingEC = colonyData.lastECDelta;
                ECStored.SelectMany(kvp => kvp.Value).ToList().TakeWhile(facility =>
                {
                    missingEC = facility.ChangeECStored(missingEC);
                    return missingEC < 0;
                }).ToList();
            }
            else
            {
                ECStored.SelectMany(kvp => kvp.Value).ToList().ForEach(facility => facility.SetStoredEC(0));
                double remainingEC = colonyData.lastECStored + colonyData.lastECProduced;
                ECConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(facility =>
                {
                    double consumed = facility.ExpectedECConsumption(lastTime, deltaTime, currentTime);
                    if (consumed <= remainingEC)
                    {
                        remainingEC -= consumed;
                        facility.ConsumeEC(lastTime, deltaTime, currentTime);
                    }
                    else if (remainingEC > 0)
                    { 
                        facility.ÍnsufficientEC(lastTime, deltaTime, currentTime, remainingEC);
                        remainingEC = 0;
                    }
                    else
                    {
                        facility.ÍnsufficientEC(lastTime, deltaTime, currentTime, 0);
                    }
                });
            }
        }
    }
}
