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
        public Dictionary<int, double> ECProduction { get; protected set; } = new Dictionary<int, double> { };
        public Dictionary<int, Dictionary<PartResourceDefinition, double>> ResourceConsumption { get; protected set; } = new Dictionary<int, Dictionary<PartResourceDefinition, double>> { };


        public KCFuelCellInfo(ConfigNode node) : base(node)
        {
            ECProduction = new Dictionary<int, double> { };
            ResourceConsumption = new Dictionary<int, Dictionary<PartResourceDefinition, double>> { };

            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (n.HasValue("ECProduction")) ECProduction.Add(kvp.Key, double.Parse(n.GetValue("ECProduction")));
                else if (kvp.Key > 0) ECProduction.Add(kvp.Key, ECProduction[kvp.Key - 1]);
                else throw new Exception($"KCFuelCellInfo ({name}): Level {kvp.Key} does not have any ECProduction (at least for level 0)");

                if (n.HasNode("ResourceConsumption"))
                {
                    ConfigNode resourceNode = n.GetNode("ResourceConsumption");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in resourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    ResourceConsumption.Add(kvp.Key, resourceList);
                }

            });
        }
    }
}
