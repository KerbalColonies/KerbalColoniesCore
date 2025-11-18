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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.FuelCell
{
    public class KCFuelCellInfo : KCFacilityInfoClass
    {
        public KCFuelCellInfo(ConfigNode node) : base(node)
        {
            PartResourceDefinition ec = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (n.HasValue("ECProduction"))
                {
                    if (ResourceUsage[kvp.Key].ContainsKey(ec)) ResourceUsage[kvp.Key][ec] += double.Parse(n.GetValue("ECProduction"));
                    else ResourceUsage[kvp.Key].Add(ec, double.Parse(n.GetValue("ECProduction")));
                }
                else if (kvp.Key > 0) ResourceUsage[kvp.Key].Add(ec, ResourceUsage[kvp.Key - 1][ec]);

                if (n.HasNode("ResourceConsumption"))
                {
                    ConfigNode resourceNode = n.GetNode("ResourceConsumption");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in resourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = -double.Parse(v.value);
                        if (ResourceUsage[kvp.Key].ContainsKey(resourceDef)) ResourceUsage[kvp.Key][resourceDef] += amount;
                        else ResourceUsage[kvp.Key].Add(resourceDef, amount);
                    }
                }
            });
        }
    }
}
