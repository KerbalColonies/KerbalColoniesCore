using KerbalColonies.Settings;
using KerbalColonies.UI;
using KSP.Localization;
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

namespace KerbalColonies.colonyFacilities.ResearchFacility
{
    public class KCResearchFacilityWindow : KCFacilityWindowBase
    {
        private KCResearchFacility researchFacility;
        public KerbalGUI kerbalGUI;

        private Vector2 resourceUsageScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            researchFacility.Colony.UpdateColony();

            kerbalGUI ??= new KerbalGUI(researchFacility, true);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KC_RESEARCH_SCIENCE", researchFacility.SciencePoints.ToString("f2")));
            GUILayout.Label(Localizer.Format("#LOC_KC_RESEARCH_MAX_SCIENCE", researchFacility.MaxSciencePoints.ToString("f2")));
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUI.enabled = facility.enabled;
            if (GUILayout.Button(Localizer.Format("#LOC_KC_RESEARCH_RETRIEVE")))
                researchFacility.RetrieveSciencePoints();


            if (facility.facilityInfo.ResourceUsage[facility.level].Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_PRIORITY", researchFacility.ResourceConsumptionPriority), GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) researchFacility.ResourceConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) researchFacility.ResourceConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_USAGE"));
                resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(120));
                {
                    researchFacility.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                        GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_RATE_ITEM", kvp.Key.displayName, kvp.Value))
                    );
                }
                GUILayout.EndScrollView();
            }
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
            kerbalGUI = null;
        }
    }
}
