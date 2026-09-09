using KerbalColonies.colonyFacilities.ProductionFacility;
using KerbalColonies.Settings;
using KerbalColonies.UI;
using KSP.Localization;
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

namespace KerbalColonies.colonyFacilities.CabFacility
{
    public class KC_CAB_Window : KCWindowBase
    {
        public static Action<colonyClass> CABInfoWindow;
        public static int CABInfoWidth = 590;

        private KC_CAB_Facility CABFacility;

        private string selectedType;
        private Vector2 scrollPosTypes = new();
        private Vector2 scrollPosFacilities = new();
        private Vector2 scrollPosOverview = new();
        protected override void CustomWindow()
        {
            CABFacility.Colony.UpdateColony();
            bool playerInColony = CABFacility.PlayerInColony;

            SortedDictionary<string, List<KCFacilityBase>> facilitiesByType = [];

            void addType(KCFacilityBase facility)
            {
                string category = facility.facilityInfo.category;
                if (!facilitiesByType.ContainsKey(category)) facilitiesByType.Add(category, [facility]);
                else if (!facilitiesByType[category].Contains(facility)) facilitiesByType[category].Add(facility);
            }

            CABFacility.Colony.Facilities.Where(facility => !KCProductionFacility.GetQueue(facility.Colony).OfType<KCFacilityProductionQueueItem>().Any(item => item.FacilityId == facility.id && !item.IsUpgrade)).ToList().ForEach(facility => addType(facility));

            facilitiesByType.ToList().ForEach(kvp => kvp.Value.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName)));

