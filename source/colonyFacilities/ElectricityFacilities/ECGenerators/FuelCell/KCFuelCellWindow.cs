using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.Electricity;
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
        KCFuelCellFacility fuelCellFacility => (KCFuelCellFacility)facility;

        Vector2 resourceProductionScrollPos = Vector2.zero;
        Vector2 resourceUseageScrollPos = new Vector2();
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

            GUILayout.Label($"Current EC delta: {(KCECManager.colonyEC[facility.Colony].lastECDelta / KCECManager.colonyEC[facility.Colony].deltaTime):f2} EC/s");
        }

        public KCFuelCellWindow(KCFuelCellFacility facility) : base(facility, Configuration.createWindowID())
        {
            this.toolRect = new Rect(100, 100, 330, 600);
        }

    }
}
