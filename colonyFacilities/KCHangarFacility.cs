using Contracts.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities
{
    public class KCHangarFacility : KCFacilityBase
    {
        

        public KCHangarFacility(bool enabled, string facilityData = "") : base("KCHangarFacility", enabled, facilityData, 0, 1)
        {

        }
    }
}
