using KerbalColonies.UI.SingleTimePopup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.UI
{
    public class Changelogwindow : KCSingleTimeWindowBase
    {
        string ParseMarkdownToGUI(string markdown)
        {
            if (markdown.StartsWith("# "))
            {
                return $"<size=20><b>{markdown.Substring(2).Trim()}</b></size>";
            }
            else if (markdown.StartsWith("- "))
            {
                return $"• {markdown.Substring(2).Trim()}";
            }
            else
            {
                return markdown.Trim();
            }
        }

        public class MarkdownSegment
        {
            public enum SegmentType { Text, Link }
            public SegmentType Type;
            public string Content;
            public string Url; // Only set if Type == Link
        }

        List<MarkdownSegment> ParseMarkdownLine(string line)
        {
            var regex = new Regex(@"\[(.+?)\]\((.+?)\)");
            var matches = regex.Matches(line);

            List<MarkdownSegment> segments = new List<MarkdownSegment>();
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                // Add text before the match
                if (match.Index > lastIndex)
                {
                    string before = line.Substring(lastIndex, match.Index - lastIndex);
                    segments.Add(new MarkdownSegment { Type = MarkdownSegment.SegmentType.Text, Content = before });
                }

                // Add the matched link
                segments.Add(new MarkdownSegment
                {
                    Type = MarkdownSegment.SegmentType.Link,
                    Content = match.Groups[1].Value,
                    Url = match.Groups[2].Value
                });

                lastIndex = match.Index + match.Length;
            }

            // Add any text after the last match
            if (lastIndex < line.Length)
            {
                string after = line.Substring(lastIndex);
                segments.Add(new MarkdownSegment { Type = MarkdownSegment.SegmentType.Text, Content = after });
            }

            return segments;
        }

        List<string> changelogText;

        Vector2 scrollpos;
        protected override void CustomWindow()
        {
            GUIStyle linkButton = new GUIStyle(GUI.skin.button);
            linkButton.normal.background = null;
            linkButton.hover.background = null;
            linkButton.active.background = null;
            linkButton.normal.textColor = Color.cyan;
            linkButton.hover.textColor = new Color(0.3f, 0.5f, 1f); // optional hover color
            linkButton.active.textColor = Color.cyan;
            linkButton.alignment = TextAnchor.UpperCenter;

            scrollpos = GUILayout.BeginScrollView(scrollpos);
            changelogText.ForEach(line =>
            {
                GUILayout.BeginHorizontal();
                ParseMarkdownLine(line).ForEach(segment =>
                {
                    if (segment.Type == MarkdownSegment.SegmentType.Text)
                    {
                        GUILayout.Label(segment.Content);
                    }
                    else if (segment.Type == MarkdownSegment.SegmentType.Link)
                    {
                        if (GUILayout.Button(segment.Content, linkButton))
                        {
                            Application.OpenURL(segment.Url);
                        }
                    }
                });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            });
            GUILayout.EndScrollView();
        }

        protected override void OnClose()
        {
            ConfigNode node = new ConfigNode("ShowKCChangelog");
            node.AddValue("ShowKCChangelog", "false");

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\Configs\\ShowChangelog.cfg";

            ConfigNode n = new ConfigNode();
            n.AddNode(node);
            n.Save(path);
        }

        public Changelogwindow() : base("KC Changlelog", "kc_changelog", true, false, false, false, false)
        {


            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\Configs\\ShowChangelog.cfg";

            ConfigNode node = ConfigNode.Load(path);

            if (node != null && node.GetNodes().Length > 0)
            {
                ConfigNode[] nodes = node.GetNodes();
                bool.TryParse(nodes[0].GetValue("ShowKCChangelog"), out bool showChangelog);
                if (showChangelog) showAgain = true;
            }

            if (showAgain)
            {
                try
                {
                    changelogText = new List<string>();
                    File.ReadAllLines(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\KCChangelog.md").ToList().ForEach(l => changelogText.Add(ParseMarkdownToGUI(l)));

                    toolRect = new Rect(Screen.width / 2 - 400, Screen.height / 2 - 500, 800, 1000);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Kerbal Colonies: Error loading changelog file: {e.Message}");
                    changelogText = new List<string> {"Error loading changelog. Please check the log for details." };
                }
            }
        }
    }
}
