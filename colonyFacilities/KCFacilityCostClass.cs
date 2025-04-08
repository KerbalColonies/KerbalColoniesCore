using System.Collections.Generic;

namespace KerbalColonies.colonyFacilities
{
    /// <summary>
    /// A class that's used to check and remove ressources from an vessel or other places. Currently only vessels are supported
    /// <para>
    /// It's also used to check to cost for upgrades of a facility.
    /// </para>
    /// <para>Level 0 is passed when building this facility.</para>
    /// <para>The maximum level is the inclusive upper limit, e.g. a facility with maxLevel = 1 can be built and then upgraded once to level 1.</para>
    /// </summary>
    public abstract class KCFacilityCostClass
    {
        /// <summary>
        /// Level, Resource, Amount
        /// </summary>
        public Dictionary<int, Dictionary<PartResourceDefinition, double>> resourceCost;
        /// <summary>
        /// currently unused
        /// </summary>
        public double electricity;
        public double funds;

        /// <summary>
        /// Used for custom checks (e.g. if a specific facility already exists in the colony), returns true if the facility can be built or upgraded.
        /// </summary>
        public virtual bool customCheck(int level, colonyClass colony)
        {
            return true;
        }

        public static bool checkResources(KCFacilityCostClass facilityCost, int level, colonyClass colony)
        {
            if (!facilityCost.customCheck(level, colony))
            {
                return false;
            }

            foreach (KeyValuePair<PartResourceDefinition, double> resource in facilityCost.resourceCost[level])
            {
                double vesselAmount = 0;
                double colonyAmount = KCStorageFacility.colonyResources(resource.Key, colony);

                if (FlightGlobals.ActiveVessel != null)
                {
                    FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);
                    if (amount >= resource.Value)
                    {
                        continue;
                    }
                    vesselAmount = amount;
                }

                else if (resource.Value <= colonyAmount)
                {
                    continue;
                }

                else
                {
                    if (vesselAmount + colonyAmount < resource.Value)
                    {
                        return false;
                    }
                }
            }

            if (Funding.Instance != null)
            {
                if (Funding.Instance.Funds < facilityCost.funds)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool removeResources(KCFacilityCostClass facilityCost, int level, colonyClass colony)
        {
            if (checkResources(facilityCost, level, colony))
            {
                foreach (KeyValuePair<PartResourceDefinition, double> resource in facilityCost.resourceCost[level])
                {
                    double remainingAmount = resource.Value;

                    double vesselAmount = 0;
                    double colonyAmount = KCStorageFacility.colonyResources(resource.Key, colony);
                    if (FlightGlobals.ActiveVessel != null)
                    {
                        FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.Key.id, out double amount, out double maxAmount);
                        vesselAmount = amount;
                    }

                    if (vesselAmount >= resource.Value)
                    {
                        FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, resource.Value, true);
                    }
                    else
                    {
                        if (FlightGlobals.ActiveVessel != null)
                        {
                            FlightGlobals.ActiveVessel.RequestResource(FlightGlobals.ActiveVessel.rootPart, resource.Key.id, resource.Value, true);
                        }
                        remainingAmount -= vesselAmount;

                        KCStorageFacility.addResourceToColony(resource.Key, -remainingAmount, colony);
                    }
                }
                if (Funding.Instance != null)
                {
                    Funding.Instance.AddFunds(-facilityCost.funds, TransactionReasons.None);
                }
                return true;
            }
            return false;
        }
    }
}