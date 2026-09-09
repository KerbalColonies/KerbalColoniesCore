using KerbalKonstructs.Core;
using KerbalKonstructs.UI;
using KSP.Localization;
using System;
using System.Reflection;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This file is based on the Kerbal Konstructs mod EditorGUI.

// Kerbal Konstructs Plugin (when not stated otherwise in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace KerbalColonies.UI
{
    public class KCInstanceEditor : EditorGUI
    {
        private static KCInstanceEditor instance;
        public static KCInstanceEditor Instance => instance ??= new KCInstanceEditor();

        private Action<StaticInstance> onSave;
        private Action<StaticInstance> onCancel;
        private bool collidersEnabled;

        public void Open(StaticInstance staticInstance, Action<StaticInstance> saveCallback, Action<StaticInstance> cancelCallback)
        {
            selectedInstance = null;
            onSave = saveCallback;
            onCancel = cancelCallback;
            collidersEnabled = false;
            toolRect = new Rect(1200, 60, 390, 700);
            base.Open();
        }

        public override void InstanceEditorWindow(int windowId)
        {
            GUILayout.Label(Localizer.Format("#LOC_KC_INSTANCEEDITOR_TITLE"));
            GUILayout.Label(Localizer.Format("#LOC_KC_INSTANCEEDITOR_MODEL", KerbalKonstructs.API.GetModelTitel(selectedInstance.UUID)));
            DrawIncrement();
            DrawPosition();
            DrawRotation();
            DrawScale();
            DrawOptions();
            DrawActions();
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        private void DrawIncrement()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_INCREMENT"));
            if (float.TryParse(GUILayout.TextField(increment.ToString(), 8, GUILayout.Width(70)), out float value)) increment = value;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            foreach (float preset in new[] { 0.001f, 0.01f, 0.1f, 1f, 10f, 25f })
                if (GUILayout.Button(preset.ToString("0.###"))) increment = preset;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KC_GROUPEDITOR_REFERENCE_SYSTEM"));
            if (GUILayout.Button(Localizer.Format("#LOC_KC_GROUPEDITOR_MODEL"))) referenceSystem = Reference.Model;
            if (GUILayout.Button(Localizer.Format("#LOC_KC_GROUPEDITOR_WORLD"))) referenceSystem = Reference.Center;
            GUILayout.EndHorizontal();
        }

        private void DrawPosition()
        {
            GUILayout.Space(5);
            GUILayout.Label(Localizer.Format("#LOC_KC_INSTANCEEDITOR_POSITION"));
            DrawVectorField(ref posXStr, Localizer.Format("#LOC_KC_INSTANCEEDITOR_LEFT_RIGHT"), Vector3.left, Vector3.right, true);
            DrawVectorField(ref posYStr, Localizer.Format("#LOC_KC_INSTANCEEDITOR_DOWN_UP"), Vector3.down, Vector3.up, true);
            DrawVectorField(ref posZStr, Localizer.Format("#LOC_KC_INSTANCEEDITOR_BACK_FORWARD"), Vector3.back, Vector3.forward, true);

            if (GUILayout.Button(Localizer.Format("#LOC_KC_INSTANCEEDITOR_APPLY_POSITION"))) ApplyInputStrings();
            if (GUILayout.Button(Localizer.Format("#LOC_KC_INSTANCEEDITOR_SNAP_TERRAIN")))
            {
                selectedInstance.gameObject.transform.position = selectedInstance.CelestialBody.GetWorldSurfacePosition(
                    selectedInstance.RefLatitude,
                    selectedInstance.RefLongitude,
                    GetSurfaceHeight(selectedInstance));
                ApplySettings();
            }
        }

        private void DrawRotation()
        {
            GUILayout.Space(5);
            GUILayout.Label(Localizer.Format("#LOC_KC_INSTANCEEDITOR_ROTATION"));
            DrawVectorField(ref oriXStr, "X", Vector3.right, Vector3.right, false);
            DrawVectorField(ref oriYStr, "Y", Vector3.up, Vector3.up, false);
            DrawVectorField(ref oriZStr, "Z", Vector3.forward, Vector3.forward, false);
            if (GUILayout.Button(Localizer.Format("#LOC_KC_INSTANCEEDITOR_APPLY_ROTATION"))) ApplyInputStrings();
        }

        private void DrawVectorField(ref string text, string label, Vector3 negativeAxis, Vector3 positiveAxis, bool position)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(95));
            text = GUILayout.TextField(text, 12, GUILayout.Width(85));
            if (GUILayout.RepeatButton("-", GUILayout.Width(35)))
            {
                if (position) SetTransform(negativeAxis * increment);
                else SetRotation(negativeAxis, -increment);
            }
            if (GUILayout.RepeatButton("+", GUILayout.Width(35)))
            {
                if (position) SetTransform(positiveAxis * increment);
                else SetRotation(positiveAxis, increment);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawScale()
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KC_INSTANCEEDITOR_SCALE"), GUILayout.Width(95));
            float originalScale = selectedInstance.ModelScale;
            if (float.TryParse(GUILayout.TextField(originalScale.ToString(), 8, GUILayout.Width(85)), out float scale))
                selectedInstance.ModelScale = Math.Max(0.01f, scale);
            if (GUILayout.RepeatButton("-", GUILayout.Width(35))) selectedInstance.ModelScale = Math.Max(0.01f, selectedInstance.ModelScale - increment);
            if (GUILayout.RepeatButton("+", GUILayout.Width(35))) selectedInstance.ModelScale += increment;
            GUILayout.EndHorizontal();
            if (selectedInstance.ModelScale != originalScale) ApplySettings();
        }

        private void DrawOptions()
        {
            GUILayout.Space(5);
            bool newColliderState = GUILayout.Toggle(collidersEnabled, Localizer.Format("#LOC_KC_INSTANCEEDITOR_COLLIDERS"));
            if (newColliderState != collidersEnabled)
            {
                collidersEnabled = newColliderState;
                selectedInstance.ToggleAllColliders(collidersEnabled);
            }

            if (HasVariants(selectedInstance) && GUILayout.Button(Localizer.Format("#LOC_KC_INSTANCEEDITOR_VARIANT", selectedInstance.VariantName)))
                OpenVariantSelector();
        }

        private static bool HasVariants(StaticInstance staticInstance)
        {
            FieldInfo modelField = typeof(StaticInstance).GetField("model", BindingFlags.Instance | BindingFlags.NonPublic);
            object model = modelField?.GetValue(staticInstance);
            FieldInfo variantsField = model?.GetType().GetField("hasVariants", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (variantsField?.GetValue(model) is bool hasVariants) return hasVariants;
            PropertyInfo variantsProperty = model?.GetType().GetProperty("hasVariants", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return variantsProperty?.GetValue(model, null) is bool hasVariantProperty && hasVariantProperty;
        }

        private static double GetSurfaceHeight(StaticInstance staticInstance)
        {
            PropertyInfo surfaceHeight = typeof(StaticInstance).GetProperty("surfaceHeight", BindingFlags.Instance | BindingFlags.NonPublic);
            return surfaceHeight?.GetValue(staticInstance, null) is double height ? height : 0;
        }

        private static void OpenVariantSelector()
        {
            Type selectorType = typeof(EditorGUI).Assembly.GetType("KerbalKonstructs.UI.VariantSelector");
            selectorType?.GetField("staticInstance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, selectedInstance);
            selectorType?.GetMethod("Open", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
        }

        private void DrawActions()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#LOC_KC_GROUPEDITOR_SAVE")))
            {
                StaticInstance savedInstance = selectedInstance;
                Action<StaticInstance> saveCallback = onSave;
                savedInstance.ToggleAllColliders(true);
                savedInstance.isInSavegame = true;
                savedInstance.SaveConfig();
                Close();
                saveCallback?.Invoke(savedInstance);
            }
            if (GUILayout.Button(Localizer.Format("#LOC_KC_COMMON_CANCEL")))
            {
                StaticInstance canceledInstance = selectedInstance;
                Action<StaticInstance> cancelCallback = onCancel;
                Close();
                cancelCallback?.Invoke(canceledInstance);
            }
            GUILayout.EndHorizontal();
        }

        public override void Close()
        {
            onSave = null;
            onCancel = null;
            KerbalKonstructs.KerbalKonstructs.DeselectObject(true, true);
            base.Close();
        }
    }
}
