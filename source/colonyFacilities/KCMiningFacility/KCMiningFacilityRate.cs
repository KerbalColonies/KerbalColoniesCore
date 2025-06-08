using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacilityRate
    {
        public PartResourceDefinition resource { get; private set; }
        public double rate { get; private set; }
        public double max { get; private set; }
        public bool useFixedRate { get; private set; } = false;

        public KCMiningFacilityRate(PartResourceDefinition resource, double rate, double max, bool useFixedRate)
        {
            this.resource = resource;
            this.rate = rate;
            this.max = max;
            this.useFixedRate = useFixedRate;
        }
    }
}
