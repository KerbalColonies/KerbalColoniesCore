using KerbalColonies.Serialization;
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
            facility.Update();
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

                        facility.AddconstructingFacility(KCFac);
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

                GUILayout.Label("Facilities waiting to be placed:");
                facility.ConstructedFacilities.ToList().ForEach(newFacility =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(newFacility.name);
                    if (GUILayout.Button("Place"))
                    {
                        facility.ConstructedFacilities.Remove(newFacility);
                        
                        KCFacilityBase.CountFacilityType(newFacility.GetType(), saveGame, bodyIndex, colonyName, out int count);
                        string groupName = $"{colonyName}_{newFacility.GetType().Name}_0_{count + 1}";

                        KerbalKonstructs.API.CreateGroup(groupName);
                        Colonies.PlaceNewGroup(newFacility, newFacility.baseGroupName, groupName, colonyName);
                    }
                    GUILayout.EndHorizontal();
                });

                GUILayout.Label("Facilities under construction:");
                facility.ConstructingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime(0);
                    GUILayout.Label($"{max - pair.Value}/{max}");
                    GUILayout.EndHorizontal();
                });

                GUILayout.Label("Upgrading Facilities:");
                facility.UpgradingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime(pair.Key.level + 1);
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

        private Dictionary<KCFacilityBase, double> constructingFacilities = new Dictionary<KCFacilityBase, double>();
        private List<KCFacilityBase> constructedFacilities = new List<KCFacilityBase>();

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

        public Dictionary<KCFacilityBase, double> ConstructingFacilities
        {
            get { return constructingFacilities; }
        }
        public List<KCFacilityBase> ConstructedFacilities
        {
            get { return constructedFacilities; }
            set { constructedFacilities = value; }
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            if (upgradingFacilities.Count > 0 || constructingFacilities.Count > 0)
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

                    if (upgradingFacilities.Count > 0)
                    {
                        if (upgradingFacilities.ElementAt(0).Value > totalProduction)
                        {
                            upgradingFacilities[upgradingFacilities.ElementAt(0).Key] -= totalProduction;
                            totalProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = upgradingFacilities.ElementAt(0).Key;
                            totalProduction -= upgradingFacilities.ElementAt(0).Value;
                            upgradedFacilities.Add(facility);
                            upgradingFacilities.Remove(facility);

                            switch (facility.upgradeType)
                            {
                                case UpgradeType.withGroupChange:
                                    KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                                    break;
                                case UpgradeType.withoutGroupChange:
                                    KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                                    break;
                                case UpgradeType.withAdditionalGroup:
                                    upgradedFacilities.Add(facility);
                                    break;
                            }
                        }
                    }
                    else if (constructingFacilities.Count > 0)
                    {
                        if (constructingFacilities.ElementAt(0).Value > totalProduction)
                        {
                            constructingFacilities[constructingFacilities.ElementAt(0).Key] -= totalProduction;
                            totalProduction = 0;
                            break;
                        }
                        else
                        {
                            KCFacilityBase facility = constructingFacilities.ElementAt(0).Key;
                            totalProduction -= constructingFacilities.ElementAt(0).Value;
                            constructedFacilities.Add(facility);
                            constructingFacilities.Remove(facility);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            base.Update();
        }

        public void AddUpgradeableFacility(KCFacilityBase facility)
        {
            if (facility.GetUpgradeTime(facility.level + 1) == 0)
            {
                switch (facility.upgradeType)
                {
                    case UpgradeType.withGroupChange:
                        KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                        break;
                    case UpgradeType.withoutGroupChange:
                        KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                        break;
                    case UpgradeType.withAdditionalGroup:
                        upgradedFacilities.Add(facility);
                        break;
                }
            }
            else
            {
                upgradingFacilities.Add(facility, facility.GetUpgradeTime(facility.level + 1));
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.GetUpgradeTime(0) == 0)
            {
                constructedFacilities.Add(facility);
            }
            else
            {
                constructingFacilities.Add(facility, facility.GetUpgradeTime(0));
            }
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }

        public override void EncodeString()
        {
            List<string> serializedFacilities = new List<string>();

            foreach (var facility in constructingFacilities.Keys)
            {
                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility);
                serializedFacilities.Add(serializedFacility);
            }

            foreach (var facility in constructedFacilities)
            {
                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility);
                serializedFacilities.Add(serializedFacility);
            }

            facilityData = string.Join("|", serializedFacilities);
        }

        public override void DecodeString()
        {
            if (!string.IsNullOrEmpty(facilityData))
            {
                string[] serializedFacilities = facilityData.Split('|');

                foreach (string serializedFacility in serializedFacilities)
                {
                    KCFacilityBase facility = KCFacilityClassConverter.DeserializeObject(serializedFacility);
                    if (facility != null)
                    {
                        if (KCFacilityBase.GetFacilityByID(facility.id, out KCFacilityBase fac))
                        {
                            facility = fac;
                        }

                        if (facility.GetUpgradeTime(0) > 0)
                        {
                            constructingFacilities.Add(facility, facility.GetUpgradeTime(0));
                        }
                        else
                        {
                            constructedFacilities.Add(facility);
                        }
                    }
                }
            }
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
