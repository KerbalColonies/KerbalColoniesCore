using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuarterCost : KCFacilityCostClass
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

        public KCCrewQuarterCost()
        {
            resourceCost = new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("Ore"), 100f } };
        }
    }

    internal class KCCrewQuartersWindow : KCWindowBase
    {
        KCCrewQuarters CrewQuarterFacility;
        VesselKerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            KSPLog.print("KCCrewQuartersWindow: " + this.ToString());

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUI.enabled = true;

            kerbalGUI.StaffingInterface(CrewQuarterFacility.MaxKerbals);

            GUILayout.EndHorizontal();

            GUILayout.Space(2);
        }

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(Configuration.createWindowID(CrewQuarterFacility), CrewQuarterFacility.name)
        {
            this.CrewQuarterFacility = CrewQuarterFacility;
            this.kerbalGUI = new VesselKerbalGUI(CrewQuarterFacility);
            toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    [System.Serializable]
    internal class KCCrewQuarters : KCKerbalFacilityBase
    {
        private KCCrewQuartersWindow testWindow;

        internal override void Update()
        {
            base.Update();
        }

        internal override void OnBuildingClicked()
        {
            KSPLog.print("KCCrewQuarters: " + this.ToString());
            testWindow.Toggle();
        }

        internal override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            this.testWindow = new KCCrewQuartersWindow(this);
        }

        public KCCrewQuarters(bool enabled, string facilityData) : base("KCCrewQuarters", true, 16)
        {
        }
    }
}
