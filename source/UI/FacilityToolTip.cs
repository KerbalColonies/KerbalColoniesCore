using ClickThroughFix;
using KerbalColonies.Settings;
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
    public class FacilityToolTip : KCWindow
    {
        protected int windowID;
        protected Rect toolRect = new(100, 100, 250, 120);
        public readonly Vector2 offset = new(20, 20);
        public static string FacilityTitle { get; set; }
        public static string FacilityText { get; set; }

        private static FacilityToolTip instance;
        public static FacilityToolTip Instance
        {
            get
            {
                instance ??= new FacilityToolTip();
                return instance;
            }
        }

        public override void Draw()
        {
            toolRect = ClickThruBlocker.GUIWindow(windowID, toolRect, KCWindow, "", UIConfig.KKWindow);
        }

        private void KCWindow(int windowID)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label($"<b><color=yellow><size=16>{FacilityTitle}</size></color></b>", UIConfig.LabelWhite);
                GUILayout.Label($"<color=white>{FacilityText}</color>");
            }
            GUILayout.EndVertical();

            toolRect.position = UnityEngine.Input.mousePosition;
            toolRect.y = Screen.height - toolRect.y;

            // offset
            toolRect.position += offset;
        }

        public FacilityToolTip() : base()
        {
            windowID = Configuration.createWindowID();
        }
    }
}
