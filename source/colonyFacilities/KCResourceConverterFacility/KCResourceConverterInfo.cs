using System;
using System.Collections.Generic;

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

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class KCResourceConverterInfo : KCKerbalFacilityInfoClass
    {
        public Dictionary<int, ResourceConversionList> availableRecipes { get; private set; } = new Dictionary<int, ResourceConversionList> { };
        public Dictionary<int, int> ISRUcount { get; private set; } = new Dictionary<int, int> { };

        public Dictionary<int, int> minKerbals { get; private set; } = new Dictionary<int, int> { };

        public KCResourceConverterInfo(ConfigNode node) : base(node)
        {
            foreach (KeyValuePair<int, ConfigNode> levelNode in levelNodes)
            {
                if (levelNode.Value.HasValue("conversionList"))
                {
                    string conversionListName = levelNode.Value.GetValue("conversionList");
                    ResourceConversionList conversionList = ResourceConversionList.GetConversionList(conversionListName);
                    if (conversionList != null)
                    {
                        availableRecipes.Add(levelNode.Key, conversionList);
                    }
                    else
                    {
                        throw new MissingFieldException($"The facility {name} (type: {type}) has no conversion list called {conversionListName}.");
                    }
                }
                else if (levelNode.Key > 0) availableRecipes.Add(levelNode.Key, availableRecipes[levelNode.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no conversion list (at least for level 0).");

                if (levelNode.Value.HasValue("ISRUcount")) ISRUcount[levelNode.Key] = int.Parse(levelNode.Value.GetValue("ISRUcount"));
                else if (levelNode.Key > 0) ISRUcount[levelNode.Key] = ISRUcount[levelNode.Key - 1];
                else throw new MissingFieldException($"The facility {name} has no ISRUcount (at least for level 0).");

                if (levelNode.Value.HasValue("minKerbals")) minKerbals[levelNode.Key] = int.Parse(levelNode.Value.GetValue("minKerbals"));
                else minKerbals[levelNode.Key] = maxKerbalsPerLevel[levelNode.Key];
            }
        }
    }
}
