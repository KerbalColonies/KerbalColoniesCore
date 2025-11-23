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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Windturbine
{
    public class KCWindturbineInfo : KCFacilityInfoClass
    {
        public SortedDictionary<int, double> Maxproduction { get; private set; } = new SortedDictionary<int, double>();
        public SortedDictionary<int, double> Minproduction { get; private set; } = new SortedDictionary<int, double>();


        public KCWindturbineInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;

                if (n.HasValue("Maxproduction"))
                    Maxproduction.Add(kvp.Key, double.Parse(n.GetValue("Maxproduction")));
                else if (kvp.Key > 0)
                    Maxproduction.Add(kvp.Key, Maxproduction[kvp.Key - 1]);
                else Maxproduction.Add(kvp.Key, double.MaxValue);

                if (n.HasValue("Minproduction"))
                    Minproduction.Add(kvp.Key, double.Parse(n.GetValue("Minproduction")));
                else if (kvp.Key > 0)
                    Minproduction.Add(kvp.Key, Minproduction[kvp.Key - 1]);
                else Minproduction.Add(kvp.Key, 0.0);
            });
        }
    }
}
