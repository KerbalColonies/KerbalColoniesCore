using KerbalColonies.Settings;
using KSP.Localization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace KerbalColonies.UI
{
    public class KCLegacySaveWarning : KCWindowBase
    {
        private static KCLegacySaveWarning instance;
        public static KCLegacySaveWarning Instance
        {
            get
            {
                instance ??= new KCLegacySaveWarning();
                return instance;
            }
        }

        protected override void CustomWindow()
        {
            GUILayout.Label(Localizer.Format("#LOC_KC_LEGACYSAVE_TITLE"));
            GUILayout.Label(Localizer.Format("#LOC_KC_LEGACYSAVE_CREATED_OLDER"));
            GUILayout.Label(Localizer.Format("#LOC_KC_LEGACYSAVE_INCOMPATIBLE"));
            GUILayout.Label(Localizer.Format("#LOC_KC_LEGACYSAVE_KEEP_EXPLANATION"));
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(Localizer.Format("#LOC_KC_LEGACYSAVE_KEEP"), GUILayout.Width(190)))
                {
                    LoadedSaves.TryAdd(HighLogic.CurrentGame.Seed.ToString(), false);
                    Close();
                }
                if (GUILayout.Button(Localizer.Format("#LOC_KC_LEGACYSAVE_DELETE"), GUILayout.Width(190)))
                {
                    LoadedSaves.Remove(HighLogic.CurrentGame.Seed.ToString());
                    Close();
                }
            }
            GUILayout.EndHorizontal();
        }

        public static Dictionary<string, bool> LoadedSaves { get; private set; } = [];
        public static void SaveSettings()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}LegacySaves.cfg";
            ConfigNode node = new("LegacySaves");

            LoadedSaves.ToList().ForEach(kvp => node.AddValue(kvp.Key, kvp.Value));

            ConfigNode n = new();
            n.AddNode(node);
            n.Save(path);
        }

        public static void LoadSettings()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}LegacySaves.cfg";
            ConfigNode node = ConfigNode.Load(path);

            if (node != null && node.GetNodes().Length > 0)
            {
                ConfigNode[] nodes = node.GetNodes();
                foreach (ConfigNode.Value value in nodes[0].values)
                {
                    LoadedSaves.TryAdd(value.name, false);
                }
            }
        }

        public KCLegacySaveWarning() : base(Configuration.createWindowID(), Localizer.Format("#LOC_KC_LEGACYSAVE_WINDOW_TITLE"), false)
        {
            toolRect = new Rect(100, 100, 400, 240);
        }
    }
}
