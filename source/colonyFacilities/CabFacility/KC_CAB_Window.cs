using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.CabFacility
{
    public class KC_CAB_Window : KCWindowBase
    {
        public static Action<colonyClass> CABInfoWindow;
        public static int CABInfoWidth = 590;

        private KC_CAB_Facility CABFacility;

        string selectedType;
        Vector2 scrollPosTypes = new Vector2();
        Vector2 scrollPosFacilities = new Vector2();
        protected override void CustomWindow()
        {
            CABFacility.Colony.UpdateColony();
            bool playerInColony = CABFacility.PlayerInColony;

            SortedDictionary<string, List<KCFacilityBase>> facilitiesByType = new SortedDictionary<string, List<KCFacilityBase>>();

            void addType(KCFacilityBase facility)
            {
                string category = facility.facilityInfo.category;
                if (!facilitiesByType.ContainsKey(category)) facilitiesByType.Add(category, new List<KCFacilityBase> { facility });
                else if (!facilitiesByType[category].Contains(facility)) facilitiesByType[category].Add(facility);
            }

            CABFacility.Colony.Facilities.Where(facility => !KCProductionFacility.ConstructingFacilities[facility.Colony].ContainsKey(facility)).ToList().ForEach(facility => addType(facility));

            facilitiesByType.ToList().ForEach(kvp => kvp.Value.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName)));

            selectedType = selectedType ?? "CAB";

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(250));
                scrollPosTypes = GUILayout.BeginScrollView(scrollPosTypes);
                {
                    if (selectedType == "CAB") GUI.enabled = false;
                    if (GUILayout.Button($"CAB"))
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
                    GUILayout.Label($"<b>Facilities of type {selectedType} in {CABFacility.Colony.DisplayName}</b>:");
                scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities);
                {
                    GUILayout.Space(10);

                    if (selectedType == "CAB")
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical(GUILayout.Width(195));
                            {
                                GUILayout.Label($"<b>{CABFacility.Colony.DisplayName}{(CABFacility.Colony.UseCustomDisplayName ? $" ({CABFacility.Colony.BodyName})" : "")}</b>");
                                GUILayout.Label($"Facilities: {CABFacility.Colony.Facilities.Count}");
                            }
                            GUILayout.EndVertical();

                            GUILayout.FlexibleSpace();

                            GUILayout.BeginVertical(GUILayout.Width(195));
                            {
                                if (KCProductionFacility.ConstructedFacilities[CABFacility.Colony].Contains(CABFacility))
                                {
                                    if (!playerInColony) GUI.enabled = false;
                                    if (GUILayout.Button("Place"))
                                    {
                                        CABFacility.enabled = true;

                                        KCProductionFacility.ConstructedFacilities[CABFacility.Colony].Remove(CABFacility);

                                        string newGroupName = $"{CABFacility.Colony.Name}_{CABFacility.name}_0_{CABFacility.facilityTypeNumber}";

                                        ColonyBuilding.PlaceNewGroup(CABFacility, newGroupName);
                                    }
                                    GUI.enabled = true;
                                }
                                else if (KCProductionFacility.UpgradedFacilities[CABFacility.Colony].Contains(CABFacility))
                                {
                                    if (!playerInColony) GUI.enabled = false;
                                    if (GUILayout.Button("Place upgrade"))
                                    {
                                        KCFacilityBase.UpgradeFacilityWithAdditionalGroup(CABFacility);
                                        KCProductionFacility.UpgradedFacilities[CABFacility.Colony].Remove(CABFacility);
                                    }
                                    GUI.enabled = true;
                                }
                                else if (KCProductionFacility.UpgradingFacilities[CABFacility.Colony].Keys.Contains(CABFacility))
                                {
                                    GUI.enabled = false;
                                    GUILayout.Button("Upgrading...");
                                    GUI.enabled = true;
                                }
                                else
                                {
                                    if (CABFacility.upgradeable && CABFacility.level < CABFacility.maxLevel)
                                    {
                                        if (!CABFacility.facilityInfo.checkResources(CABFacility.level + 1, CABFacility.Colony)) GUI.enabled = false;
                                        if (GUILayout.Button("Upgrade"))
                                        {
                                            Configuration.writeLog($"KC: Upgrading facility {CABFacility.DisplayName} in {CABFacility.Colony.DisplayName} to level {CABFacility.level + 1}");
                                            CABFacility.facilityInfo.removeResources(CABFacility.level + 1, CABFacility.Colony);
                                            CABFacility.AddUpgradeableFacility(CABFacility);
                                        }
                                        GUI.enabled = true;
                                        GUILayout.BeginHorizontal();
                                        {
                                            GUILayout.Label("Upgrade cost:");
                                            GUILayout.BeginVertical();
                                            {
                                                CABFacility.facilityInfo.resourceCost[CABFacility.level + 1].ToList().ForEach(pair =>
                                                {
                                                    GUILayout.Label($"{pair.Key.displayName}: {pair.Value * Configuration.FacilityCostMultiplier}");
                                                });
                                                if (CABFacility.facilityInfo.Funds[CABFacility.level + 1] != 0) GUILayout.Label($"Funds: {CABFacility.facilityInfo.Funds[CABFacility.level + 1] * Configuration.FacilityCostMultiplier}");
                                            }
                                            GUILayout.EndVertical();
                                        }
                                        GUILayout.EndHorizontal();
                                        GUILayout.Label($"Time: {CABFacility.facilityInfo.UpgradeTimes[CABFacility.level + 1] * Configuration.FacilityTimeMultiplier}");
                                    }
                                    else
                                    {
                                        GUI.enabled = false;
                                        GUILayout.Button("Max level reached");
                                        GUI.enabled = true;
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();

                        scrollPosFacilities = GUILayout.BeginScrollView(scrollPosFacilities);
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
                                    GUILayout.Label($"Level: {facility.level}");
                                    if (facility.AllowClick && playerInColony || facility.AllowRemote && !playerInColony)
                                    {
                                        if (KCProductionFacility.ConstructedFacilities[facility.Colony].Contains(facility) || (!facility.AllowClick && playerInColony) || (!facility.AllowRemote && !playerInColony))
                                            GUI.enabled = false;

                                        if (GUILayout.Button("Open"))
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
                                        if (GUILayout.Button("Place"))
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
                                        if (GUILayout.Button("Place upgrade"))
                                        {
                                            KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facility);
                                            KCProductionFacility.UpgradedFacilities[facility.Colony].Remove(facility);
                                        }
                                        GUI.enabled = true;
                                    }
                                    else if (KCProductionFacility.UpgradingFacilities[facility.Colony].Keys.Contains(facility))
                                    {
                                        GUI.enabled = false;
                                        GUILayout.Button("Upgrading...");
                                        GUI.enabled = true;
                                    }
                                    else
                                    {
                                        if (facility.upgradeable && facility.level < facility.maxLevel)
                                        {
                                            if (!facility.facilityInfo.checkResources(facility.level + 1, CABFacility.Colony)) GUI.enabled = false;
                                            if (GUILayout.Button("Upgrade"))
                                            {
                                                Configuration.writeLog($"KC: Upgrading facility {facility.DisplayName} in {CABFacility.Colony.DisplayName} to level {facility.level + 1}");
                                                facility.facilityInfo.removeResources(facility.level + 1, CABFacility.Colony);
                                                CABFacility.AddUpgradeableFacility(facility);
                                                continue;
                                            }
                                            GUI.enabled = true;
                                            GUILayout.BeginHorizontal();
                                            {
                                                GUILayout.Label("Upgrade cost:");
                                                GUILayout.BeginVertical();
                                                {
                                                    facility.facilityInfo.resourceCost[facility.level + 1].ToList().ForEach(pair =>
                                                    {
                                                        GUILayout.Label($"{pair.Key.displayName}: {pair.Value * Configuration.FacilityCostMultiplier}");
                                                    });
                                                    if (facility.facilityInfo.Funds[facility.level + 1] != 0) GUILayout.Label($"Funds: {facility.facilityInfo.Funds[facility.level + 1] * Configuration.FacilityCostMultiplier}");
                                                }
                                                GUILayout.EndVertical();
                                            }
                                            GUILayout.EndHorizontal();
                                            GUILayout.Label($"Time: {facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier}");
                                        }
                                        else
                                        {
                                            GUI.enabled = false;
                                            GUILayout.Button("Max level reached");
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
            this.CABFacility = facility;
            this.toolRect = new Rect(100, 100, 890, 600);
        }
    }
}
