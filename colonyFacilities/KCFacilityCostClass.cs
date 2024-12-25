using System.Collections.Generic;

namespace KerbalColonies.colonyFacilities
{
    /// <summary>
    /// A class that's used to check and remove ressources from an vessel or other places. Currently only vessels are supported
    /// </summary>
    public abstract class KCFacilityCostClass
    {
        public Dictionary<PartResourceDefinition, float> resourceCost;

        public virtual string GetRessourceCostString() { return ""; }

        public abstract bool VesselHasRessources(Vessel vessel);

        public abstract bool RemoveVesselRessources(Vessel vessel);
    }
}