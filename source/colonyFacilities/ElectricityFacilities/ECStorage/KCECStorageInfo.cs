using KerbalColonies.colonyFacilities.StorageFacility;
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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage
{
    public class KCECStorageInfo : KCStorageFacilityInfo
    {
        public SortedDictionary<int, double> ECCapacity { get; protected set; } = new SortedDictionary<int, double>();

        public KCECStorageInfo(ConfigNode node) : base(node)
        {
            PartResourceDefinition ec = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (!resourceWhitelist[kvp.Key].Contains(ec)) resourceWhitelist[kvp.Key].Add(ec);

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
