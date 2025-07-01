using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.FuelCell
{
    public class KCFuelCellWindow : KCFacilityWindowBase
    {
        KCFuelCellFacility fuelCellFacility => (KCFuelCellFacility)facility;

        Vector2 resourceScrollPos = new Vector2();
        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();
            GUILayout.Label($"Current EC Production: {(facility.enabled ? fuelCellFacility.ECPerSecond() : 0)} EC/s");
            GUILayout.Space(10);
            GUILayout.Label("Resource consumption:");
            resourceScrollPos = GUILayout.BeginScrollView(resourceScrollPos);
            {
                fuelCellFacility.fuelCellInfo.ResourceConsumption[facility.level].ToList().ForEach(kvp =>
                {
                    GUILayout.Label($"- {kvp.Key.name}: {kvp.Value:f2}/s, {KCStorageFacility.colonyResources(kvp.Key, facility.Colony):f2} stored");
                });
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);
            facility.enabled = GUILayout.Toggle(facility.enabled, "Enabled", GUILayout.Height(18));
            GUILayout.Space(10);

            GUILayout.Label($"Current EC delta: {(KCECManager.colonyEC[facility.Colony].lastECDelta / KCECManager.colonyEC[facility.Colony].deltaTime):f2} EC/s");
        }

        public KCFuelCellWindow(KCFuelCellFacility facility) : base(facility, Configuration.createWindowID())
        {
            this.toolRect = new Rect(100, 100, 330, 600);
        }

    }
}
