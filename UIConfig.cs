using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies
{
    internal class UIConfig
    {
        internal static bool layoutIsInitialized = false;

        public static Texture2D tNormalButton;
        public static Texture2D tHoverButton;

        public static GUIStyle Yellowtext;
        public static GUIStyle TextAreaNoBorder;
        public static GUIStyle BoxNoBorder;
        public static GUIStyle BoxNoBorderW;
        public static GUIStyle ButtonKK;
        public static GUIStyle ButtonInactive;
        public static GUIStyle ButtonRed;
        public static GUIStyle DeadButton3;
        public static GUIStyle DeadButtonRed;
        public static GUIStyle KKToolTip;
        public static GUIStyle LabelWhite;
        public static GUIStyle LabelRed;
        public static GUIStyle DeadButton;
        public static GUIStyle LabelInfo;
        public static GUIStyle ButtonTextYellow;
        public static GUIStyle ButtonTextOrange;
        public static GUIStyle ButtonDefault;

        public static GUIStyle KKWindow;

        public static GUIStyle navStyle;

        public static void SetStyles()
        {
            navStyle = new GUIStyle();
            navStyle.padding.left = 0;
            navStyle.padding.right = 0;
            navStyle.padding.top = 1;
            navStyle.padding.bottom = 3;
            navStyle.normal.background = null;


            DeadButtonRed = new GUIStyle(GUI.skin.button);
            DeadButtonRed.normal.background = null;
            DeadButtonRed.hover.background = null;
            DeadButtonRed.active.background = null;
            DeadButtonRed.focused.background = null;
            DeadButtonRed.normal.textColor = Color.red;
            DeadButtonRed.hover.textColor = Color.yellow;
            DeadButtonRed.active.textColor = Color.red;
            DeadButtonRed.focused.textColor = Color.red;
            DeadButtonRed.fontSize = 12;
            DeadButtonRed.fontStyle = FontStyle.Bold;

            ButtonRed = new GUIStyle(GUI.skin.button);
            ButtonRed.normal.textColor = Color.red;
            ButtonRed.active.textColor = Color.red;
            ButtonRed.focused.textColor = Color.red;
            ButtonRed.hover.textColor = Color.red;

            ButtonKK = new GUIStyle(GUI.skin.button);
            ButtonKK.padding.left = 0;
            ButtonKK.padding.right = 0;
            ButtonKK.normal.background = tNormalButton;
            ButtonKK.hover.background = tHoverButton;

            ButtonInactive = new GUIStyle(GUI.skin.button);
            ButtonInactive.padding.left = 0;
            ButtonInactive.padding.right = 0;
            ButtonInactive.normal.background = tNormalButton;
            ButtonInactive.hover.background = tHoverButton;
            ButtonInactive.normal.textColor = XKCDColors.Grey;
            ButtonInactive.active.textColor = XKCDColors.Grey;
            ButtonInactive.focused.textColor = XKCDColors.Grey;
            ButtonInactive.hover.textColor = XKCDColors.Grey;

            Yellowtext = new GUIStyle(GUI.skin.box);
            Yellowtext.normal.textColor = Color.yellow;
            Yellowtext.normal.background = null;

            TextAreaNoBorder = new GUIStyle(GUI.skin.textArea);
            TextAreaNoBorder.normal.background = null;

            BoxNoBorder = new GUIStyle(GUI.skin.box);
            BoxNoBorder.normal.background = null;

            BoxNoBorderW = new GUIStyle(GUI.skin.box);
            BoxNoBorderW.normal.background = null;
            BoxNoBorderW.normal.textColor = Color.white;

            KKToolTip = new GUIStyle(GUI.skin.box);
            KKToolTip.normal.textColor = Color.white;
            KKToolTip.fontSize = 11;
            KKToolTip.fontStyle = FontStyle.Normal;

            LabelWhite = new GUIStyle(GUI.skin.label);
            LabelWhite.normal.textColor = Color.white;
            LabelWhite.fontSize = 13;
            LabelWhite.fontStyle = FontStyle.Normal;
            LabelWhite.padding.bottom = 1;
            LabelWhite.padding.top = 1;

            LabelRed = new GUIStyle(GUI.skin.label);
            LabelRed.normal.textColor = XKCDColors.TomatoRed;
            LabelRed.fontSize = 13;
            LabelRed.fontStyle = FontStyle.Normal;
            LabelRed.padding.bottom = 1;
            LabelRed.padding.top = 1;

            LabelInfo = new GUIStyle(GUI.skin.label);
            LabelInfo.normal.background = null;
            LabelInfo.normal.textColor = Color.white;
            LabelInfo.fontSize = 13;
            LabelInfo.fontStyle = FontStyle.Bold;
            LabelInfo.padding.left = 3;
            LabelInfo.padding.top = 0;
            LabelInfo.padding.bottom = 0;

            DeadButton = new GUIStyle(GUI.skin.button);
            DeadButton.normal.background = null;
            DeadButton.hover.background = null;
            DeadButton.active.background = null;
            DeadButton.focused.background = null;
            DeadButton.normal.textColor = Color.white;
            DeadButton.hover.textColor = Color.white;
            DeadButton.active.textColor = Color.white;
            DeadButton.focused.textColor = Color.white;
            DeadButton.fontSize = 14;
            DeadButton.fontStyle = FontStyle.Bold;

            DeadButton3 = new GUIStyle(GUI.skin.button);
            DeadButton3.normal.background = null;
            DeadButton3.hover.background = null;
            DeadButton3.active.background = null;
            DeadButton3.focused.background = null;
            DeadButton3.normal.textColor = Color.white;
            DeadButton3.hover.textColor = Color.white;
            DeadButton3.active.textColor = Color.white;
            DeadButton3.focused.textColor = Color.white;
            DeadButton3.fontSize = 13;
            DeadButton3.fontStyle = FontStyle.Bold;



            KKWindow = new GUIStyle(GUI.skin.window);
            KKWindow.padding = new RectOffset(8, 8, 3, 3);

            ButtonTextYellow = new GUIStyle(GUI.skin.button);
            ButtonTextYellow.normal.textColor = XKCDColors.YellowGreen;
            ButtonTextYellow.active.textColor = XKCDColors.YellowGreen;
            ButtonTextYellow.focused.textColor = XKCDColors.YellowGreen;
            ButtonTextYellow.hover.textColor = XKCDColors.YellowGreen;

            ButtonTextOrange = new GUIStyle(GUI.skin.button);
            ButtonTextOrange.normal.textColor = XKCDColors.PumpkinOrange;
            ButtonTextOrange.active.textColor = XKCDColors.PumpkinOrange;
            ButtonTextOrange.focused.textColor = XKCDColors.PumpkinOrange;
            ButtonTextOrange.hover.textColor = XKCDColors.PumpkinOrange;

            ButtonDefault = new GUIStyle(GUI.skin.button);

        }
    }
}
