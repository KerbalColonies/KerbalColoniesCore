using ClickThroughFix;
using KerbalColonies.UI.SingleTimeWindow;
using System;
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

namespace KerbalColonies.UI.SingleTimePopup
{
    public abstract class KCSingleTimeWindowBase : KCWindow, IComparable<KCSingleTimeWindowBase>
    {
        public bool Mainmenu = false;
        public bool KSC = false;
        public bool Editor = false;
        public bool Flight = false;
        public bool Trackingstation = false;


        protected GUIStyle LabelGreen;
        public string identifier { get; private set; }

        protected int windowID;
        protected string title;
        private bool guiInitialized;

        protected Rect toolRect = new Rect(100, 100, 330, 100);

        public bool showAgain = true;

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

        void KCWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                GUILayout.Button("-KC-", UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();


                GUILayout.Label(title, UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUI.enabled = true;

                if (GUILayout.Button("X", UIConfig.DeadButtonRed, GUILayout.Height(21)))
                {
                    showAgain = true;
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(1);

            CustomWindow();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Don't show again", GUILayout.Width(toolRect.width / 2 - 15))) { showAgain = false; this.Close(); }
                if (GUILayout.Button("Close", GUILayout.Width(toolRect.width / 2 - 15))) { showAgain = true; this.Close(); }
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public int CompareTo(KCSingleTimeWindowBase other)
        {
            if (ReferenceEquals(other, null)) return 1; // null is always less than any instance
            return string.Compare(this.identifier, other.identifier, StringComparison.Ordinal);
        }

        public static bool operator ==(KCSingleTimeWindowBase a, KCSingleTimeWindowBase b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.identifier == b.identifier;
        }
        public static bool operator !=(KCSingleTimeWindowBase a, KCSingleTimeWindowBase b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;
            return a.identifier != b.identifier;
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj is KCSingleTimeWindowBase other && ((KCSingleTimeWindowBase)obj).identifier == this.identifier;
        }

        public override int GetHashCode()
        {
            return identifier.GetHashCode();
        }

        public KCSingleTimeWindowBase(string title, string identifier, bool Mainmenu, bool KSC, bool Editor, bool Flight, bool Trackingstation)
        {
            this.windowID = Configuration.createWindowID();
            this.identifier = identifier;
            this.title = title;
            this.Mainmenu = Mainmenu;
            this.KSC = KSC;
            this.Editor = Editor;
            this.Flight = Flight;
            this.Trackingstation = Trackingstation;

            if (!SingleTimeWindowManager.shownWindows.ContainsKey(identifier))
            {
                SingleTimeWindowManager.shownWindows.Add(identifier, true);
            }
            else
            {
                showAgain = SingleTimeWindowManager.shownWindows[identifier];
            }
        }
    }
}