            selectedType ??= "CAB";

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(250));
                scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes);
                {
                    if (selectedType == "CAB") GUI.enabled = false;
                    if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_CAB")))
                    {
                        selectedType = "CAB";
                        scrollPosFacilities = new Vector2();
                    }
                    GUI.enabled = true;

                    facilitiesByType.ToList().ForEach(kvp =>
                    {
                        if (selectedType == kvp.Key) GUI.enabled = false;
                        if (GUILayout.Button($"{kvp.Key} ({kvp.Value.Count})"))
                        {
                            selectedType = kvp.Key;
                            scrollPosFacilities = new Vector2();
                        }
                        GUI.enabled = true;
                    });
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(620));
                if (selectedType != "CAB")
                    GUILayout.Label(Localizer.Format("#LOC_KC_CAB_FACILITIES_OF_TYPE", selectedType, CABFacility.Colony.DisplayName));
                scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities);
                {
                    GUILayout.Space(10);

                    if (selectedType == "CAB")
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical(GUILayout.Width(250));
                            {
                                GUILayout.Label($"<b>{CABFacility.Colony.DisplayName}{(CABFacility.Colony.UseCustomDisplayName ? $" ({CABFacility.Colony.BodyName})" : "")}</b>");
                                GUILayout.Label(Localizer.Format("#LOC_KC_CAB_LEVEL", CABFacility.level, CABFacility.maxLevel));
                                GUILayout.Label(Localizer.Format("#LOC_KC_CAB_FACILITIES", CABFacility.Colony.Facilities.Count));
                            }
                            GUILayout.EndVertical();

                            GUILayout.FlexibleSpace();

                            GUILayout.BeginVertical(GUILayout.Width(250));
                            {
                                if (KCProductionFacility.UpgradedFacilities[CABFacility.Colony].Contains(CABFacility))
                                {
                                    if (!playerInColony) GUI.enabled = false;
                                    if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_PLACE_UPGRADE")))
                                    {
                                        KCFacilityBase.UpgradeFacilityWithAdditionalGroup(CABFacility);
                                        KCProductionFacility.UpgradedFacilities[CABFacility.Colony].Remove(CABFacility);
                                    }
                                    GUI.enabled = true;
                                }
                                else if (KCProductionFacility.GetQueue(CABFacility.Colony).OfType<KCFacilityProductionQueueItem>().Any(item => item.FacilityId == CABFacility.id && item.IsUpgrade))
                                {
                                    GUI.enabled = false;
                                    GUILayout.Button(Localizer.Format("#LOC_KC_CAB_UPGRADING"));
                                    GUI.enabled = true;
                                }
                                else
                                {
                                    if (CABFacility.upgradeable && CABFacility.level < CABFacility.maxLevel)
                                    {
                                        if (!KCTechTreeHandler.CanBuild(CABFacility.facilityInfo, CABFacility.level + 1))
                                        {
                                            GUI.enabled = false;
                                            GUILayout.Button(Localizer.Format("#LOC_KC_CAB_UPGRADE_TECH_REQUIRED"));

                                            GUI.enabled = true;
                                            List<string> missingTechIds = KCTechTreeHandler.GetMissingTechIds(CABFacility.facilityInfo, CABFacility.level + 1);
                                            foreach (string techId in missingTechIds)
                                            {
                                                GUILayout.Label(Localizer.Format("#LOC_KC_CAB_MISSING_TECH", ResearchAndDevelopment.GetTechnologyTitle(techId)));
                                            }
                                        }
                                        else
                                        {
                                            if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_UPGRADE")))
                                            {
                                                Configuration.writeLog($"KC: Upgrading facility {CABFacility.DisplayName} in {CABFacility.Colony.DisplayName} to level {CABFacility.level + 1}");
                                                CABFacility.AddUpgradeableFacility(CABFacility);
                                            }
                                            else
                                            {
                                                GUI.enabled = true;
                                                GUILayout.Label(Localizer.Format("#LOC_KC_CAB_UPGRADE_COST"));
                                                CABFacility.facilityInfo.resourceCost[CABFacility.level + 1].ToList().ForEach(pair =>
                                                {
                                                    GUILayout.Label(Localizer.Format("#LOC_KC_CAB_RESOURCE_COST", pair.Key.displayName, (pair.Value * Configuration.FacilityCostMultiplier).ToString("f3")));
                                                });
                                                if (CABFacility.facilityInfo.Funds[CABFacility.level + 1] != 0) GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_FUNDS", (CABFacility.facilityInfo.Funds[CABFacility.level + 1] * Configuration.FacilityCostMultiplier).ToString("f3")));
                                                GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_TIME", (CABFacility.facilityInfo.UpgradeTimes[CABFacility.level + 1] * Configuration.FacilityTimeMultiplier).ToString("f3")));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GUI.enabled = false;
                                        GUILayout.Button(Localizer.Format("#LOC_KC_CAB_MAX_LEVEL"));
                                        GUI.enabled = true;
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();

                        scrollPosOverview = GUILayout.BeginScrollView(scrollPosOverview, GUILayout.Height(400));
                        {
                            CABInfoWindow.Invoke(CABFacility.Colony);
                        }
                        GUILayout.EndScrollView();
                    }
                    else
                        for (int i = 0; i < facilitiesByType[selectedType].Count; i++)
                        {
                            KCFacilityBase facility = facilitiesByType[selectedType][i];
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.BeginVertical(GUILayout.Width(195));
                                {
                                    GUILayout.Label(facility.DisplayName);
                                    GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_LEVEL", facility.level));
                                    if ((facility.AllowClick && playerInColony) || (facility.AllowRemote && !playerInColony))
                                    {
                                        if (KCProductionFacility.ConstructedFacilities[facility.Colony].Contains(facility) || (!facility.AllowClick && playerInColony) || (!facility.AllowRemote && !playerInColony))
                                            GUI.enabled = false;

                                        if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_OPEN")))
                                        {
                                            facility.Update();
                                            if (playerInColony) facility.OnBuildingClicked();
                                            else facility.OnRemoteClicked();
                                        }
                                        GUI.enabled = true;
                                    }
                                }
                                GUILayout.EndVertical();
                                GUILayout.BeginVertical(GUILayout.Width(195));
                                {
                                    GUILayout.Label(facility.GetFacilityProductionDisplay());
                                }
                                GUILayout.EndVertical();
                                GUILayout.BeginVertical(GUILayout.Width(195));
                                {
                                    if (KCProductionFacility.ConstructedFacilities[facility.Colony].Contains(facility))
                                    {
                                        if (!playerInColony) GUI.enabled = false;
                                        if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_PLACE")))
                                        {
                                            facility.enabled = true;

                                            KCProductionFacility.ConstructedFacilities[facility.Colony].Remove(facility);

                                            string newGroupName = $"{CABFacility.Colony.Name}_{facility.name}_0_{facility.facilityTypeNumber}";

                                            ColonyBuilding.PlaceNewGroup(facility, newGroupName);
                                        }
                                        GUI.enabled = true;
                                    }
                                    else if (KCProductionFacility.UpgradedFacilities[facility.Colony].Contains(facility))
                                    {
                                        if (!playerInColony) GUI.enabled = false;
                                        if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_PLACE_UPGRADE")))
                                        {
                                            KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facility);
                                            KCProductionFacility.UpgradedFacilities[facility.Colony].Remove(facility);
                                        }
                                        GUI.enabled = true;
                                    }
                                    else if (KCProductionFacility.GetQueue(facility.Colony).OfType<KCFacilityProductionQueueItem>().Any(item => item.FacilityId == facility.id && item.IsUpgrade))
                                    {
                                        GUI.enabled = false;
                                        GUILayout.Button(Localizer.Format("#LOC_KC_CAB_UPGRADING"));
                                        GUI.enabled = true;
                                    }
                                    else
                                    {
                                        if (facility.upgradeable && facility.level < facility.maxLevel)
                                        {
                                            if (!KCTechTreeHandler.CanBuild(facility.facilityInfo, facility.level + 1))
                                            {
                                                GUI.enabled = false;
                                                GUILayout.Button(Localizer.Format("#LOC_KC_CAB_UPGRADE_TECH_REQUIRED"));

                                                GUI.enabled = true;
                                                List<string> missingTechIds = KCTechTreeHandler.GetMissingTechIds(facility.facilityInfo, facility.level + 1);
                                                foreach (string techId in missingTechIds)
                                                {
                                                    GUILayout.Label(Localizer.Format("#LOC_KC_CAB_MISSING_TECH", ResearchAndDevelopment.GetTechnologyTitle(techId)));
                                                }
                                            }
                                            else
                                            {
                                                bool higherCABLevelNeeded = facility.facilityInfo.MinCABLevel[facility.level] > CABFacility.level;

                                                if (higherCABLevelNeeded) GUI.enabled = false;
                                                if (GUILayout.Button(Localizer.Format("#LOC_KC_CAB_UPGRADE")))
                                                {
                                                    Configuration.writeLog($"KC: Upgrading facility {facility.DisplayName} in {CABFacility.Colony.DisplayName} to level {facility.level + 1}");
                                                    CABFacility.AddUpgradeableFacility(facility);
                                                    continue;
                                                }
                                                GUI.enabled = true;

                                                GUILayout.Label(Localizer.Format("#LOC_KC_CAB_UPGRADE_COST"));
                                                facility.facilityInfo.resourceCost[facility.level + 1].ToList().ForEach(pair =>
                                                {
                                                    GUILayout.Label(Localizer.Format("#LOC_KC_CAB_RESOURCE_COST", pair.Key.displayName, (pair.Value * Configuration.FacilityCostMultiplier).ToString("f3")));
                                                });
                                                if (facility.facilityInfo.Funds[facility.level + 1] != 0) GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_FUNDS", (facility.facilityInfo.Funds[facility.level + 1] * Configuration.FacilityCostMultiplier).ToString("f3")));
                                                GUILayout.Label(Localizer.Format("#LOC_KC_COMMON_TIME", (facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier).ToString("f3")));
                                                if (higherCABLevelNeeded) GUILayout.Label(Localizer.Format("#LOC_KC_CAB_LEVEL_REQUIRED", facility.facilityInfo.MinCABLevel[facility.level], CABFacility.level));
                                            }
                                        }
                                        else
                                        {
                                            GUI.enabled = false;
                                            GUILayout.Button(Localizer.Format("#LOC_KC_CAB_MAX_LEVEL"));
                                            GUI.enabled = true;
                                        }
                                    }
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndHorizontal();
                            if (i < facilitiesByType[selectedType].Count - 1)
                            {
                                GUILayout.Space(10);
                                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                                GUILayout.Space(10);
                            }
                        }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public KC_CAB_Window(KC_CAB_Facility facility) : base(Configuration.createWindowID(), facility.name)
        {
            CABFacility = facility;
            toolRect = new Rect(100, 100, 890, 600);
        }
    }
}
