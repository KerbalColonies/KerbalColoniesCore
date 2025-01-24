using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace KerbalColonies.colonyFacilities
{
    internal class KCResearchFacilityCost : KCFacilityCostClass
    {
        public override bool VesselHasRessources(Vessel vessel, int level)
        {
            for (int i = 0; i < resourceCost[level].Count; i++)
            {
                vessel.GetConnectedResourceTotals(resourceCost[level].ElementAt(i).Key.id, false, out double amount, out double maxAmount);

                if (amount < resourceCost[level].ElementAt(i).Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool RemoveVesselRessources(Vessel vessel, int level)
        {
            if (VesselHasRessources(vessel, 0))
            {
                for (int i = 0; i < resourceCost[level].Count; i++)
                {
                    vessel.RequestResource(vessel.rootPart, resourceCost[level].ElementAt(i).Key.id, resourceCost[level].ElementAt(i).Value, true);
                }
                return true;
            }
            return false;
        }

        public KCResearchFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, float>>{ 
                { 0, new Dictionary<PartResourceDefinition, float> { 
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1000f },
                    { PartResourceLibrary.Instance.GetDefinition("XenonGas"), 100f } }
                },
                { 1, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1200f },
                    { PartResourceLibrary.Instance.GetDefinition("XenonGas"), 200f } }
                },
                { 2, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1400f },
                    { PartResourceLibrary.Instance.GetDefinition("XenonGas"), 400f } }
                },
                { 3, new Dictionary<PartResourceDefinition, float> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1600f },
                    { PartResourceLibrary.Instance.GetDefinition("XenonGas"), 600f } }
                },
            };
        }
    }

    internal class KCResearchFacilityWindow : KCWindowBase
    {
        KCResearchFacility researchFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            researchFacility.Update();

            if (kerbalGUI == null)
            {
                KCFacilityBase.GetInformationByFacilty(researchFacility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
                kerbalGUI = new KerbalGUI(researchFacility, saveGame, bodyIndex, colonyName);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Science Points: " + researchFacility.SciencePoints);
            GUILayout.Label("Max Science Points: " + researchFacility.MaxSciencePoints);
            GUILayout.EndHorizontal();

            kerbalGUI.StaffingInterface();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Retrieve Science Points"))
            {
                researchFacility.RetrieveSciencePoints();
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            kerbalGUI.ksg.Close();
            kerbalGUI.transferWindow = false;
        }

        public KCResearchFacilityWindow(KCResearchFacility researchFacility) : base(Configuration.createWindowID(researchFacility), "Researchfacility")
        {
            this.researchFacility = researchFacility;
            toolRect = new Rect(100, 100, 400, 800);
            this.kerbalGUI = null;
        }
    }


    [System.Serializable]
    internal class KCResearchFacility : KCKerbalFacilityBase
    {
        private KCResearchFacilityWindow researchFacilityWindow;

        private float sciencePoints;

        public float MaxSciencePoints { get { return maxSciencePointList[level]; } }
        public float SciencePoints { get { return sciencePoints; } }

        private List<float> maxSciencePointList = new List<float> { 50, 100, 200, 400 };
        private List<float> researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
        private List<int> maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Scientist").ToList();
        }

        public override void Update()
        {
            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();
            sciencePoints = Math.Min(maxSciencePointList[level], sciencePoints + (float)((researchpointsPerDayperResearcher[level] / 24 / 60 / 60) * deltaTime) * kerbals.Count);
            Configuration.SaveColonies();
        }

        public override void OnBuildingClicked()
        {
            if (researchFacilityWindow.IsOpen())
            {
                researchFacilityWindow.Close();
                researchFacilityWindow.kerbalGUI.ksg.Close();
                researchFacilityWindow.kerbalGUI.transferWindow = false;
            }
            else
            {
                researchFacilityWindow.Open();
            }
        }

        public bool RetrieveSciencePoints()
        {
            if (sciencePoints > 0)
            {
                if (ResearchAndDevelopment.Instance == null)
                {
                    sciencePoints = 0;
                    return false;
                }
                ResearchAndDevelopment.Instance.AddScience(sciencePoints, TransactionReasons.Cheating);
                sciencePoints = 0;
                return true;
            }
            return false;
        }

        public override bool UpgradeFacility(int level)
        {
            base.UpgradeFacility(level);
            maxKerbals = maxKerbalsPerLevel[level];
            return true;
        }

        public override void EncodeString()
        {
            string kerbalString = CreateKerbalString(kerbals);
            facilityData = $"sciencePoints&{sciencePoints}|maxKerbals&{maxKerbals}{((kerbalString != "") ? $"|{kerbalString}" : "")}";
        }

        public override void DecodeString()
        {
            if (facilityData != "")
            {
                string[] facilityDatas = facilityData.Split(new[] { '|' }, 3);
                sciencePoints = float.Parse(facilityDatas[0].Split('&')[1]);
                maxKerbals = Convert.ToInt32(facilityDatas[1].Split('&')[1]);
                if (facilityDatas.Length > 2)
                {
                    kerbals = CreateKerbalList(facilityDatas[2]);
                }
            }
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            this.researchFacilityWindow = new KCResearchFacilityWindow(this);
            this.baseGroupName = "KC_CAB";

            maxSciencePointList = new List<float> { 50, 100, 200, 400 };
            researchpointsPerDayperResearcher = new List<float> { 0.25f, 0.3f, 0.35f, 0.4f };
            maxKerbalsPerLevel = new List<int> { 4, 6, 8, 12 };

            this.maxKerbals = maxKerbalsPerLevel[level];
            this.upgradeType = UpgradeType.withoutGroupChange;
        }

        public KCResearchFacility(bool enabled, string facilityData = "") : base("KCResearchFacility", enabled, 8, "", 0, 3)
        {
            sciencePoints = 0;
        }
    }
}
