using KerbalKonstructs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KerbalKonstructs;
using KerbalKonstructs.UI;
using KodeUI;
using KerbalColonies.colonyFacilities;

namespace KerbalColonies
{
    public class  KCGroupEditor : GroupEditor
    {
        protected static KCGroupEditor _instance = null;
        public static KCGroupEditor instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new KCGroupEditor();

                }
                return _instance;
            }
        }

        public static KCFacilityBase selectedFacility;

        protected override void GroupEditorWindow(int windowID)
        {
            UpdateVectors();

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                GUILayout.Button("KC", UIMain.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUILayout.Button("Custom Group Editor", UIMain.DeadButton, GUILayout.Height(21));
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(1);
            GUILayout.Box(tHorizontalSep, UIMain.BoxNoBorder, GUILayout.Height(4));

            GUILayout.Space(2);

            GUILayout.BeginHorizontal();

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize *= 2;
            GUILayout.Label($"Facility: {selectedFacility.displayName}", labelStyle);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (!foldedIn)
                {
                    GUILayout.Label("Increment");
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
                    GUILayout.Label("i");
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
            GUILayout.Label("Reference System: ");
            GUILayout.FlexibleSpace();
            GUI.enabled = (referenceSystem == Space.World);

            if (GUILayout.Button(new GUIContent(UIMain.iconCubes, "Model"), GUILayout.Height(23), GUILayout.Width(23)))
            {
                referenceSystem = Space.Self;
                UpdateVectors();
            }

            GUI.enabled = (referenceSystem == Space.Self);
            if (GUILayout.Button(new GUIContent(UIMain.iconWorld, "World"), GUILayout.Height(23), GUILayout.Width(23)))
            {
                referenceSystem = Space.World;
                UpdateVectors();
            }
            GUI.enabled = true;

            GUILayout.Label(referenceSystem.ToString());

            GUILayout.EndHorizontal();
            float fTempWidth = 80f;
            //
            // Position editing
            //
            GUILayout.BeginHorizontal();

            if (referenceSystem == Space.Self)
            {
                GUILayout.Label("Back / Forward:");
                GUILayout.FlexibleSpace();

                if (foldedIn)
                    fTempWidth = 40f;

                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    SetTransform(Vector3.back * increment);
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    SetTransform(Vector3.forward * increment);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Left / Right:");
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    SetTransform(Vector3.left * increment);
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    SetTransform(Vector3.right * increment);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

            }
            else
            {
                GUILayout.Label("West / East :");
                GUILayout.FlexibleSpace();

                if (foldedIn)
                    fTempWidth = 40f;

                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    Setlatlng(0d, -increment);
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    Setlatlng(0d, increment);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("South / North:");
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    Setlatlng(-increment, 0d);
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    Setlatlng(increment, 0d);
                }
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Lat: ");
                GUILayout.FlexibleSpace();
                refLat = GUILayout.TextField(refLat, 10, GUILayout.Width(fTempWidth));

                GUILayout.Label("  Lng: ");
                GUILayout.FlexibleSpace();
                refLng = GUILayout.TextField(refLng, 10, GUILayout.Width(fTempWidth));
            }
            GUILayout.EndHorizontal();

            // 
            // Altitude editing
            //
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Alt.");
                GUILayout.FlexibleSpace();
                selectedGroup.RadiusOffset = float.Parse(GUILayout.TextField(selectedGroup.RadiusOffset.ToString(), 25, GUILayout.Width(fTempWidth)));
                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(21)))
                {
                    selectedGroup.RadiusOffset -= increment;
                    ApplySettings();
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(21)) || GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(21)))
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
                GUILayout.Label("Rotation:");
                GUILayout.FlexibleSpace();
                headingStr = GUILayout.TextField(headingStr, 9, GUILayout.Width(fTempWidth));

                if (GUILayout.RepeatButton("<<", GUILayout.Width(30), GUILayout.Height(23)))
                {
                    SetRotation(-increment);
                }
                if (GUILayout.Button("<", GUILayout.Width(30), GUILayout.Height(23)))
                {
                    SetRotation(-increment);
                }
                if (GUILayout.Button(">", GUILayout.Width(30), GUILayout.Height(23)))
                {
                    SetRotation(increment);
                }
                if (GUILayout.RepeatButton(">>", GUILayout.Width(30), GUILayout.Height(23)))
                {
                    SetRotation(increment);
                }
            }
            GUILayout.EndHorizontal();


            GUILayout.Space(1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("SeaLevel as Reference:");
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
                if (GUILayout.Button("Save", GUILayout.Height(23)))
                {
                    selectedGroup.Save();
                    this.Close();
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
    }
}
