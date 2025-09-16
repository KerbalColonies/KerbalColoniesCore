using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.Screens.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalColonies
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KCTechTreeHandler : MonoBehaviour
    {
        public class KCTechPartInfo : IComparable<KCTechPartInfo>
        {
            public AvailablePart part = null;

            public List<string> techNodes = new List<string>();
            public string partName = string.Empty;
            public string description = string.Empty;
            public string manufacturer = string.Empty;
            public Sprite icon = null;
            public KCFacilityInfoClass facility = null;
            public int level = 0;

            public int CompareTo(KCTechPartInfo other)
            {
                if (other == null) return 1;
                else if (this.partName == other.partName) return this.level.CompareTo(other.level);
                else return this.partName.CompareTo(other.partName);
            }

            public KCTechPartInfo(KCFacilityInfoClass facility, int level)
            {
                this.facility = facility;
                this.level = level;
                this.partName = facility.PartName[level];
                this.description = facility.Description[level];
                this.manufacturer = facility.Manufacturer[level];
                this.techNodes = facility.TechNodesRequired[level];

                byte[] fileData = File.ReadAllBytes(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", facility.IconPath[level]));

                // Create new texture and load image
                Texture2D tex = new Texture2D(2, 2);
                if (!tex.LoadImage(fileData))  // LoadImage auto-resizes texture
                {
                    Debug.LogError("Failed to load image from: " + facility.IconPath[level]);
                    return;
                }

                // Create sprite from texture
                icon = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }


        public static List<KCTechPartInfo> TechParts = new List<KCTechPartInfo>();

        public static bool ContainsFacility(KCFacilityInfoClass facility, int level) => TechParts.Any(tp => tp.facility == facility && tp.level == level);
        public static KCTechPartInfo ContainsFacilityName(string partName) => TechParts.FirstOrDefault(tp => tp.partName == partName);


        public static void AddFacility(KCFacilityInfoClass facility, int level)
        {
            if (!ContainsFacility(facility, level))
            {
                TechParts.Add(new KCTechPartInfo(facility, level));
            }
        }

        public static void RemoveFacility(KCFacilityInfoClass facility, int level)
        {
            TechParts.ToList().ForEach(tp =>
            {
                if (tp.facility == facility && tp.level == level)
                {
                    TechParts.Remove(tp);
                }
            });
        }

        public static void RemoveFacility(KCFacilityInfoClass facility)
        {
            TechParts.ToList().ForEach(tp =>
            {
                if (tp.facility == facility)
                {
                    TechParts.Remove(tp);
                }
            });
        }

        public static void ClearFacilities() => TechParts.Clear();


        public static bool CanBuild(KCFacilityInfoClass facility, int level)
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;

            foreach (KCTechPartInfo tp in TechParts)
            {
                if (tp.facility == facility && tp.level <= level)
                {
                    foreach (string tech in tp.techNodes)
                    {
                        if (ResearchAndDevelopment.GetTechnologyState(tech) != RDTech.State.Available) return false;
                    }
                }
            }

            return true;
        }


        public void Start()
        {
            RDTechTree.OnTechTreeSpawn.Add(TechTreeSpawn);
            RDNode.OnNodeSelected.Add(TechNodeChange);
        }

        public void onDestroy()
        {
            RDTechTree.OnTechTreeSpawn.Remove(TechTreeSpawn);
            RDNode.OnNodeSelected.Remove(TechNodeChange);
        }


        private static bool techTreeModified = true;
        private static bool techTreeModifiedLateUpdate = true;
        private static string currentToolTipPart = string.Empty;
        private static Sprite defaultBackground = null;
        private static GameObject toolTip = null;

        public void TechTreeSpawn(RDTechTree tree) => techTreeModified = false;
        public void TechNodeChange(RDNode node) => techTreeModifiedLateUpdate = false;

        public void Update()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return;

            if (!techTreeModified)
            {
                RDTech[] RDTechs = FindObjectsOfType<RDTech>();
                if (RDTechs.Length > 0)
                {
                    techTreeModified = true;
                    Configuration.writeDebug($"Modifing RDTech");

                    AvailablePart basePart = null;

                    foreach (RDTech tech in RDTechs)
                    {
                        if (basePart == null && tech.partsAssigned.Count > 0) basePart = tech.partsAssigned[0];

                        TechParts.Where(tp => tp.techNodes.Contains(tech.techID)).ToList().ForEach(tp =>
                        {
                            if (tp.part == null)
                            {
                                tp.part = new AvailablePart();
                                tp.part.name = "kerbalColoniesFakePart";
                                tp.part.title = tp.partName;
                                tp.part.manufacturer = tp.manufacturer;
                                tp.part.description = tp.description;
                                tp.part.author = "AMPW";

                                tp.part.iconPrefab = basePart.iconPrefab;
                                tp.part.iconScale = basePart.iconScale;
                                tp.part.partPrefab = basePart.partPrefab;
                            }

                            tech.partsAssigned.Add(tp.part);
                        });
                    }
                }
            }

            if (!techTreeModifiedLateUpdate)
            {
                List<RDPartListItem> partList = FindObjectsOfType<RDPartListItem>().ToList();

                if (partList.Count > 0)
                {
                    techTreeModifiedLateUpdate = true;

                    foreach (RDPartListItem item in partList)
                    {
                        if (item == null) continue;
                        if (item.gameObject == null) continue;

                        if (item.AvailPart == null) continue;
                        if (string.IsNullOrEmpty(item.AvailPart.name)) continue;

                        KCTechPartInfo techPart = ContainsFacilityName(item.AvailPart.title);

                        if (techPart == null) continue;

                        foreach (Transform child in item.gameObject.transform)
                        {
                            if (child.name.Contains("Image"))
                            {
                                child.gameObject.SetActive(true);

                                Image img = child.gameObject.GetComponent<Image>();

                                if (img == null) continue;
                                else
                                {
                                    img.sprite = techPart.icon;
                                }
                            }
                            else if (child.name.Contains("icon"))
                            {
                                child.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }


            if (toolTip == null )
            {
                foreach (Transform item in UIMasterController.Instance.gameObject.transform)
                {
                    if (item.name.Contains("TooltipCanvas"))
                    {
                        foreach (Transform child in item.gameObject.transform)
                            if (child.name.Contains("PartListTooltip"))
                            {
                                toolTip = child.gameObject;
                                break;
                            }
                        break;
                    }
                }
            }
            else
            {
                GameObject standard = toolTip.GetChild("StandardInfo");

                GameObject partName = standard.GetChild("PartName").GetChild("PartNameField");
                TextMeshProUGUI partNameMesh = partName.GetComponent<TextMeshProUGUI>();

                if (currentToolTipPart != partNameMesh.text)
                {
                    currentToolTipPart = partNameMesh.text;

                    GameObject ThumbPrimary = standard.GetChild("ThumbAndPrimaryInfo");
                    GameObject container = ThumbPrimary.GetChild("ThumbContainer");
                    GameObject info = ThumbPrimary.GetChild("Scroll View");

                    Image img = container.GetComponent<Image>();

                    KCTechPartInfo techPart = ContainsFacilityName(partNameMesh.text);

                    if (techPart != null)
                    {
                        if (defaultBackground == null) defaultBackground = img.sprite;

                        img.sprite = techPart.icon;

                        container.GetChild("ThumbMask").SetActive(false);
                        info.SetActive(false);
                    }
                    else
                    {
                        container.GetChild("ThumbMask").SetActive(true);
                        info.SetActive(true);

                        if (img != null && defaultBackground != null) img.sprite = defaultBackground;
                    }
                }
            }
        }
    }
}
