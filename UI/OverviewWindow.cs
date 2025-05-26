using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.UI
{
    public class OverviewWindow : KCWindowBase
    {
        private static OverviewWindow instance = null;
        public static OverviewWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OverviewWindow();
                }
                return instance;
            }
        }

        private Vector2 scrollPointer;

        private bool showNameField = false;
        private string newTitle;
        private colonyClass selectedColony;
        protected override void CustomWindow()
        {
            GUIStyle borderOnlyStyle = new GUIStyle(GUI.skin.box);

            // Create a 1x1 transparent texture so background is invisible
            Texture2D transparentTex = new Texture2D(1, 1);
            transparentTex.SetPixel(0, 0, Color.clear);
            transparentTex.Apply();

            borderOnlyStyle.normal.background = transparentTex;
            borderOnlyStyle.normal.textColor = Color.white;
            borderOnlyStyle.border = new RectOffset(2, 2, 2, 2);
            borderOnlyStyle.margin = new RectOffset(10, 10, 10, 10);
            borderOnlyStyle.padding = new RectOffset(10, 10, 10, 10);

            GUILayout.Label("Colony list:");

            GUILayout.BeginScrollView(scrollPointer);
            Configuration.colonyDictionary.SelectMany(x => x.Value).ToList().ForEach(colony =>
            {
                GUILayout.BeginHorizontal(borderOnlyStyle);

                GUI.enabled = !showNameField;
                if (GUILayout.Button(colony.DisplayName))
                {
                    showNameField = true;
                    newTitle = colony.DisplayName;
                    selectedColony = colony;
                }

                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open CAB"))
                {
                    colony.CAB.Update();
                    colony.CAB.OnRemoteClicked();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            });
            GUILayout.EndScrollView();

            if (showNameField)
            {
                GUILayout.Label("Enter new Name: ");

                newTitle = GUILayout.TextField(newTitle, GUILayout.Width(150));

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("OK", GUILayout.Height(23)))
                    {
                        Configuration.writeDebug($"Changing the name of the {selectedColony.Name} from {selectedColony.DisplayName} to {newTitle}");
                        selectedColony.DisplayName = newTitle;
                        showNameField = false;
                        selectedColony.Facilities.ForEach(facility => facility.OnColonyNameChange(title));
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Height(23)))
                    {
                        showNameField = false;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        public static void ToggleWindow()
        {
            Instance.Toggle();
        }

        /// <summary>
        /// Used for the toolbar button
        /// </summary>
        public static void OnTrue()
        {
            Instance.Open();
        }

        /// <summary>
        /// Used for the toolbar button
        /// </summary>
        public static void OnFalse()
        {
            Instance.Close();
        }

        protected override void OnClose()
        {
            //KerbalColonies.toolbarControl.enabled = false;
            //KerbalColonies.toolbarControl.Enabled = false;
            //KerbalColonies.toolbarControl.buttonActive = false;
        }

        private OverviewWindow() : base(Configuration.createWindowID(), "Overview")
        {
            toolRect = new Rect(100, 100, 330, 600);
        }
    }
}
