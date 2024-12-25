using KerbalKonstructs.Modules;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
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

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(Configuration.createWindowID(CrewQuarterFacility))
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

        internal override void Initialize(string facilityName, int id, string facilityData, bool enabled)
        {
            base.Initialize(facilityName, id, facilityData, enabled);
            this.testWindow = new KCCrewQuartersWindow(this);
        }

        internal KCCrewQuarters(bool enabled, string facilityData = "") : base("KCCrewQuarters", true, 16)
        {
        }
    }
}
