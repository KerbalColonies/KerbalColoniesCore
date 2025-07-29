using KerbalColonies.UI;
using System.Collections.Generic;
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

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacilityPlacementWindow : KCWindowBase
    {
        private static KCMiningFacilityPlacementWindow instance;
        public static KCMiningFacilityPlacementWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KCMiningFacilityPlacementWindow();
                }
                return instance;
            }
        }

        public Dictionary<PartResourceDefinition, double> newRates { get; set; } = new Dictionary<PartResourceDefinition, double> { };

        Vector2 scrollPos = new Vector2();
        protected override void CustomWindow()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                newRates.ToList().ForEach(rate => GUILayout.Label($"{rate.Key.displayName}: {rate.Value}/day"));
            }
            GUILayout.EndScrollView();
        }

        public KCMiningFacilityPlacementWindow() : base(Configuration.createWindowID(), "Miningfacility placement info")
        {
            this.toolRect = new Rect(100, 100, 400, 300);
        }
    }
}
