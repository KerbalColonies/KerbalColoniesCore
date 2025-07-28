using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage
{
    public class KCECStorageInfo : KCFacilityInfoClass
    {
        public SortedDictionary<int, double> ECCapacity { get; protected set; } = new SortedDictionary<int, double>();
        public SortedDictionary<int, float> TransferRange { get; protected set; } = new SortedDictionary<int, float>();
        public SortedDictionary<int, List<Type>> RangeTypes { get; protected set; } = new SortedDictionary<int, List<Type>>(); // List of facility types that are used as nodes for the transfer range
        public SortedDictionary<int, List<string>> RangeFacilities { get; protected set; } = new SortedDictionary<int, List<string>>(); // List of (additional) facility names that are used as nodes for the transfer range, if type in the facility type list then this name is not used
        public SortedDictionary<int, bool> UseGravityMultiplier { get; protected set; } = new SortedDictionary<int, bool>();
        public SortedDictionary<int, float> MinGravity { get; protected set; } = new SortedDictionary<int, float>();
        public SortedDictionary<int, float> MaxGravity { get; protected set; } = new SortedDictionary<int, float>();

        public KCECStorageInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (n.HasValue("capacity")) ECCapacity.Add(kvp.Key, double.Parse(n.GetValue("capacity")));
                else if (kvp.Key > 0) ECCapacity.Add(kvp.Key, ECCapacity[kvp.Key - 1]);
                else throw new Exception($"KCECStorageInfo ({name}): Level {kvp.Key} does not have any capacity (at least for level 0)");

                if (n.HasValue("range")) TransferRange.Add(kvp.Key, float.Parse(n.GetValue("range")));
                else if (kvp.Key > 0) TransferRange.Add(kvp.Key, TransferRange[kvp.Key - 1]);
                else TransferRange.Add(0, 150);


                if (n.HasValue("RangeTypes"))
                {
                    string[] strings = n.GetValue("RangeTypes").Split(',');

                    List<Type> types = new List<Type>();

                    strings.ToList().ForEach(s =>
                    {
                        Type t = KCFacilityTypeRegistry.GetType(s.Trim());
                        if (t != null)
                        {
                            Configuration.writeDebug($"KCECStorageInfo ({name}): Found type {t.FullName} in RangeTypes for level {kvp.Key}");
                            types.Add(t);
                        }
                        else
                        {
                            Configuration.writeDebug($"KCECStorageInfo ({name}): Could not find type {s.Trim()} in RangeTypes for level {kvp.Key}");
                            throw new Exception($"KCECStorageInfo ({name}): Could not find type {s.Trim()} in RangeTypes for level {kvp.Key}");
                        }
                    });

                    RangeTypes.Add(kvp.Key, types);
                }
                else if (kvp.Key > 0)
                {
                    RangeTypes.Add(kvp.Key, RangeTypes[kvp.Key - 1]);
                }
                else
                {
                    RangeTypes.Add(0, new List<Type>());
                }

                if (n.HasValue("RangeFacilities"))
                {
                    string[] strings = n.GetValue("RangeFacilities").Split(',');
                    List<string> facilities = new List<string>();
                    strings.ToList().ForEach(s =>
                    {
                        string facilityName = s.Trim();
                        if (!string.IsNullOrEmpty(facilityName))
                        {
                            Configuration.writeDebug($"KCECStorageInfo ({name}): Found facility {facilityName} in RangeFacilities for level {kvp.Key}");
                            facilities.Add(facilityName);
                        }
                        else
                        {
                            Configuration.writeDebug($"KCECStorageInfo ({name}): Empty facility name in RangeFacilities for level {kvp.Key}");
                        }
                    });
                    RangeFacilities.Add(kvp.Key, facilities);
                }
                else if (kvp.Key > 0)
                    RangeFacilities.Add(kvp.Key, RangeFacilities[kvp.Key - 1]);
                else
                    RangeFacilities.Add(0, new List<string>());


                if (n.HasValue("UseGravityMultiplier"))
                {
                    UseGravityMultiplier.Add(kvp.Key, bool.Parse(n.GetValue("UseGravityMultiplier")));
                    if (UseGravityMultiplier[kvp.Key])
                    {
                        MinGravity.Add(kvp.Key, float.Parse(n.GetValue("MinGravity")));
                        MaxGravity.Add(kvp.Key, float.Parse(n.GetValue("MaxGravity")));
                    }
                    else
                    {
                        MinGravity.Add(kvp.Key, 1);
                        MaxGravity.Add(kvp.Key, 1);
                    }
                }
                else if (kvp.Key > 0)
                {
                    UseGravityMultiplier.Add(kvp.Key, UseGravityMultiplier[kvp.Key - 1]);
                    MinGravity.Add(kvp.Key, MinGravity[kvp.Key - 1]);
                    MaxGravity.Add(kvp.Key, MaxGravity[kvp.Key - 1]);
                }
                else
                {
                    UseGravityMultiplier.Add(0, false);
                    MinGravity.Add(0, 1);
                    MaxGravity.Add(0, 1);
                }
            });
        }
    }
}
