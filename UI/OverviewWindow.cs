using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KerbalColonies;

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
                GUILayout.Label(colony.Name);
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
