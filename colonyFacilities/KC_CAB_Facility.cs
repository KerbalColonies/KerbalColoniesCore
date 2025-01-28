using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KC_CAB_Window : KCWindowBase
    {
        private KC_CAB_Facility facility;

        protected override void CustomWindow()
        {
            KCFacilityBase.GetInformationByUUID(KCFacilityBase.GetUUIDbyFacility(facility), out string saveGame, out int bodyIndex, out string colonyName, out GroupPlaceHolder gph, out List<KCFacilityBase> facilities);

            GUILayout.BeginScrollView(new Vector2());
            {
                foreach (Type t in Configuration.BuildableFacilities.Keys)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(t.Name);
                    GUILayout.BeginVertical();
                    {
                        for (int i = 0; i < Configuration.BuildableFacilities[t].resourceCost[0].Count; i++)
                        {
                            GUILayout.Label($"{Configuration.BuildableFacilities[t].resourceCost[0].ElementAt(i).Key.displayName}: {Configuration.BuildableFacilities[t].resourceCost[0].ElementAt(i).Value}");
                        }
                    }
                    GUILayout.EndVertical();

                    if (!Configuration.BuildableFacilities[t].VesselHasRessources(FlightGlobals.ActiveVessel, 0)) { GUI.enabled = false; }
                    if (GUILayout.Button("Build"))
                    {
                        Configuration.BuildableFacilities[t].RemoveVesselRessources(FlightGlobals.ActiveVessel, 0);
                        KCFacilityBase KCFac = Configuration.CreateInstance(t, true, "");

                        KCFacilityBase.CountFacilityType(t, saveGame, bodyIndex, colonyName, out int count);
                        string groupName = $"{colonyName}_{t.Name}_0_{count + 1}";

                        KerbalKonstructs.API.CreateGroup(groupName);
                        Colonies.PlaceNewGroup(t, KCFac.baseGroupName, groupName, colonyName);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Facilities in this colony:");

                GUILayout.BeginVertical();

                List<KCFacilityBase> colonyFacilitiyList = new List<KCFacilityBase>();

                Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Values.ToList().ForEach(UUIDdict =>
                {
                    UUIDdict.Values.ToList().ForEach(colonyFacilitys =>
                    {
                        colonyFacilitys.ForEach(colonyFacility =>
                        {
                            if (!colonyFacilitiyList.Contains(colonyFacility))
                            {
                                colonyFacilitiyList.Add(colonyFacility);
                            }
                        });
                    });
                });


                colonyFacilitiyList.ForEach(colonyFacility =>
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(colonyFacility.name);
                    GUILayout.Label(colonyFacility.level.ToString());
                    GUILayout.Label(colonyFacility.GetFacilityProductionDisplay());

                    if (colonyFacility.upgradeable && colonyFacility.level < colonyFacility.maxLevel)
                    {
                        if (Configuration.BuildableFacilities[colonyFacility.GetType()].VesselHasRessources(FlightGlobals.ActiveVessel, colonyFacility.level + 1))
                        {
                            GUILayout.BeginVertical();
                            {
                                Configuration.BuildableFacilities[colonyFacility.GetType()].resourceCost[colonyFacility.level].ToList().ForEach(pair =>
                                {
                                    GUILayout.Label($"{pair.Key.displayName}: {pair.Value}");
                                });

                                if (GUILayout.Button("Upgrade"))
                                {
                                    Configuration.BuildableFacilities[colonyFacility.GetType()].RemoveVesselRessources(FlightGlobals.ActiveVessel, colonyFacility.level + 1);
                                    if (colonyFacility.upgradeType == UpgradeType.withGroupChange)
                                    {
                                        KCFacilityBase.UpgradeFacilityWithGroupChange(colonyFacility);
                                    }
                                    else if (colonyFacility.upgradeType == UpgradeType.withoutGroupChange)
                                    {
                                        KCFacilityBase.UpgradeFacilityWithoutGroupChange(colonyFacility);
                                    }
                                    else if (colonyFacility.upgradeType == UpgradeType.withAdditionalGroup)
                                    {
                                        facility.AddUpgradeableFacility(colonyFacility);

                                        //KCFacilityBase.UpgradeFacilityWithAdditionalGroup(colonyFacility);
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndHorizontal();
                });

                GUILayout.Label("Upgrading Facilities:");
                facility.UpgradingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime();
                    GUILayout.Label($"{max - pair.Value}/{max}");
                    GUILayout.EndHorizontal();
                });

                GUILayout.Label("Upgraded Facilities:");
                facility.UpgradedFacilities.ToList().ForEach(facilityUpgrade =>
                {
                    GUILayout.Label(facilityUpgrade.name);

                    if (GUILayout.Button("Place upgrade"))
                    {
                        KCFacilityBase.UpgradeFacilityWithAdditionalGroup(facilityUpgrade);
                        facility.UpgradedFacilities.Remove(facilityUpgrade);
                    }
                });

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }


        public KC_CAB_Window(KC_CAB_Facility facility) : base(Configuration.createWindowID(facility), facility.name)
        {
            this.facility = facility;
            this.toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    [System.Serializable]
    internal class KC_CAB_Facility : KCFacilityBase
    {
        private KC_CAB_Window window;

        /// <summary>
        /// A dictionary containg all additional group upgrades for facilities.
        /// <para>The additional groups can be placed when the </para>
        /// <para>The key is the facilityUpgrade id</para>
        /// </summary>
        private Dictionary<KCFacilityBase, double> upgradingFacilities = new Dictionary<KCFacilityBase, double>();
        private List<KCFacilityBase> upgradedFacilities = new List<KCFacilityBase>();

        public Dictionary<KCFacilityBase, double> UpgradingFacilities
        {
            get { return upgradingFacilities; }
        }

        public List<KCFacilityBase> UpgradedFacilities
        {
            get { return upgradedFacilities; }
            set { upgradedFacilities = value; }
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            if (upgradingFacilities.Count > 0)
            {
                double totalProduction = 0;
                KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs);

                List<KCFacilityBase> colonyFacilities = KCFacilityBase.GetFacilitiesInColony(saveGame, bodyIndex, colonyName);
                colonyFacilities.ForEach(facility =>
                {
                    if (typeof(KCBuildingProductionFacility).IsAssignableFrom(facility.GetType()))
                    {
                        totalProduction += ((KCBuildingProductionFacility)facility).dailyProduction() * deltaTime / 24 / 60 / 60;
                    }
                });

                while (totalProduction > 0)
                {

                    if (upgradingFacilities.ElementAt(0).Value > totalProduction)
                    {
                        upgradingFacilities[upgradingFacilities.ElementAt(0).Key] -= totalProduction;
                        totalProduction = 0;
                        break;
                    }
                    else
                    {
                        totalProduction -= upgradingFacilities.ElementAt(0).Value;
                        upgradedFacilities.Add(upgradingFacilities.ElementAt(0).Key);
                        upgradingFacilities.Remove(upgradingFacilities.ElementAt(0).Key);

                        if (upgradingFacilities.Count == 0)
                        {
                            break;
                        }
                    }
                }
            }

            base.Update();
        }

        public void AddUpgradeableFacility(KCFacilityBase facility)
        {
            if (facility.GetUpgradeTime() == 0)
            {
                upgradedFacilities.Add(facility);
            }
            else
            {
                upgradingFacilities.Add(facility, facility.GetUpgradeTime());
            }
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            window = new KC_CAB_Window(this);
            enabled = true;
        }

        public KC_CAB_Facility() : base("KCCABFacility", true, "")
        {

        }
    }
}
