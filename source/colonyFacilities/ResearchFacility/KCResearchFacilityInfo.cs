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

namespace KerbalColonies.colonyFacilities.ResearchFacility
{
    public class KCResearchFacilityInfo : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, double> maxSciencePoints = [];
        public SortedDictionary<int, double> sciencePointsPerDayperResearcher = [];

        public KCResearchFacilityInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                sciencePointsPerDayperResearcher[n.Key] = n.Value.HasValue("scienceRate")
                    ? double.Parse(n.Value.GetValue("scienceRate"))
                    : n.Key > 0
                    ? sciencePointsPerDayperResearcher[n.Key - 1]
                    : throw new MissingFieldException($"The facility {name} (type: {type}) has no scienceRate (at least for level 0).");

                maxSciencePoints[n.Key] = n.Value.HasValue("maxScience")
                    ? double.Parse(n.Value.GetValue("maxScience"))
                    : n.Key > 0
                    ? maxSciencePoints[n.Key - 1]
                    : throw new MissingFieldException($"The facility {name} (type: {type}) has no maxScience (at least for level 0).");
            });
        }
    }
}
