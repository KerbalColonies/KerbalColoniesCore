using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class RecipeSelectorWindow : KCWindowBase
    {
        KCResourceConverterFacility resourceConverter;

        Vector2 scrollPos;

        protected override void CustomWindow()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            foreach (ResourceConversionRate recipe in resourceConverter.availableRecipes().GetRecipes())
            {
                GUILayout.Label(recipe.DisplayName);
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

                if (GUILayout.Button("Use this recipe"))
                {
                    resourceConverter.activeRecipe = recipe;
                    this.Close();
                }
                GUILayout.Space(10);
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(10);
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public RecipeSelectorWindow(KCResourceConverterFacility resourceConverter) : base(Configuration.createWindowID(), "Recipe Selector")
        {
            this.resourceConverter = resourceConverter;
            toolRect = new Rect(100, 100, 400, 800);
        }
    }
}
