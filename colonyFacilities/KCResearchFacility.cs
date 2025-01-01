using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;


namespace KerbalColonies.colonyFacilities
{
    internal class KCResearchFacilityCost : KCFacilityCostClass
    {
        public override bool VesselHasRessources(Vessel vessel, int level)
        {
            for (int i = 0; i < resourceCost.Count; i++)
            {
                vessel.GetConnectedResourceTotals(resourceCost.ElementAt(i).Key.id, false, out double amount, out double maxAmount);

                if (amount < resourceCost.ElementAt(i).Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool RemoveVesselRessources(Vessel vessel, int level)
        {
            if (VesselHasRessources(vessel, 0))
            {
                for (int i = 0; i < resourceCost.Count; i++)
                {
                    vessel.RequestResource(vessel.rootPart, resourceCost.ElementAt(i).Key.id, resourceCost.ElementAt(i).Value, true);
                }
                return true;
            }
            return false;
        }

        public KCResearchFacilityCost()
        {
            resourceCost = new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("Ore"), 200f }, { PartResourceLibrary.Instance.GetDefinition("MonoPropellant"), 100f } };
        }
    }

    internal class KCResearchFacilityWindow : KCWindowBase
    {
        KCResearchFacility facility;

        protected override void CustomWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Science Points: " + facility.SciencePoints);
            GUILayout.Label("Max Science Points: " + facility.MaxSciencePoints);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve Science Points"))
            {
                facility.RetrieveSciencePoints();
            }
            GUILayout.EndHorizontal();
        }

        public KCResearchFacilityWindow(KCResearchFacility facility) : base(Configuration.createWindowID(facility), "Researchfacility")
        {
            this.facility = facility;
            toolRect = new Rect(100, 100, 400, 400);
        }
    }


    [System.Serializable]
    internal class KCResearchFacility : KCKerbalFacilityBase
    {
        private KCResearchFacilityWindow researchFacilityWindow;

        public float sciencePoints;

        public float MaxSciencePoints { get { return maxSciencePointList[level]; } }
        public float SciencePoints { get { return sciencePoints; } }

        private List<float> maxSciencePointList = new List<float> { 50, 100, 200, 400 };
        private List<float> researchpointsPerDayperResearcher = new List<float> { 0.1f, 0.15f, 0.2f, 0.25f };

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            sciencePoints = Math.Min(maxSciencePointList[level], sciencePoints + (float)(researchpointsPerDayperResearcher[level] / 24 / 60 / 60 * deltaTime)); //  * kerbals.Count
            //ResearchAndDevelopment.Instance.AddScience((float) (researchpointsPerDayperResearcher[level] / 24 / 60 / 60 * deltaTime) * kerbals.Count, TransactionReasons.Cheating);
        }

        public override void OnBuildingClicked()
        {
            researchFacilityWindow.Toggle();
        }

        public bool RetrieveSciencePoints()
        {
            if (sciencePoints > 0)
            {
                ResearchAndDevelopment.Instance.AddScience(sciencePoints, TransactionReasons.Cheating);
                sciencePoints = 0;
                return true;
            }
            return false;
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
            this.baseGroupName = "KC_CAB";

            maxSciencePointList = new List<float> { 50, 100, 200, 400 };
            researchpointsPerDayperResearcher = new List<float> { 0.05f, 0.1f, 0.15f, 0.2f };
        }

        public KCResearchFacility(bool enabled, string facilityData = "") : base("KCResearchFacility", true, 8) {
            sciencePoints = 0;
        }
    }
}
