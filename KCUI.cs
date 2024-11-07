using KerbalKonstructs.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies
{
    internal class KCUI : KCWindow
    {
        private static KCUI _instance = null;
        public static KCUI instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new KCUI();

                }
                return _instance;
            }
        }

        internal Rect toolRect = new Rect(300, 35, 330, 350);

        public override void Draw()
        {
            drawEditor();
        }


        internal void drawEditor()
        {
            toolRect = GUI.Window(0xB07B1E3, toolRect, TestWindow, "", UIConfig.KKWindow);
        }

        void TestWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                GUILayout.Button("-KK-", UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUILayout.Button("Group Editor", UIConfig.DeadButton, GUILayout.Height(21));

                GUILayout.FlexibleSpace();

                GUI.enabled = true;

                if (GUILayout.Button("X", UIMain.DeadButtonRed, GUILayout.Height(21)))
                {
                    //KerbalKonstructs.instance.saveObjects();
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

    }
}
