using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies
{
    /// <summary>
    /// A class that's used to check and remove ressources from an vessel or other colonies.
    /// <para>
    /// It's also used to check to cost for upgrades of a facility.
    /// </para>
    /// <para>Level 0 is passed when building this facility.</para>
    /// <para>The maximum level is the inclusive upper limit, e.g. a facility with maxLevel = 1 can be built and then upgraded once to level 1.</para>
    /// <para>Repurpose: Now it's also used for creating the facilties from config files so values like cost and custom production parameters aren't hardcoded</para>
    /// </summary>
    public class KCFacilityInfoClass
    {
        public static bool operator ==(KCFacilityInfoClass leftInfo, KCFacilityInfoClass rightInfo)
        {
            if (ReferenceEquals(leftInfo, null) && ReferenceEquals(rightInfo, null)) return true;
            else if (ReferenceEquals(leftInfo, null) || ReferenceEquals(rightInfo, null)) return false;
            else return leftInfo.name == rightInfo.name;
        }

        public static bool operator !=(KCFacilityInfoClass leftInfo, KCFacilityInfoClass rightInfo)
        {
            if (ReferenceEquals(leftInfo, null) && ReferenceEquals(rightInfo, null)) return false;
            else if (ReferenceEquals(leftInfo, null) || ReferenceEquals(rightInfo, null)) return true;
            else return leftInfo.name != rightInfo.name;
        }

        public override bool Equals(object obj)
        {
            return obj is KCFacilityInfoClass && ((KCFacilityInfoClass)obj).name == name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public Type type { get; protected set; }
        public ConfigNode facilityConfig { get; protected set; }
        public string name { get; protected set; }
        public string displayName { get; protected set; }

        public SortedDictionary<int, ConfigNode> levelNodes { get; protected set; } = new SortedDictionary<int, ConfigNode> { };

        /// <summary>
        /// Level, Resource, Amount
        /// </summary>
        public SortedDictionary<int, Dictionary<PartResourceDefinition, double>> resourceCost { get; protected set; }
        /// <summary>
        /// currently unused
        /// </summary>
        public SortedDictionary<int, double> Electricity { get; protected set; } = new SortedDictionary<int, double> { };
        public SortedDictionary<int, double> Funds { get; protected set; } = new SortedDictionary<int, double> { };
        public SortedDictionary<int, UpgradeType> UpgradeTypes { get; protected set; } = new SortedDictionary<int, UpgradeType> { };
        public SortedDictionary<int, string> BasegroupNames { get; protected set; } = new SortedDictionary<int, string> { };

        // 1 Kerbin day = 0.25 days
        // 100 per day * 5 engineers = 500 per day
        // 500 per day * 4 kerbin days = 500
        // 500 per day * 2 kerbin days = 250
        public SortedDictionary<int, float> UpgradeTimes { get; protected set; } = new SortedDictionary<int, float> { };

        /// <summary>
        /// Used for custom checks (e.g. if a specific facility already exists in the colony), returns true if the facility can be built or upgraded.
        /// </summary>
        public virtual bool customCheck(int level, colonyClass colony)
        {
            return true;
        }

        /// <summary>
        /// Called after all configs are loaded but the order of the lateInit is not guaranteed.
        /// </summary>
        public virtual void lateInit()
        {

        }

        public bool checkResources(int level, colonyClass colony)
        {
            if (!customCheck(level, colony))
            {
                return false;
            }

            foreach (KeyValuePair<PartResourceDefinition, double> resource in resourceCost[level])
            {
                double vesselAmount = 0;
                double colonyAmount = KCStorageFacility.colonyResources(resource.Key, colony);

                if (colony.CAB.PlayerInColony)
                {
                    FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);

                    vesselAmount = amount;
                }

                if (vesselAmount >= resource.Value * Configuration.FacilityCostMultiplier) continue;
                else if (colonyAmount >= resource.Value * Configuration.FacilityCostMultiplier) continue;
                else if (vesselAmount + colonyAmount >= resource.Value * Configuration.FacilityCostMultiplier) continue;
                else return false;
            }

            if (Funding.Instance != null)
            {
                if (Funding.Instance.Funds < Funds[level] * Configuration.FacilityCostMultiplier)
                {
                    return false;
                }
            }

            return true;
        }

        public bool removeResources(int level, colonyClass colony)
        {
            if (checkResources(level, colony))
            {
                foreach (KeyValuePair<PartResourceDefinition, double> resource in resourceCost[level])
                {
                    double remainingAmount = resource.Value * Configuration.FacilityCostMultiplier;

                    double vesselAmount = 0;
                    double colonyAmount = KCStorageFacility.colonyResources(resource.Key, colony);
                    if (colony.CAB.PlayerInColony)
                    {
                        FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);
                        vesselAmount = amount;
                    }

                    if (vesselAmount >= resource.Value * Configuration.FacilityCostMultiplier)
                    {
                        FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, resource.Value * Configuration.FacilityCostMultiplier, true);
                    }
                    else
                    {
                        if (colony.CAB.PlayerInColony)
                        {
                            FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, vesselAmount, true);
                        }
                        remainingAmount -= vesselAmount;

                        KCStorageFacility.addResourceToColony(resource.Key, -remainingAmount, colony);
                    }
                }
                if (Funding.Instance != null)
                {
                    Funding.Instance.AddFunds(-Funds[level] * Configuration.FacilityCostMultiplier, TransactionReasons.None);
                }
                return true;
            }
            return false;
        }

        public KCFacilityInfoClass(ConfigNode node)
        {
            facilityConfig = node;

            if (!node.HasValue("name")) throw new MissingFieldException("A config without a name has been found.");
            name = node.GetValue("name");
            if (!node.HasValue("displayName")) throw new MissingFieldException($"The facility {name} has no displayName.");
            displayName = node.GetValue("displayName");

            if (!node.HasValue("type")) throw new MissingFieldException($"The facility {name} has no type.");
            type = KCFacilityTypeRegistry.GetType(node.GetValue("type"));

            resourceCost = new SortedDictionary<int, Dictionary<PartResourceDefinition, double>>();
            Electricity = new SortedDictionary<int, double>();
            Funds = new SortedDictionary<int, double>();
            UpgradeTypes = new SortedDictionary<int, UpgradeType>();
            BasegroupNames = new SortedDictionary<int, string>();

            if (!node.HasNode("level")) throw new MissingFieldException($"The facility {name} has no level node.");
            ConfigNode levelNode = node.GetNode("level");
            levelNode.GetNodes().ToList().ForEach(n =>
            {
                int level = int.Parse(n.name);
                levelNodes.Add(level, n);
                if (n.HasValue("upgradeType")) UpgradeTypes.Add(level, (UpgradeType)Enum.Parse(typeof(UpgradeType), n.GetValue("upgradeType")));
                else UpgradeTypes.Add(level, UpgradeType.withoutGroupChange);

                if (n.HasValue("basegroupName")) BasegroupNames.Add(level, n.GetValue("basegroupName"));
                else if (level != 0) BasegroupNames.Add(level, BasegroupNames[level - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no basegroupName (at least for level 0).");

                if (n.HasNode("resources"))
                {
                    ConfigNode resourceNode = n.GetNode("resources");
                    Dictionary<PartResourceDefinition, double> resourceList = new Dictionary<PartResourceDefinition, double>();
                    foreach (ConfigNode.Value v in resourceNode.values)
                    {
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        double amount = double.Parse(v.value);
                        resourceList.Add(resourceDef, amount);
                    }
                    resourceCost.Add(level, resourceList);
                }
                else
                {
                    resourceCost.Add(level, new Dictionary<PartResourceDefinition, double>());
                }

                if (n.HasValue("Electricity")) Electricity.Add(level, double.Parse(n.GetValue("Electricity")));
                else Electricity.Add(level, 0);

                if (n.HasValue("Funds")) Funds.Add(level, double.Parse(n.GetValue("Funds")));
                else Funds.Add(level, 0);

                if (n.HasValue("upgradeTime")) UpgradeTimes.Add(level, float.Parse(n.GetValue("upgradeTime")));
                else UpgradeTimes.Add(level, 0);
            });
        }
    }

    public class KCZeroUpgradeInfoClass : KCFacilityInfoClass
    {

        public KCZeroUpgradeInfoClass(ConfigNode node) : base(node)
        {
            if (levelNodes.Count > 0) levelNodes = new SortedDictionary<int, ConfigNode> { { 0, levelNodes[0] } };
            if (resourceCost.Count > 0) resourceCost = new SortedDictionary<int, Dictionary<PartResourceDefinition, double>> { { 0, resourceCost[0] } };
            if (Electricity.Count > 0) Electricity = new SortedDictionary<int, double> { { 0, Electricity[0] } };
            if (Funds.Count > 0) Funds = new SortedDictionary<int, double> { { 0, Funds[0] } };
            if (UpgradeTypes.Count > 0) UpgradeTypes = new SortedDictionary<int, UpgradeType> { { 0, UpgradeTypes[0] } };
            if (BasegroupNames.Count > 0) BasegroupNames = new SortedDictionary<int, string> { { 0, BasegroupNames[0] } };
            if (UpgradeTimes.Count > 0) UpgradeTimes = new SortedDictionary<int, float> { { 0, UpgradeTimes[0] } };
        }
    }
}