using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies
{
    public class Colony
    {
        public string Name;

        public KC_CAB_Facility CAB;

        public List<KCFacilityBase> Facilities;
        public List<ConfigNode> colonyNodes;
    }
}
