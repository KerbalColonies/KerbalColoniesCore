using ClickThroughFix;
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

namespace KerbalColonies.UI
{
    public abstract class KCWindowBase : KCWindow
    {
        protected GUIStyle LabelGreen;

        protected int windowID;
        protected string title;
        protected bool canChangeTitle = false;
        private bool guiInitialized;

        protected Rect toolRect = new Rect(100, 100, 330, 100);

        public virtual void OnTitleChange(string title) { }

        public override void Draw()
        {
            if (!guiInitialized)
            {
                InitializeLayout();
                guiInitialized = true;
            }

            drawEditor();
        }

        private void InitializeLayout()
        {
            LabelGreen = new GUIStyle(GUI.skin.label);
            LabelGreen.normal.textColor = Color.green;
            LabelGreen.fontSize = 13;
            LabelGreen.fontStyle = FontStyle.Bold;
            LabelGreen.padding.bottom = 1;
            LabelGreen.padding.top = 1;
        }

        internal void drawEditor()
        {

            toolRect = ClickThruBlocker.GUIWindow(windowID, toolRect, KCWindow, "", UIConfig.KKWindow);
        }

        protected abstract void CustomWindow();


        private bool showNameField = false;
        private string newTitle;
        void KCWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                GUILayout.Button("-KC-", UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUI.enabled = canChangeTitle;

                if (GUILayout.Button(title, UIConfig.DeadButton, GUILayout.Height(21)))
                {
                    showNameField = true;
                    newTitle = title;
                }

                GUILayout.FlexibleSpace();

                GUI.enabled = true;

                if (GUILayout.Button("X", UIConfig.DeadButtonRed, GUILayout.Height(21)))
                {
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(1);

            if (showNameField)
            {
                GUILayout.Label("Enter new Name: ");

                newTitle = GUILayout.TextField(newTitle);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("OK", GUILayout.Height(23)))
                    {
                        OnTitleChange(newTitle);
                        title = newTitle;
                        showNameField = false;
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Height(23)))
                    {
                        showNameField = false;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            CustomWindow();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public KCWindowBase(int windowID, string title, bool canChangeTitle = false)
        {
            this.windowID = windowID;
            this.title = title;
            this.canChangeTitle = canChangeTitle;
        }
    }
}
