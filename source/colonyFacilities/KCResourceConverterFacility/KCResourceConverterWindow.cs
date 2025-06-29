using KerbalColonies.UI;
using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            resourceConverter.Update();

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
                GUILayout.BeginVertical(GUILayout.Width(toolRect.width / 2 -10));
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
