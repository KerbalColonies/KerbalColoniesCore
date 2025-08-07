using KerbalColonies.UI;
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
        KCResourceConverterFacility resourceConverter;
        private RecipeSelectorWindow recipeSelector;
        public KerbalGUI kerbalGUI;

        Vector2 scrollPosISRUCount = Vector2.zero;

        protected override void CustomWindow()
        {
            resourceConverter.Colony.UpdateColony();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(resourceConverter, true);
            }

            ResourceConversionRate recipe = resourceConverter.activeRecipe;
            if (recipe == null)
            {
                GUILayout.Label($"Failed to load a recipe");
                if (GUILayout.Button("Select a new Recipe:"))
                {
                    recipeSelector.Open();
                }
                return;
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(toolRect.width / 2 - 10));
                {
                    GUILayout.Label($"Current recipe: {recipe.DisplayName}");

                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical();
                    GUILayout.Label("Input:");
                    foreach (PartResourceDefinition prd in recipe.InputResources.Keys)
                    {
                        GUILayout.Label(prd.displayName);
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.Label("Amount:");
                    foreach (double amount in recipe.InputResources.Values)
                    {
                        GUILayout.Label(amount.ToString());
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.Label("Output:");
                    foreach (PartResourceDefinition prd in recipe.OutputResources.Keys)
                    {
                        GUILayout.Label(prd.displayName);
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.Label("Amount:");
                    foreach (double amount in recipe.OutputResources.Values)
                    {
                        GUILayout.Label(amount.ToString());
                    }
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Select a new Recipe:"))
                    {
                        recipeSelector.Toggle();
                    }

                    facility.enabled = GUILayout.Toggle(facility.enabled, "enable/disable");

                    resourceConverter.outOfResourceDisable = GUILayout.Toggle(resourceConverter.outOfResourceDisable, "Disable facility if resources are missing");
                    resourceConverter.outOfECDisable = GUILayout.Toggle(resourceConverter.outOfECDisable, "Disable facility if EC is missing");


                    GUILayout.Label($"Current ISRU count: {resourceConverter.ISRUcount()}");
                    GUILayout.Label("This facility works with the following ISRU counts for the following kerbal counts.");

                    scrollPosISRUCount = GUILayout.BeginScrollView(scrollPosISRUCount);
                    {
                        KCResourceConverterInfo info = resourceConverter.info;
                        SortedDictionary<int, int> ISRUperKerbals = new SortedDictionary<int, int>();
                        resourceConverter.AvailableISRUCounts.ToList().ForEach(kvp =>
                        {
                            int kerbals = info.minKerbals[kvp.Key];
                            if (!ISRUperKerbals.ContainsKey(kerbals)) ISRUperKerbals.Add(kerbals, kvp.Value);
                            else ISRUperKerbals[kerbals] = Math.Max(ISRUperKerbals[kerbals], kvp.Value);
                        });

                        List<int> kerbalCounts = new List<int>();
                        ISRUperKerbals.ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key} Kerbals = {kvp.Value} ISRUs"));
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(toolRect.width / 2 - 10));
                {
                    kerbalGUI.StaffingInterface();
                    if (facility.facilityInfo.ECperSecond[facility.level] > 0)
                    {
                        GUILayout.Space(10);
                        GUILayout.Label($"EC/s: {(facility.enabled ? facility.facilityInfo.ECperSecond[facility.level] * resourceConverter.ISRUcount() : 0):f2}");
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"EC Consumption Priority: {resourceConverter.ECConsumptionPriority}", GUILayout.Height(18));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) resourceConverter.ECConsumptionPriority--;
                            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) resourceConverter.ECConsumptionPriority++;
                        }
                        GUILayout.EndHorizontal();
                    }

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
            this.recipeSelector = new RecipeSelectorWindow(resourceConverter);
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 700, 500);

        }
    }
}
