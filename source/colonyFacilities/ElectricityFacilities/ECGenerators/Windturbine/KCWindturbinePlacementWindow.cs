using KerbalColonies.UI;
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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Windturbine
{
    public class KCWindturbinePlacementWindow : KCWindowBase
    {
        private static KCWindturbinePlacementWindow instance;

        public static KCWindturbinePlacementWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KCWindturbinePlacementWindow();
                }
                return instance;
            }
        }

        public double ECProductionRate { get; set; } = 0.0;
        public double Density { get; set; } = 0.0;
        public double Pressure { get; set; } = 0.0;
        public double Temperature { get; set; } = 0.0;

        protected override void CustomWindow()
        {
            GUILayout.Label($"Wind turbine production rate: {ECProductionRate} EC/s");
            GUILayout.Label($"Density: {Density} kg/m³");
            GUILayout.Label($"Pressure: {Pressure} Pa");
        }

        public KCWindturbinePlacementWindow() : base(Configuration.createWindowID(), "Wind turbine placement info")
        {
            this.toolRect = new Rect(100, 100, 300, 200);
        }
    }
}
