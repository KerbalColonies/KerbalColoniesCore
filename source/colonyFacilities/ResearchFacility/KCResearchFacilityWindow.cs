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

namespace KerbalColonies.colonyFacilities.ResearchFacility
{
    public class KCResearchFacilityWindow : KCFacilityWindowBase
    {
        KCResearchFacility researchFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            researchFacility.Colony.UpdateColony();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(researchFacility, true);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Science Points: {researchFacility.SciencePoints:f2}");
            GUILayout.Label($"Max Science Points: {researchFacility.MaxSciencePoints:f2}");
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUI.enabled = facility.enabled;
            if (GUILayout.Button("Retrieve Science Points"))
                researchFacility.RetrieveSciencePoints();


            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"EC Consumption Priority: {researchFacility.ResourceConsumptionPriority}", GUILayout.Height(18));
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) researchFacility.ResourceConsumptionPriority--;
                if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) researchFacility.ResourceConsumptionPriority++;
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCResearchFacilityWindow(KCResearchFacility researchFacility) : base(researchFacility, Configuration.createWindowID())
        {
            this.researchFacility = researchFacility;
            toolRect = new Rect(100, 100, 400, 800);
            this.kerbalGUI = null;
        }
    }
}
