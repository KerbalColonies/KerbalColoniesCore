using UnityEngine;

namespace KerbalColonies.UI
{
    public abstract class KCWindowBase : KCWindow
    {
        protected GUIStyle LabelGreen;

        protected int windowID;
        protected string title;
        private bool guiInitialized;

        protected Rect toolRect = new Rect(100, 100, 330, 100);

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
            toolRect = GUI.Window(windowID, toolRect, KCWindow, "", UIConfig.KKWindow);
        }

        protected abstract void CustomWindow();

        void KCWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                GUILayout.Button("-KC-", UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUILayout.Button(title, UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUI.enabled = true;

                if (GUILayout.Button("X", UIConfig.DeadButtonRed, GUILayout.Height(21)))
                {
                    //KerbalKonstructs.KCInstance.saveObjects();
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(1);

            CustomWindow();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public KCWindowBase(int windowID, string title)
        {
            this.windowID = windowID;
            this.title = title;
        }
    }
}
