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
                    resourceConverter.ChangeRecipe(recipe);
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
