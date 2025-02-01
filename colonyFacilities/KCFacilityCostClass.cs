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
        public Dictionary<int, Dictionary<PartResourceDefinition, float>> resourceCost;

        public virtual string GetRessourceCostString(int level) { return ""; }

        public abstract bool VesselHasRessources(Vessel vessel, int level);

        public abstract bool RemoveVesselRessources(Vessel vessel, int level);
    }
}