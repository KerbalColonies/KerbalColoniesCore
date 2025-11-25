using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.ResourceManagment;
using KerbalColonies.Settings;
using KerbalColonies.UI;
using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.FuelCell
{
    public class KCFuelCellWindow : KCFacilityWindowBase
    {
        private KCFuelCellFacility fuelCellFacility => (KCFuelCellFacility)facility;

        private Vector2 resourceProductionScrollPos = Vector2.zero;
        private Vector2 resourceUseageScrollPos = new();
        private Vector2 resourceDeltaScrollPos = new();
        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();
            GUILayout.Label("Resource Production:");
            resourceProductionScrollPos = GUILayout.BeginScrollView(resourceProductionScrollPos);
            {
                fuelCellFacility.facilityInfo.ResourceUsage[facility.level].Where(x => x.Value > 0).ToList().ForEach(kvp =>
                    GUILayout.Label($"- {kvp.Key.name}: {kvp.Value:f2}/s, {KCUnifiedColonyStorage.colonyStorages[facility.Colony].Resources[kvp.Key]:f2} stored")
                );
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Resource consumption:");
            resourceUseageScrollPos = GUILayout.BeginScrollView(resourceUseageScrollPos);
            {
                fuelCellFacility.facilityInfo.ResourceUsage[facility.level].Where(x => x.Value < 0).ToList().ForEach(kvp =>
                    GUILayout.Label($"- {kvp.Key.name}: {kvp.Value:f2}/s, {KCUnifiedColonyStorage.colonyStorages[facility.Colony].Resources[kvp.Key]:f2} stored")
                );
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);
            facility.enabled = GUILayout.Toggle(facility.enabled, "Enabled", GUILayout.Height(18));
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"Resource Consumption Priority: {fuelCellFacility.ResourceConsumptionPriority}", GUILayout.Height(18));
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) fuelCellFacility.ResourceConsumptionPriority--;
                if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) fuelCellFacility.ResourceConsumptionPriority++;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Resource Deltas:");
            resourceDeltaScrollPos = GUILayout.BeginScrollView(resourceDeltaScrollPos, GUILayout.Height(120));
            {
                fuelCellFacility.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                    GUILayout.Label($"- {kvp.Key.displayName}: {KCResourceManager.colonyResources[facility.Colony].ResourceDelta(kvp.Key) / KCResourceManager.colonyResources[facility.Colony].deltaTime}/s")
                );
            }
            GUILayout.EndScrollView();
        }

        public KCFuelCellWindow(KCFuelCellFacility facility) : base(facility, Configuration.createWindowID())
        {
            toolRect = new Rect(100, 100, 330, 600);
        }

    }
}
