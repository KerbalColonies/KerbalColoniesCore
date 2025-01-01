using System.Collections.Generic;

namespace KerbalColonies.colonyFacilities
{
    internal class KCResearchFacility : KCKerbalFacilityBase
    {
        private List<float> researchpointsPerDayperResearcher = new List<float> { 0.05f, 0.1f, 0.15f, 0.2f };

        public override void Update()
        {
            double deltaTime = HighLogic.CurrentGame.UniversalTime - lastUpdateTime;
            lastUpdateTime = HighLogic.CurrentGame.UniversalTime;

            ResearchAndDevelopment.Instance.AddScience((float) (researchpointsPerDayperResearcher[level] / 24 / 60 / 60 * deltaTime) * kerbals.Count, TransactionReasons.Cheating);
        }

        public KCResearchFacility(bool enabled, string facilityData = "") : base("KCResearchFacility", true, 8) { }
    }
}
