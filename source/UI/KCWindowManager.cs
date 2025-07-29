using System;
using System.Collections.Generic;
using System.Linq;
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

/// This file is a modified version of the WindowManager.cs file from the Kerbal Konstructs mod which is licensed under the MIT License.

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace KerbalColonies.UI
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal class KCWindowManager : MonoBehaviour
    {

        public static KCWindowManager instance = null;
        private static bool layoutInitialized = false;

        private Action draw;

        private List<KCWindow> openWindows = new List<KCWindow> { };

        /// <summary>
        /// First called before start. used settig up internal vaiabled
        /// </summary>
        public void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }
            instance = this;
            //DontDestroyOnLoad(KCInstance);
            draw = delegate { };
            openWindows = new List<KCWindow>();
        }

        #region Monobehavior functions
        /// <summary>
        /// Called after Awake. used for setting up references between objects and initializing windows.
        /// </summary>
        public void Start()
        {

        }

        /// <summary>
        /// Called every scene-switch. remove all external references here.
        /// </summary>
        public void OnDestroy()
        {
            openWindows.ToList().ForEach(w => w.Close());
        }

        /// <summary>
        /// Monobehaviour function for drawing. 
        /// </summary>
        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (!layoutInitialized)
            {
                UIConfig.SetStyles();
                layoutInitialized = true;
            }
            draw.Invoke();
        }
        #endregion


        #region public Functions

        /// <summary>
        /// Adds a function pointer to the list of drawn windows.
        /// </summary>
        /// <param name="drawfunct"></param>
        public static void OpenWindow(KCWindow drawfunct)
        {
            if (!IsOpen(drawfunct))
            {
                instance.openWindows.Add(drawfunct);
                instance.draw += drawfunct.Draw;
            }
        }

        /// <summary>
        /// Removes a function pointer from the list of open windows.
        /// </summary>
        /// <param name="drawfunct"></param>
        public static void CloseWindow(KCWindow drawfunct)
        {
            if (IsOpen(drawfunct))
            {
                instance.openWindows.Remove(drawfunct);
                instance.draw -= drawfunct.Draw;
            }
        }

        /// <summary>
        /// Opens a closed window or closes an open one.
        /// </summary>
        /// <param name="drawfunct"></param>
        public static void ToggleWindow(KCWindow drawfunct)
        {
            if (IsOpen(drawfunct))
            {
                CloseWindow(drawfunct);
            }
            else
            {
                OpenWindow(drawfunct);
            }

        }

        /// <summary>
        /// checks if a window is openend
        /// </summary>
        /// <param name="drawfunct"></param>
        /// <returns></returns>
        public static bool IsOpen(KCWindow drawfunct)
        {
            if (instance == null)
            {
                return false;
            }

            return instance.openWindows.Contains(drawfunct);
        }


        #endregion



    }
}
