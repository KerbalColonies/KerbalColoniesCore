using KerbalKonstructs.Modules;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCKerbalTestWindow : KCWindowBase
    {
        KCKerbalTestFacility testFacility;

        protected override void CustomWindow()
        {
            KerbalGUI kerbalGUI = new KerbalGUI(testFacility);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUI.enabled = true;

            kerbalGUI.StaffingInterface(5);

            GUILayout.EndHorizontal();

            GUILayout.Space(2);
        }

        public KCKerbalTestWindow(KCKerbalTestFacility testFacility) : base(Configuration.createWindowID(testFacility))
        {
            this.testFacility = testFacility;
            toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    [System.Serializable]
    internal class KCKerbalTestFacility : KCKerbalFacilityBase
    {
        private KCKerbalTestWindow testWindow;

        internal override void Update()
        {
            base.Update();
        }

        internal override void OnBuildingClicked()
        {
            KSPLog.print("KCKerbalTestWindow: " + testWindow.ToString());
            testWindow.Toggle();
        }

        internal override void Initialize(string facilityName, int id, string facilityData, bool enabled)
        {
            base.Initialize(facilityName, id, facilityData, enabled);
            this.testWindow = new KCKerbalTestWindow(this);
        }

        internal KCKerbalTestFacility(bool enabled) : base()
        {
            Initialize("KCKerbalTestFacility", createID(), "", enabled);
        }
    }
}
