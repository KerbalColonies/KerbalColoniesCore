using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;

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
            if (!LoadedSaves.ContainsKey(HighLogic.CurrentGame.Seed.ToString())) return;

            string path = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Configs{Path.DirectorySeparatorChar}LegacySaves.cfg";
            ConfigNode node = ConfigNode.Load(path);

            string saveName = HighLogic.CurrentGame.Seed.ToString();

            if (node != null && node.GetNodes().Length > 0)
            {
                ConfigNode[] nodes = node.GetNodes();
                nodes[0].SetValue(saveName, LoadedSaves[saveName].ToString(), true);

                ConfigNode n = new ConfigNode();
                n.AddNode(nodes[0]);
                n.Save(path);
            }
            else
            {
                node = new ConfigNode("LegacySaves");
                node.AddValue(saveName, LoadedSaves[saveName]);

                ConfigNode n = new ConfigNode();
                n.AddNode(node);
                n.Save(path);
            }
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
