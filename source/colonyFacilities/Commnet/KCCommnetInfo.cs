using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.Commnet
{
    public class KCCommnetInfo : KCFacilityInfoClass
    {
        public Dictionary<int, double> range { get; protected set; } = new Dictionary<int, double> { };

        public KCCommnetInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                if (kvp.Value.HasValue("range")) range.Add(kvp.Key, double.Parse(kvp.Value.GetValue("range")));
                else if (kvp.Key > 0) range.Add(kvp.Key, range[kvp.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no range (at least for level 0).");
            });
        }
    }

    public class KCGroundStationInfo : KCKerbalFacilityInfoClass
    {
        public Dictionary<int, double> range { get; protected set; } = new Dictionary<int, double> { };
        public Dictionary<int, double> kerbalRange { get; protected set; } = new Dictionary<int, double> { };
        public Dictionary<int, double> kerbalMultiplier { get; protected set; } = new Dictionary<int, double> { };

        public KCGroundStationInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                if (kvp.Value.HasValue("range")) range.Add(kvp.Key, double.Parse(kvp.Value.GetValue("range")));
                else if (kvp.Key > 0) range.Add(kvp.Key, range[kvp.Key - 1]);
                else range.Add(0, 0);

                if (kvp.Value.HasValue("kerbalRange")) kerbalRange.Add(kvp.Key, double.Parse(kvp.Value.GetValue("kerbalRange")));
                else if (kvp.Key > 0) kerbalRange.Add(kvp.Key, kerbalRange[kvp.Key - 1]);
                else kerbalRange.Add(0, 0);

                if (kvp.Value.HasValue("kerbalMultiplier")) kerbalMultiplier.Add(kvp.Key, double.Parse(kvp.Value.GetValue("kerbalMultiplier")));
                else if (kvp.Key > 0) kerbalMultiplier.Add(kvp.Key, kerbalMultiplier[kvp.Key - 1]);
                else kerbalMultiplier.Add(0, 0);
            });
        }
    }
}
