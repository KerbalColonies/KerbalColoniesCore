using System;
using System.Collections.Generic;
using System.Linq;
using static KerbalColonies.colonyFacilities.HangarFacility.KCHangarFacility;

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

/// This file contains parts from the Kerbal Konstructs mod Hangar.cs file which is licensed under the MIT License.
/// The general idea on how to store vessels is also taken from the Kerbal Konstructs mod

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.colonyFacilities.HangarFacility
{
    public class KCHangarInfo : KCFacilityInfoClass
    {

        public SortedDictionary<int, SortedDictionary<int, double>> Sizes { get; protected set; } = [];
        public SortedDictionary<int, KCHangarFacility.NewDimCalculationMode> NewDimCalculations { get; protected set; } = [];
        public SortedDictionary<int, int> VesselCapacity { get; protected set; } = [];
        public double Volume(int level) => Sizes[level].Values.Aggregate(1.0, (a, b) => a * b);
        public int Dimension => Sizes[0].Count;

        public KCHangarInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                Sizes.Add(n.Key, new SortedDictionary<int, double>());
                if (n.Value.HasNode("size"))
                {
                    ConfigNode sizeNode = n.Value.GetNode("size");
                    foreach (ConfigNode.Value v in sizeNode.values)
                    {
                        Sizes[n.Key].Add(int.Parse(v.name), double.Parse(v.value));
                    }
                }
                else if (n.Value.HasValue("x") && n.Value.HasValue("y") && n.Value.HasValue("z"))
                {
                    Sizes[n.Key].Add(0, double.Parse(n.Value.GetValue("x")));
                    Sizes[n.Key].Add(1, double.Parse(n.Value.GetValue("y")));
                    Sizes[n.Key].Add(2, double.Parse(n.Value.GetValue("z")));
                }
                else if (n.Key > 0) Sizes.Add(n.Key, Sizes[n.Key - 1]);
                else throw new Exception($"Missing \"size\" node for level {n.Key}");

                if (n.Value.HasValue("capacity")) VesselCapacity[n.Key] = int.Parse(n.Value.GetValue("capacity"));
                else VesselCapacity[n.Key] = n.Key > 0
                    ? VesselCapacity[n.Key - 1]
                    : int.MaxValue;
            });

            if (Dimension < 3) throw new Exception($"Unsupported dimension count: {Dimension}. At least 3 Dimensions are required.");
            else if (Dimension > 3)
            {
                if (node.HasNode("DimCalculation"))
                {
                    ConfigNode DimCalculations = node.GetNode("DimCalculation");
                    foreach (ConfigNode.Value v in DimCalculations.values)
                    {
                        NewDimCalculations.Add(int.Parse(v.name), (NewDimCalculationMode)Enum.Parse(typeof(NewDimCalculationMode), v.value));
                    }
                }
                else
                {
                    for (int i = 4; i < Dimension; i++) NewDimCalculations.Add(i, NewDimCalculationMode.Fixed);
                }
            }
        }
    }
}
