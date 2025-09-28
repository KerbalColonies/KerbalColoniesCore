using ClickThroughFix;
using KSP.UI.TooltipTypes;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.UI
{
    public class FacilityToolTip : KCWindow
    {
        protected int windowID;
        protected Rect toolRect = new Rect(100, 100, 250, 120);
        public readonly Vector2 offset = new Vector2(20, 20);
        public static string FacilityTitle { get; set; }
        public static string FacilityText { get; set; }

        private static FacilityToolTip instance;
        public static FacilityToolTip Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FacilityToolTip();
                }
                return instance;
            }
        }

        public override void Draw()
        {
            toolRect = ClickThruBlocker.GUIWindow(windowID, toolRect, KCWindow, "", UIConfig.KKWindow);
        }

        void KCWindow(int windowID)
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
