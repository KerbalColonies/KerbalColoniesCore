using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a colony system with Kerbal Konstructs statics
// Copyright (C) 2024 AMPW, Halengar

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

namespace KerbalColonies
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class SettingsLoader : MonoBehaviour
    {
        public void Awake()
        {
            // load settings when game start

        }
    }

    /// <summary>
    /// Reads and holds configuration parameters
    /// </summary>
    static class Configuration
    {
        public static int coloniesPerBody = 3;              // Limits the amount of colonies per celestial body (planet/moon)
                                                            // set it to zero to disable the limit
        public static int OreRequiredPerColony = 10000;     // The required amount of ore to start a colony
                                                            // It's planned to change this so different resources can be used

        internal const string APP_NAME = "KerbalColonies";
        public static bool enableLogging = true;            // Enable this only in debug purposes as it floods the logs very much
    }
}
