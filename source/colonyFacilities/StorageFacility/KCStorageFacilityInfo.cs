using System;
using System.Collections.Generic;
using System.Linq;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities.StorageFacility
{
    public class KCStorageFacilityInfo : KCFacilityInfoClass
    {
        public SortedDictionary<int, double> maxVolume { get; protected set; } = new SortedDictionary<int, double> { };

        public SortedDictionary<int, List<PartResourceDefinition>> resourceWhitelist { get; protected set; } = new SortedDictionary<int, List<PartResourceDefinition>> { };
        public SortedDictionary<int, List<PartResourceDefinition>> resourceBlacklist { get; protected set; } = new SortedDictionary<int, List<PartResourceDefinition>> { };

        public SortedDictionary<int, float> TransferRange { get; protected set; } = new SortedDictionary<int, float>();
        public SortedDictionary<int, List<Type>> RangeTypes { get; protected set; } = new SortedDictionary<int, List<Type>>(); // List of facility types that are used as nodes for the transfer range
        public SortedDictionary<int, List<string>> RangeFacilities { get; protected set; } = new SortedDictionary<int, List<string>>(); // List of (additional) facility names that are used as nodes for the transfer range, if type in the facility type list then this name is not used
        public SortedDictionary<int, bool> UseGravityMultiplier { get; protected set; } = new SortedDictionary<int, bool>();
        public SortedDictionary<int, float> MinGravity { get; protected set; } = new SortedDictionary<int, float>();
        public SortedDictionary<int, float> MaxGravity { get; protected set; } = new SortedDictionary<int, float>();

        public KCStorageFacilityInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                int level = kvp.Key;
                ConfigNode n = kvp.Value;

                if (n.HasValue("maxVolume")) maxVolume[level] = double.Parse(n.GetValue("maxVolume"));
                else if (level > 0) maxVolume[level] = maxVolume[level - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no maxVolume (at least for level 0).");

                if (n.HasValue("resourceWhitelist"))
                {
                    n.GetValue("resourceWhitelist").Split(',').Select(s => s.Trim()).ToList().ForEach(r =>
                    {
                        PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(r);
                        if (resource != null)
                        {
                            Configuration.writeDebug($"KCStorageFacilityInfo: Adding resource {r} to whitelist for facility {name} (type: {type}) at level {level}.");
                            if (!resourceWhitelist.ContainsKey(level)) resourceWhitelist.Add(level, new List<PartResourceDefinition> { resource });
                            else resourceWhitelist[level].Add(resource);
                        }
                        else throw new Exception($"KCStorageFacilityInfo: Resource {r} not found in PartResourceLibrary for facility {name} (type: {type}) at level {level}.");
                    });
                }
                else if (level > 0) resourceWhitelist[level] = resourceWhitelist[level - 1].ToList();
                else resourceWhitelist[level] = new List<PartResourceDefinition>();

                if (n.HasValue("resourceBlacklist"))
                {
                    n.GetValue("resourceBlacklist").Split(',').Select(s => s.Trim()).ToList().ForEach(r =>
                    {
                        PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(r);
                        if (resource != null)
                        {
                            Configuration.writeDebug($"KCStorageFacilityInfo: Adding resource {r} to blacklist for facility {name} (type: {type}) at level {level}.");
                            if (!resourceBlacklist.ContainsKey(level)) resourceBlacklist.Add(level, new List<PartResourceDefinition> { resource });
                            else resourceBlacklist[level].Add(resource);
                        }
                        else throw new Exception($"KCStorageFacilityInfo: Resource {r} not found in PartResourceLibrary for facility {name} (type: {type}) at level {level}.");
                    });
                }
                else if (level > 0) resourceBlacklist[level] = resourceBlacklist[level - 1].ToList();
                else resourceBlacklist[level] = new List<PartResourceDefinition>();

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
