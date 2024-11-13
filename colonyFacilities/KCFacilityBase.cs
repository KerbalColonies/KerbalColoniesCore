using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities
{
    [System.Serializable]
    public abstract class KCFacilityBase
    {
        public string type;
        protected string facilityName;
        protected int facilityID;
        protected bool enabled;
        protected string facilityData;


        virtual internal void EncodeString()
        {
            List<string> facilities = new List<string>();


            facilityData = $"";
        }
        virtual internal dynamic DecodeString() { return null; }

        internal static List<string> GetFacilities() {  return null; }

        virtual internal void Update() { }

        protected KCFacilityBase(string facilityName, bool enabled)
        {
            this.facilityName = facilityName;
            this.enabled = enabled;
        }
    }
}
