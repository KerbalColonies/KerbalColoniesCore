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
                instance = instance ?? new KCLegacySaveWarning();
                return instance;
            }
        }

        protected override void CustomWindow()
        {
            GUILayout.Label("<b>Legacy Save Detected</b>");
            GUILayout.Label("This save was created with an older version of Kerbal Colonies.");
            GUILayout.Label("Because of large changes like the electricity system old saves are not compatible with the current version.");
            GUILayout.Label("You can keep the colonies from this save but they won't be loaded in the current version of KC.");
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Keep Colonies", GUILayout.Width(190)))
                {
                    LoadedSaves.TryAdd(HighLogic.CurrentGame.Seed.ToString(), false);
                    this.Close();
                }
                if (GUILayout.Button("Delete Colonies", GUILayout.Width(190)))
                {
                    LoadedSaves.Remove(HighLogic.CurrentGame.Seed.ToString());
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();
        }

        public static Dictionary<string, bool> LoadedSaves { get; private set; } = new Dictionary<string, bool>();
        public static void SaveSettings()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}LegacySaves.cfg";
            ConfigNode node = new ConfigNode("LegacySaves");

            LoadedSaves.ToList().ForEach(kvp => node.AddValue(kvp.Key, kvp.Value));

            ConfigNode n = new ConfigNode();
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

        public KCLegacySaveWarning() : base(Configuration.createWindowID(), "<b>Legacy Save Warning</b>", false)
        {
            this.toolRect = new Rect(100, 100, 400, 240);
        }
    }
}
