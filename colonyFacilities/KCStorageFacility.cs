using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities
{
    [System.Serializable]
    internal class KCStorageFacility : KCFacilityBase
    {
        
        internal KCStorageFacility(bool enabled) : base("KCStorageFacility", enabled)
        {

        }
    }
}
