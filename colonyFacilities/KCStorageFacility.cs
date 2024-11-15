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
        internal PartResourceDefinition resource;
        internal float amount = 0f;
        internal float maxVolume;
        internal float currentVolume { get { return amount * resource.volume; } }

        internal override string EncodeString()
        {
            return $"ressource&{resource.id}|amount&{amount}|maxVolume&{maxVolume}";
        }

        internal override void DecodeString(string facilityData)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach (string s in facilityData.Split('|'))
            {
                data.Add(s.Split('&')[0], s.Split('&')[1]);
            }
            resource = PartResourceLibrary.Instance.GetDefinition(int.Parse(data["ressource"]));
            amount = float.Parse(data["amount"]);
            maxVolume = float.Parse(data["maxVolume"]);
        }

        internal override void Update()
        {
            base.Update();
        }

        internal override void OnBuildingClicked()
        {
            
        }

        internal KCStorageFacility(bool enabled, float maxVolume) : base("KCStorageFacility", enabled, "")
        {
            this.maxVolume = maxVolume;
        }
    }
}
