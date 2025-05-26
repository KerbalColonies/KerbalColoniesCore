using KerbalColonies.UI.SingleTimePopup;
using System.Collections.Generic;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.UI.SingleTimeWindow
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class SingleTimeWindowManager : MonoBehaviour
    {
        List<KCSingleTimeWindowBase> windows = new List<KCSingleTimeWindowBase> { };

        protected void Start()
        {
            foreach (KCSingleTimeWindowBase item in windows)
            {
                if (!item.showAgain) continue;

                if (
                    HighLogic.LoadedScene == GameScenes.MAINMENU && item.Mainmenu ||
                    HighLogic.LoadedScene == GameScenes.SPACECENTER && item.KSC ||
                    HighLogic.LoadedScene == GameScenes.EDITOR && item.Editor ||
                    HighLogic.LoadedScene == GameScenes.FLIGHT && item.Flight ||
                    HighLogic.LoadedScene == GameScenes.TRACKSTATION && item.Trackingstation
                    )
                {
                    item.Open();
                }
            }
        }
    }
}
