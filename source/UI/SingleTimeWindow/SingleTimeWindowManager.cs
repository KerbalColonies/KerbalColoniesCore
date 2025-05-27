using KerbalColonies.UI.SingleTimePopup;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using static KSP.UI.Screens.Settings.SettingsSetup;

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
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class SingleTimeWindowManager : MonoBehaviour
    {
        public static Dictionary<string, bool> shownWindows = new Dictionary<string, bool> { };

        public static List<KCSingleTimeWindowBase> windows = new List<KCSingleTimeWindowBase> { };

        protected void Awake()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\Configs\\SingleTimeWindows.cfg";

            ConfigNode node = ConfigNode.Load(path);

            if (node != null && node.GetNodes().Length > 0)
            {
                ConfigNode[] nodes = node.GetNodes();
                foreach (ConfigNode.Value value in nodes[0].values)
                {
                    if (bool.TryParse(value.value, out bool showAgain))
                    {
                        if (!shownWindows.ContainsKey(value.name))
                        {
                            shownWindows.Add(value.name, showAgain);
                        }
                    }
                }
            }
        }

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

        protected void OnDestroy()
        {
            ConfigNode node = new ConfigNode("SingleTimeWindows");
            windows.ForEach(w => node.AddValue(w.identifier, w.showAgain.ToString()));
                
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\Configs\\SingleTimeWindows.cfg";

            ConfigNode n = new ConfigNode();
            n.AddNode(node);
            n.Save(path);
        }
    }
}
