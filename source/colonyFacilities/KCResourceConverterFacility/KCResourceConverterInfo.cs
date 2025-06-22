using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class KCResourceConverterInfo : KCKerbalFacilityInfoClass
    {
        public Dictionary<int, ResourceConversionList> availableRecipes { get; private set; } = new Dictionary<int, ResourceConversionList> { };
        public Dictionary<int, int> ISRUcount { get; private set; } = new Dictionary<int, int> { };

        public KCResourceConverterInfo(ConfigNode node) : base(node)
        {
            foreach (KeyValuePair<int, ConfigNode> levelNode in levelNodes)
            {
                if (levelNode.Value.HasValue("conversionList"))
                {
                    string conversionListName = levelNode.Value.GetValue("conversionList");
                    ResourceConversionList conversionList = ResourceConversionList.GetConversionList(conversionListName);
                    if (conversionList != null)
                    {
                        availableRecipes.Add(levelNode.Key, conversionList);
                    }
                    else
                    {
                        throw new MissingFieldException($"The facility {name} (type: {type}) has no conversion list called {conversionListName}.");
                    }
                }
                else if (levelNode.Key > 0) availableRecipes.Add(levelNode.Key, availableRecipes[levelNode.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no conversion list (at least for level 0).");

                if (levelNode.Value.HasValue("ISRUcount")) ISRUcount[levelNode.Key] = int.Parse(levelNode.Value.GetValue("ISRUcount"));
                else if (levelNode.Key > 0) ISRUcount[levelNode.Key] = ISRUcount[levelNode.Key - 1];
                else throw new MissingFieldException($"The facility {name} has no ISRUcount (at least for level 0).");
            }
        }
    }
}
