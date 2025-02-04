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
                        KCFacilityBase KCFac = Configuration.CreateInstance(t, false, "");

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

                                if (!(facility.UpgradingFacilities.ContainsKey(colonyFacility) || facility.UpgradedFacilities.Contains(colonyFacility)))
                                {
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
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Facilities under construction:");
                facility.ConstructingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime(0);
                    GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Facilities waiting to be placed:");
                facility.ConstructedFacilities.ToList().ForEach(newFacility =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(newFacility.name);
                    if (GUILayout.Button("Place"))
                    {
                        newFacility.enabled = true;

                        facility.ConstructedFacilities.Remove(newFacility);

                        KCFacilityBase.CountFacilityType(newFacility.GetType(), saveGame, bodyIndex, colonyName, out int count);
                        string groupName = $"{colonyName}_{newFacility.GetType().Name}_0_{count + 1}";

                        KerbalKonstructs.API.CreateGroup(groupName);
                        Colonies.PlaceNewGroup(newFacility, newFacility.baseGroupName, groupName, colonyName);
                    }
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

                GUILayout.Label("Upgrading Facilities:");
                facility.UpgradingFacilities.ToList().ForEach(pair =>
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key.name);
                    double max = pair.Key.GetUpgradeTime(pair.Key.level + 1);
                    GUILayout.Label($"{Math.Round(max - pair.Value, 2)}/{Math.Round(max, 2)}");
                    GUILayout.EndHorizontal();
                });

                GUILayout.Space(10);

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
        public static Dictionary<Type, int> defaultFacilities = new Dictionary<Type, int>();
        public static void addDefaultFacility(Type facilityType, int amount)
        {
            if (typeof(KCFacilityBase).IsAssignableFrom(facilityType))
            {
                if (!defaultFacilities.ContainsKey(facilityType))
                {
                    defaultFacilities.Add(facilityType, amount);
                }
            }
        }

        private KC_CAB_Window window;

        private Dictionary<KCFacilityBase, double> constructingFacilities = new Dictionary<KCFacilityBase, double>();
        private List<KCFacilityBase> constructedFacilities = new List<KCFacilityBase>();
        internal void addConstructingFacility(KCFacilityBase facility, double time)
        {
            if (!(constructingFacilities.ContainsKey(facility) || constructedFacilities.Contains(facility)))
            {
                constructingFacilities.Add(facility, time);
            }
        }
        internal void addConstructedFacility(KCFacilityBase facility)
        {
            if (constructingFacilities.ContainsKey(facility))
            {
                constructingFacilities.Remove(facility);
            }
            if (!constructedFacilities.Contains(facility))
            {
                constructedFacilities.Add(facility);
            }
        }

        /// <summary>
        /// A dictionary containg all additional group upgrades for facilities.
        /// <para>The additional groups can be placed when the </para>
        /// <para>The key is the facilityUpgrade id</para>
        /// </summary>
        private Dictionary<KCFacilityBase, double> upgradingFacilities = new Dictionary<KCFacilityBase, double>();
        private List<KCFacilityBase> upgradedFacilities = new List<KCFacilityBase>();
        internal void addUpgradingFacility(KCFacilityBase facility, double time)
        {
            if (!(upgradingFacilities.ContainsKey(facility) || upgradedFacilities.Contains(facility)))
            {
                upgradingFacilities.Add(facility, time);
            }
        }
        internal void addUpgradedFacility(KCFacilityBase facility)
        {
            if (upgradingFacilities.ContainsKey(facility))
            {
                upgradingFacilities.Remove(facility);
            }

            if (!upgradedFacilities.Contains(facility))
            {
                upgradedFacilities.Add(facility);
            }
        }

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
                                    addUpgradedFacility(facility);
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
                            constructingFacilities.Remove(facility);
                            addConstructedFacility(facility);
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
                        addUpgradedFacility(facility);
                        break;
                }
            }
            else
            {
                addUpgradingFacility(facility, facility.GetUpgradeTime(facility.level + 1));
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.GetUpgradeTime(0) == 0)
            {
                addConstructedFacility(facility);
            }
            else
            {
                addConstructingFacility(facility, facility.GetUpgradeTime(0));
            }
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }

        public override void EncodeString()
        {
            string data = "";
            List<string> serializedFacilities = new List<string>();

            foreach (KCFacilityBase facility in constructingFacilities.Keys)
            {
                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility);
                data += $"<<>>{serializedFacility}<0>constr<0>{constructingFacilities[facility]}";
            }

            foreach (KCFacilityBase facility in constructedFacilities)
            {
                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility);
                data += $"<<>>{serializedFacility}<0>constrDone";
            }

            foreach (KCFacilityBase facility in upgradingFacilities.Keys)
            {
                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility);
                data += $"<<>>{serializedFacility}<0>upgr<0>{upgradingFacilities[facility]}";
            }

            foreach (KCFacilityBase facility in upgradedFacilities)
            {
                string serializedFacility = KCFacilityClassConverter.SerializeObject(facility);
                data += $"<<>>{serializedFacility}<0>upgrDone";
            }

            facilityData = data.Replace("/{", "<#>").Replace("}", ">#<").Replace(":", "<##>").Replace(",", "<###>");
        }

        public override void DecodeString()
        {
            if (!string.IsNullOrEmpty(facilityData))
            {
                facilityData = facilityData.Replace("<#>", "/{").Replace(">#<", "}").Replace("<##>", ":").Replace("<###>", ",");

                string[] serializedFacilities = facilityData.Split(new[] { "<<>>" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string serializedFacility in serializedFacilities)
                {
                    if (string.IsNullOrEmpty(serializedFacility))
                    {
                        continue;
                    }

                    string[] data = serializedFacility.Split(new[] { "<0>" }, StringSplitOptions.RemoveEmptyEntries);

                    KCFacilityBase facility = KCFacilityClassConverter.DeserializeObject(data[0]);

                    switch (data[1])
                    {
                        case "constr":
                            constructingFacilities.Add(facility, double.Parse(data[2]));
                            break;
                        case "constrDone":
                            constructedFacilities.Add(facility);
                            break;
                        case "upgr":
                            upgradingFacilities.Add(facility, double.Parse(data[2]));
                            break;
                        case "upgrDone":
                            upgradedFacilities.Add(facility);
                            break;
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

        public KC_CAB_Facility(bool enabled, string facilityData = "") : base("KCCABFacility", enabled, facilityData, 0, 0) { }

        public KC_CAB_Facility() : base("KCCABFacility", true, "")
        {

        }
    }
}
