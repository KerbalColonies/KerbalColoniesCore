using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Windturbine
{
    public class KCWindturbineInfo : KCFacilityInfoClass
    {
        public SortedDictionary<int, double> Maxproduction { get; private set; } = new SortedDictionary<int, double>();
        public SortedDictionary<int, double> Minproduction { get; private set; } = new SortedDictionary<int, double>();


        public KCWindturbineInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (n.HasValue("Maxproduction"))
                    Maxproduction.Add(kvp.Key, double.Parse(n.GetValue("Maxproduction")));
                else if (kvp.Key > 0)
                    Maxproduction.Add(kvp.Key, Maxproduction[kvp.Key - 1]);
                else Maxproduction.Add(kvp.Key, double.MaxValue);

                if (n.HasValue("Minproduction"))
                    Minproduction.Add(kvp.Key, double.Parse(n.GetValue("Minproduction")));
                else if (kvp.Key > 0)
                    Minproduction.Add(kvp.Key, Minproduction[kvp.Key - 1]);
                else Minproduction.Add(kvp.Key, 0.0);
            });
        }
    }
}
