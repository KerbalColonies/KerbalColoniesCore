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

namespace KerbalColonies.colonyFacilities.ProductionFacility
{
    public class KCProductionInfo : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, Dictionary<PartResourceDefinition, double>> vesselResourceCost { get; private set; } = [];

        public SortedDictionary<int, double> baseProduction { get; private set; } = [];
        public SortedDictionary<int, double> experienceMultiplier { get; private set; } = [];
        public SortedDictionary<int, double> facilityLevelMultiplier { get; private set; } = [];

        public bool CanBuildVessels(int level) => vesselResourceCost.ContainsKey(level);

        public bool HasSameRecipe(int level, KCProductionFacility otherFacility)
        {
            if (!CanBuildVessels(level)) return false;
            Dictionary<PartResourceDefinition, double> vesselCost = vesselResourceCost[level];
            KCProductionInfo otherInfo = (KCProductionInfo)otherFacility.facilityInfo;
            return otherInfo.CanBuildVessels(otherFacility.level) && vesselCost.All(vc => otherInfo.vesselResourceCost[otherFacility.level].ContainsKey(vc.Key) && otherInfo.vesselResourceCost[otherFacility.level][vc.Key] == vc.Value);
        }

        public KCProductionInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                ConfigNode iLevel = n.Value;
                if (iLevel.HasValue("baseProduction")) baseProduction.Add(n.Key, double.Parse(iLevel.GetValue("baseProduction")));
                else if (n.Key > 0) baseProduction.Add(n.Key, baseProduction[n.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no baseProduction (at least for level 0).");

                if (iLevel.HasValue("experienceMultiplier")) experienceMultiplier.Add(n.Key, double.Parse(iLevel.GetValue("experienceMultiplier")));
                else if (n.Key > 0) experienceMultiplier.Add(n.Key, experienceMultiplier[n.Key - 1]);
                else experienceMultiplier.Add(0, 0);

                if (iLevel.HasValue("facilityLevelMultiplier")) facilityLevelMultiplier.Add(n.Key, double.Parse(iLevel.GetValue("facilityLevelMultiplier")));
                else if (n.Key > 0) facilityLevelMultiplier.Add(n.Key, facilityLevelMultiplier[n.Key - 1]);
                else facilityLevelMultiplier.Add(0, 0);

                if (n.Value.HasNode("vesselResourceCost"))
                {
                    ConfigNode craftResourceNode = n.Value.GetNode("vesselResourceCost");
                    Dictionary<PartResourceDefinition, double> resourceList = [];
                    foreach (ConfigNode.Value v in craftResourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    vesselResourceCost.Add(n.Key, resourceList);
                }
                else if (n.Key > 0 && vesselResourceCost.ContainsKey(n.Key - 1)) vesselResourceCost.Add(n.Key, vesselResourceCost[n.Key - 1]);
            });
        }
    }
}
