using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacilityInfo : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, List<KCMiningFacilityRate>> rates { get; protected set; } = new SortedDictionary<int, List<KCMiningFacilityRate>> { };

        public KCMiningFacilityInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                if (n.Value.HasNode("resourceProduction"))
                {
                    rates[n.Key] = new List<KCMiningFacilityRate> { };
                    foreach (ConfigNode.Value value in n.Value.GetNode("resourceProduction").values)
                    {
                        PartResourceDefinition res = PartResourceLibrary.Instance.GetDefinition(value.name);
                        if (res == null) throw new NullReferenceException($"The resource {value.name} is not defined in the PartResourceLibrary. Please check your configuration for the facility {name} (type: {type}).");
                        string[] strings = value.value.Split(',');
                        if (strings.Length == 2)
                        {
                            if (!double.TryParse(strings[0], out double rate) || !double.TryParse(strings[1], out double max)) throw new FormatException($"The resourceProduction value for {value.name} in the facility {name} (type: {type}) is not in the correct format. It should be 'rate,max'.");

                            rates[n.Key].Add(new KCMiningFacilityRate(res, rate, max, false));
                        }
                        else if (strings.Length == 3)
                        {
                            if (!double.TryParse(strings[0], out double rate) || !double.TryParse(strings[1], out double max) || !bool.TryParse(strings[2], out bool useFixedRate)) throw new FormatException($"The resourceProduction value for {value.name} in the facility {name} (type: {type}) is not in the correct format. It should be 'rate,max,useFixedRate'.");
                            rates[n.Key].Add(new KCMiningFacilityRate(res, rate, max, useFixedRate));
                        }
                        else throw new FormatException($"The resourceProduction value for {value.name} in the facility {name} (type: {type}) is not in the correct format. It should be 'rate,max,useFixedRate'.");
                    }
                }
                else if (n.Key > 0) rates[n.Key] = rates[n.Key - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no resourceProduction (at least for level 0).");
            });
        }
    }
}
