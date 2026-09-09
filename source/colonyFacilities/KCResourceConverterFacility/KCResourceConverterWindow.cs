using KerbalColonies.Settings;
using KerbalColonies.UI;
using KSP.Localization;
using System;
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

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class KCResourceConverterWindow : KCFacilityWindowBase
    {
        private KCResourceConverterFacility resourceConverter;
        private RecipeSelectorWindow recipeSelector;
        public KerbalGUI kerbalGUI;

        private Vector2 scrollPosISRUCount = Vector2.zero;
        private Vector2 resourceScrollPos = new();
        protected override void CustomWindow()
        {
            resourceConverter.Colony.UpdateColony();

            kerbalGUI ??= new KerbalGUI(resourceConverter, true);

            ResourceConversionRate recipe = resourceConverter.activeRecipe;
            if (recipe == null)
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_RECIPE_LOAD_FAILED"));
                if (GUILayout.Button(Localizer.Format("#LOC_KC_CONVERTER_SELECT_RECIPE")))
                {
                    recipeSelector.Open();
                }
                return;
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width((toolRect.width * 3.5f / 10) - 10));
                {
                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_CURRENT_RECIPE", recipe.DisplayName));

                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical();
                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_INPUT"));
                    foreach (PartResourceDefinition prd in recipe.InputResources.Keys)
                    {
                        GUILayout.Label(prd.displayName);
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_AMOUNT"));
                    foreach (double amount in recipe.InputResources.Values)
                    {
                        GUILayout.Label(amount.ToString());
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_OUTPUT"));
                    foreach (PartResourceDefinition prd in recipe.OutputResources.Keys)
                    {
                        GUILayout.Label(prd.displayName);
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_AMOUNT"));
                    foreach (double amount in recipe.OutputResources.Values)
                    {
                        GUILayout.Label(amount.ToString());
                    }
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    if (GUILayout.Button(Localizer.Format("#LOC_KC_CONVERTER_SELECT_RECIPE")))
                    {
                        recipeSelector.Toggle();
                    }

                    facility.enabled = GUILayout.Toggle(facility.enabled, Localizer.Format("#LOC_KC_CONVERTER_ENABLE"));

                    resourceConverter.outOfResourceDisable = GUILayout.Toggle(resourceConverter.outOfResourceDisable, Localizer.Format("#LOC_KC_CONVERTER_DISABLE_ON_SHORTAGE"));

                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_ISRU_COUNT", resourceConverter.ISRUcount()));
                    GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_ISRU_DESCRIPTION"));

                    scrollPosISRUCount = GUILayout.BeginScrollView(scrollPosISRUCount);
                    {
                        KCResourceConverterInfo info = resourceConverter.info;
                        SortedDictionary<int, int> ISRUperKerbals = [];
                        resourceConverter.AvailableISRUCounts.ToList().ForEach(kvp =>
                        {
                            int kerbals = info.minKerbals[kvp.Key];
                            if (!ISRUperKerbals.ContainsKey(kerbals)) ISRUperKerbals.Add(kerbals, kvp.Value);
                            else ISRUperKerbals[kerbals] = Math.Max(ISRUperKerbals[kerbals], kvp.Value);
                        });

                        List<int> kerbalCounts = [];
                        ISRUperKerbals.ToList().ForEach(kvp => GUILayout.Label(Localizer.Format("#LOC_KC_CONVERTER_ISRU_ITEM", kvp.Key, kvp.Value)));
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width((toolRect.width * 3f / 10) - 10));
                {

                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_RESOURCE_PRIORITY", resourceConverter.ResourceConsumptionPriority), GUILayout.Height(18));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) resourceConverter.ResourceConsumptionPriority--;
                        if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) resourceConverter.ResourceConsumptionPriority++;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(20);

                    resourceScrollPos = GUILayout.BeginScrollView(resourceScrollPos);
                    {
                        resourceConverter.resourceLimitsEnabled.ToList().ForEach(res =>
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label($"<size=20><b>{res.Key.displayName}</b></size>");

                                GUILayout.FlexibleSpace();

                                GUILayout.BeginVertical(GUILayout.Width(150));
                                {
                                    resourceConverter.resourceLimitsEnabled[res.Key] = GUILayout.Toggle(res.Value, Localizer.Format("#LOC_KC_CONVERTER_RESOURCE_LIMITS"));
                                    if (double.TryParse(GUILayout.TextField(resourceConverter.resourceLimits[res.Key].ToString("F3")), out double autoLimit)) resourceConverter.resourceLimits[res.Key] = autoLimit;
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(10);
                            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                            GUILayout.Space(10);
                        });
                    }
                    GUILayout.EndScrollView();

                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width((toolRect.width * 3.5f / 10) - 10));
                {
                    kerbalGUI.StaffingInterface();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();


        }

        protected override void OnClose()
        {
            recipeSelector.Close();
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCResourceConverterWindow(KCResourceConverterFacility resourceConverter) : base(resourceConverter, Configuration.createWindowID())
        {
            this.resourceConverter = resourceConverter;
            recipeSelector = new RecipeSelectorWindow(resourceConverter);
            kerbalGUI = null;
            toolRect = new Rect(100, 100, 1050, 500);

        }
    }
}
