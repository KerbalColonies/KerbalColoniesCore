using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fusion
{
    public class KCFusionInfo : KCKerbalFacilityInfoClass
    {
        public Dictionary<int, double> ECProduction { get; protected set; } = new Dictionary<int, double>();
        public Dictionary<int, double> MinECThrottle { get; protected set; } = new Dictionary<int, double>();
        public Dictionary<int, double> MaxECChangeRate { get; protected set; } = new Dictionary<int, double>();
        public Dictionary<int, double> MinECRateChangeTime { get; protected set; } = new Dictionary<int, double>();
        public Dictionary<int, double> ECChangeThreshold { get; protected set; } = new Dictionary<int, double>();
        public Dictionary<int, double> PowerlevelChangeTime { get; protected set; } = new Dictionary<int, double>();
        public Dictionary<int, double> LevelOffTime { get; protected set; } = new Dictionary<int, double>();

        public Dictionary<int, Dictionary<PartResourceDefinition, double>> InputResources { get; protected set; } = new Dictionary<int, Dictionary<PartResourceDefinition, double>>();
        public Dictionary<int, Dictionary<PartResourceDefinition, double>> OutputResources { get; protected set; } = new Dictionary<int, Dictionary<PartResourceDefinition, double>>();
        public Dictionary<int, int> MinKerbals { get; protected set; } = new Dictionary<int, int>();
        public Dictionary<int, int> MinKerbalLevel { get; protected set; } = new Dictionary<int, int>();
        public Dictionary<int, Dictionary<string, int>> RequiredTraits { get; protected set; } = new Dictionary<int, Dictionary<string, int>>();

        public KCFusionInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;
                if (n.HasValue("ECProduction")) ECProduction.Add(kvp.Key, double.Parse(n.GetValue("ECProduction")));
                else if (kvp.Key > 0) ECProduction.Add(kvp.Key, ECProduction[kvp.Key - 1]);
                else throw new Exception($"KCFissionInfo ({name}): Level {kvp.Key} does not have any ECProduction (at least for level 0)");

                if (n.HasNode("InputResources"))
                {
                    ConfigNode inputNode = n.GetNode("InputResources");
                    Dictionary<PartResourceDefinition, double> inputList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in inputNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        inputList.Add(resourceDef, amount);
                    }
                    InputResources.Add(kvp.Key, inputList);
                }
                else
                    InputResources.Add(kvp.Key, new Dictionary<PartResourceDefinition, double>());

                if (n.HasNode("OutputResources"))
                {
                    ConfigNode outputNode = n.GetNode("OutputResources");
                    Dictionary<PartResourceDefinition, double> outputList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in outputNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        outputList.Add(resourceDef, amount);
                    }
                    OutputResources.Add(kvp.Key, outputList);
                }
                else
                    OutputResources.Add(kvp.Key, new Dictionary<PartResourceDefinition, double>());


                if (n.HasValue("minKerbals")) MinKerbals.Add(kvp.Key, int.Parse(n.GetValue("minKerbals")));
                else MinKerbals.Add(kvp.Key, maxKerbalsPerLevel[kvp.Key]);

                if (n.HasValue("minKerbalLevel")) MinKerbalLevel.Add(kvp.Key, int.Parse(n.GetValue("minKerbalLevel")));
                else MinKerbalLevel.Add(kvp.Key, 0);

                if (n.HasValue("minECThrottle")) MinECThrottle.Add(kvp.Key, double.Parse(n.GetValue("minECThrottle")));
                else MinECThrottle.Add(kvp.Key, 0.0);

                if (n.HasValue("maxECChangeRate")) MaxECChangeRate.Add(kvp.Key, double.Parse(n.GetValue("maxECChangeRate")));
                else MaxECChangeRate.Add(kvp.Key, 1000.0);

                if (n.HasValue("minECRateChangeTime")) MinECRateChangeTime.Add(kvp.Key, double.Parse(n.GetValue("minECRateChangeTime")));
                else MinECRateChangeTime.Add(kvp.Key, 1.0);

                if (n.HasValue("ecChangeThreshold")) ECChangeThreshold.Add(kvp.Key, double.Parse(n.GetValue("ecChangeThreshold")));
                else ECChangeThreshold.Add(kvp.Key, 0.5);

                if (n.HasValue("powerlevelChangeTime")) PowerlevelChangeTime.Add(kvp.Key, double.Parse(n.GetValue("powerlevelChangeTime")));
                else PowerlevelChangeTime.Add(kvp.Key, 0);

                if (n.HasValue("levelOffTime")) LevelOffTime.Add(kvp.Key, double.Parse(n.GetValue("levelOffTime")));
                else LevelOffTime.Add(kvp.Key, 0);


                if (n.HasNode("RequiredTraits"))
                {
                    ConfigNode traitsNode = n.GetNode("RequiredTraits");
                    Dictionary<string, int> traitsList = new Dictionary<string, int>();
                    foreach (ConfigNode.Value v in traitsNode.values)
                    {
                        string traitName = v.name.ToLower();
                        int level = int.Parse(v.value);
                        traitsList.Add(traitName, level);
                    }
                    RequiredTraits.Add(kvp.Key, traitsList);
                }
                else if (kvp.Key > 0)
                    RequiredTraits.Add(kvp.Key, RequiredTraits[kvp.Key - 1]);
                else
                    RequiredTraits.Add(kvp.Key, new Dictionary<string, int>());
            });
        }
    }
}
