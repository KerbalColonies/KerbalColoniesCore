using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.FuelCell
{
    public class KCFuelCellInfo : KCFacilityInfoClass
    {
        public Dictionary<int, double> ECProduction { get; protected set; } = new Dictionary<int, double> { };
        public Dictionary<int, Dictionary<PartResourceDefinition, double>> ResourceConsumption { get; protected set; } = new Dictionary<int, Dictionary<PartResourceDefinition, double>> { };


        public KCFuelCellInfo(ConfigNode node) : base(node)
        {
            ECProduction = new Dictionary<int, double> { };
            ResourceConsumption = new Dictionary<int, Dictionary<PartResourceDefinition, double>> { };

            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (n.HasValue("ECProduction")) ECProduction.Add(kvp.Key, double.Parse(n.GetValue("ECProduction")));
                else if (kvp.Key > 0) ECProduction.Add(kvp.Key, ECProduction[kvp.Key - 1]);
                else throw new Exception($"KCFuelCellInfo ({name}): Level {kvp.Key} does not have any ECProduction (at least for level 0)");

                if (n.HasNode("ResourceConsumption"))
                {
                    ConfigNode resourceNode = n.GetNode("ResourceConsumption");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in resourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    ResourceConsumption.Add(kvp.Key, resourceList);
                }

            });
        }
    }
}
