using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.CabFacility;
using KerbalColonies.Settings;
using KerbalKonstructs;
using KerbalKonstructs.UI;
using KSP.Localization;
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

/// This file is based on the Kerbal Konstructs mod GroupEditor
/// It mainly doesn't offers the player the option to delete the group

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.UI
{
    public class KCGroupEditor : GroupEditor
    {
        protected static KCGroupEditor _kcInstance = null;
        public static KCGroupEditor KCInstance
        {
            get
            {
                _kcInstance ??= new KCGroupEditor();
                return _kcInstance;
            }
        }

        public static float oldEditorRange = 0f;
        public static IKCColonyBuildable selectedBuildable;
        protected override void GroupEditorWindow(int windowID)
        {
            toolRect.size = new Vector2(330, 350);

            UpdateVectors();

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                GUILayout.Button(Localizer.Format("#LOC_KC_COMMON_BRAND"), UIMain.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUILayout.Button(Localizer.Format("#LOC_KC_GROUPEDITOR_TITLE"), UIMain.DeadButton, GUILayout.Height(21));
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(1);
            GUILayout.Box(tHorizontalSep, UIMain.BoxNoBorder, GUILayout.Height(4));

            GUILayout.Space(2);

            GUILayout.BeginHorizontal();

            GUIStyle labelStyle = new(GUI.skin.label);
            labelStyle.fontSize *= 2;
            GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_FACILITY", selectedBuildable.DisplayName), labelStyle);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (!foldedIn)
                {
                    GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_INCREMENT"));
                    increment = float.Parse(GUILayout.TextField(increment.ToString(), 5, GUILayout.Width(48)));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("0.001", GUILayout.Height(18)))
                    {
                        increment = 0.001f;
                    }
                    if (GUILayout.Button("0.01", GUILayout.Height(18)))
                    {
                        increment = 0.01f;
                    }
                    if (GUILayout.Button("0.1", GUILayout.Height(18)))
                    {
                        increment = 0.1f;
                    }
                    if (GUILayout.Button("1", GUILayout.Height(18)))
                    {
                        increment = 1f;
                    }
                    if (GUILayout.Button("10", GUILayout.Height(18)))
                    {
                        increment = 10f;
                    }
                    if (GUILayout.Button("25", GUILayout.Height(16)))
                    {
                        increment = 25f;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                else
                {
                    GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_INCREMENT_SHORT"));
                    increment = float.Parse(GUILayout.TextField(increment.ToString(), 3, GUILayout.Width(25)));

                    if (GUILayout.Button("0.1", GUILayout.Height(23)))
                    {
                        increment = 0.1f;
                    }
                    if (GUILayout.Button("1", GUILayout.Height(23)))
                    {
                        increment = 1f;
                    }
                    if (GUILayout.Button("10", GUILayout.Height(23)))
                    {
                        increment = 10f;
                    }
                }
            }
            GUILayout.EndHorizontal();

            //
            // Set reference butons
            //
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_REFERENCE_SYSTEM"));
            GUILayout.FlexibleSpace();
            GUI.enabled = referenceSystem == Space.World;

            if (GUILayout.Button(new GUIContent(UIMain.iconCubes, Localizer.Format("#LOC_KC_GROUPEDITOR_MODEL")), GUILayout.Height(23), GUILayout.Width(23)))
            {
                referenceSystem = Space.Self;
                UpdateVectors();
            }

            GUI.enabled = referenceSystem == Space.Self;
            if (GUILayout.Button(new GUIContent(UIMain.iconWorld, Localizer.Format("#LOC_KC_GROUPEDITOR_WORLD")), GUILayout.Height(23), GUILayout.Width(23)))
            {
                referenceSystem = Space.World;
                UpdateVectors();
            }
            GUI.enabled = true;

            GUILayout.Label(Localizer.Format(referenceSystem == Space.Self ? "#LOC_KC_GROUPEDITOR_SELF" : "#LOC_KC_GROUPEDITOR_WORLD"));

            GUILayout.EndHorizontal();
            float fTempWidth = 80f;
            //
            // Position editing
            //
            GUILayout.BeginHorizontal();

            if (referenceSystem == Space.Self)
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_BACK_FORWARD"));
                GUILayout.FlexibleSpace();

                if (foldedIn)
                    fTempWidth = 40f;

                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21))) SetTransform(Vector3.back * increment);
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21))) SetTransform(Vector3.forward * increment);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_LEFT_RIGHT"));
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21))) SetTransform(Vector3.left * increment);
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21))) SetTransform(Vector3.right * increment);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

            }
            else
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_WEST_EAST"));
                GUILayout.FlexibleSpace();

                if (foldedIn)
                    fTempWidth = 40f;

                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21))) Setlatlng(0d, -increment);
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21))) Setlatlng(0d, increment);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_SOUTH_NORTH"));
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    Setlatlng(-increment, 0d);
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    Setlatlng(increment, 0d);
                }
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_LATITUDE"));
                GUILayout.FlexibleSpace();
                refLat = GUILayout.TextField(refLat, 10, GUILayout.Width(fTempWidth));

                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_LONGITUDE"));
                GUILayout.FlexibleSpace();
                refLng = GUILayout.TextField(refLng, 10, GUILayout.Width(fTempWidth));
            }
            GUILayout.EndHorizontal();

            // 
            // Altitude editing
            //
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_ALTITUDE"));
                GUILayout.FlexibleSpace();
                selectedGroup.RadiusOffset = float.Parse(GUILayout.TextField(selectedGroup.RadiusOffset.ToString(), 25, GUILayout.Width(fTempWidth)));
                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    selectedGroup.RadiusOffset -= increment;
                    ApplySettings();
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) | GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    selectedGroup.RadiusOffset += increment;
                    ApplySettings();
                }
            }
            GUILayout.EndHorizontal();


            GUI.enabled = true;

            GUILayout.Space(5);



            //
            // Rotation
            //
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_ROTATION"));
                GUILayout.FlexibleSpace();
                headingStr = GUILayout.TextField(headingStr, 9, GUILayout.Width(fTempWidth));

                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(23))) SetRotation(-increment);
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(23))) SetRotation(increment);
            }
            GUILayout.EndHorizontal();


            GUILayout.Space(1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_SEA_LEVEL"));
                GUILayout.FlexibleSpace();
                selectedGroup.SeaLevelAsReference = GUILayout.Toggle(selectedGroup.SeaLevelAsReference, "", GUILayout.Width(140), GUILayout.Height(23));
            }
            GUILayout.EndHorizontal();

            GUILayout.Box(tHorizontalSep, UIMain.BoxNoBorder, GUILayout.Height(4));



            GUILayout.Space(2);
            GUILayout.Space(5);



            GUI.enabled = true;



            GUI.enabled = true;
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = true;
                if (GUILayout.Button(Localizer.Format("#LOC_KC_GROUPEDITOR_SAVE"), GUILayout.Height(23)))
                {
                    selectedGroup.isInSavegame = true;
                    selectedGroup.childInstances.ForEach(instance => instance.isInSavegame = true);

                    KerbalKonstructs.API.OnGroupSaved.Invoke(selectedGroup);
                    Close();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.Space(1);
            GUILayout.Box(tHorizontalSep, UIMain.BoxNoBorder, GUILayout.Height(4));

            GUILayout.Space(2);

            if (GUI.tooltip != "")
            {
                var labelSize = GUI.skin.GetStyle("Label").CalcSize(new GUIContent(GUI.tooltip));
                GUI.Box(new Rect(Event.current.mousePosition.x - (25 + (labelSize.x / 2)), Event.current.mousePosition.y - 40, labelSize.x + 10, labelSize.y + 5), GUI.tooltip);
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public override void Open()
        {
            oldEditorRange = KerbalKonstructs.UI.GroupEditor.maxEditorRange;
            KC_CAB_Facility cab = selectedBuildable.Colony.CAB;
            KerbalKonstructs.UI.GroupEditor.maxEditorRange = cab.cabInfo.EditorRange[cab.level] * Configuration.EditorRangeMultiplier;
            selectedGroup.isInSavegame = true;
            foreach (var item in API.GetGroupStatics(selectedGroup.Group, selectedGroup.CelestialBody.name))
            {
                item.ToggleAllColliders(false);
                item.isInSavegame = true;
            }
            base.Open();
        }

        public override void Close()
        {
            KerbalKonstructs.UI.GroupEditor.maxEditorRange = oldEditorRange;
            base.Close();
        }
    }
}
