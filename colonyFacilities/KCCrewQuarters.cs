using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuarterCost : KCFacilityCostClass
    {
        public override bool VesselHasRessources(Vessel vessel, int level)
        {
            for (int i = 0; i < resourceCost.Count; i++)
            {
                vessel.GetConnectedResourceTotals(resourceCost.ElementAt(i).Key.id, false, out double amount, out double maxAmount);

                if (amount < resourceCost.ElementAt(i).Value)
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
                for (int i = 0; i < resourceCost.Count; i++)
                {
                    vessel.RequestResource(vessel.rootPart, resourceCost.ElementAt(i).Key.id, resourceCost.ElementAt(i).Value, true);
                }
                return true;
            }
            return false;
        }

        public KCCrewQuarterCost()
        {
            resourceCost = new Dictionary<PartResourceDefinition, float> { { PartResourceLibrary.Instance.GetDefinition("Ore"), 100f } };
        }
    }

    internal class KCCrewQuartersWindow : KCWindowBase
    {
        KCCrewQuarters CrewQuarterFacility;
        VesselKerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            KSPLog.print("KCCrewQuartersWindow: " + this.ToString());

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUI.enabled = true;

            kerbalGUI.StaffingInterface();

            GUILayout.EndHorizontal();

            GUILayout.Space(2);
        }

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(Configuration.createWindowID(CrewQuarterFacility), "Crewquarters")
        {
            this.CrewQuarterFacility = CrewQuarterFacility;
            this.kerbalGUI = new VesselKerbalGUI(CrewQuarterFacility);
            toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    [System.Serializable]
    internal class KCCrewQuarters : KCKerbalFacilityBase
    {
        public static KCCrewQuarters FindKerbalInCrewQuarters(string saveGame, int bodyIndex, string colonyName, ProtoCrewMember kerbal)
        {
            List<KCKerbalFacilityBase> facilitiesWithKerbal = KCKerbalFacilityBase.findKerbal(saveGame, bodyIndex, colonyName, kerbal);
            foreach (KCKerbalFacilityBase facility in facilitiesWithKerbal)
            {
                if (typeof(KCCrewQuarters).IsAssignableFrom(facility.GetType()))
                {
                    return (KCCrewQuarters) facility;
                }
            }
            return null;
        }
        private KCCrewQuartersWindow crewQuartersWindow;

        /// <summary>
        /// Adds the kerbal to this crew quarrter or moves it from another crew quarter over to this one if the kerbal is already assigned to a crew quarter in this colony
        /// </summary>
        /// <param name="kerbal"></param>
        public override void AddKerbal(ProtoCrewMember kerbal)
        {
            KCFacilityBase.GetInformationByFacilty(this, out List<string> saveGame, out List<int> bodyIndex, out List<string> colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
            KCCrewQuarters oldCrewQuarter = FindKerbalInCrewQuarters(saveGame[0], bodyIndex[0], colonyName[0], kerbal);

            if (oldCrewQuarter != null)
            {
                int status = oldCrewQuarter.kerbals[kerbal];
                oldCrewQuarter.kerbals.Remove(kerbal);
                kerbals.Add(kerbal, status);
            }
            else
            {
                kerbals.Add(kerbal, 0);
            }
        }

        /// <summary>
        /// Removes the kerbal from the crew quarters and all other facilities that the kerbal is assigned to
        /// </summary>
        public override void RemoveKerbal(ProtoCrewMember kerbal)
        {
            if (kerbals.ContainsKey(kerbal))
            {
                KCFacilityBase.GetInformationByFacilty(this, out List<string> saveGame, out List<int> bodyIndex, out List<string> colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);

                List<KCKerbalFacilityBase> facilities = KCKerbalFacilityBase.findKerbal(saveGame[0], bodyIndex[0], colonyName[0], kerbal).Where(x => !typeof(KCCrewQuarters).IsAssignableFrom(x.GetType())).ToList();

                facilities.ForEach(facility =>
                {
                    facility.Update();
                    facility.RemoveKerbal(kerbal);
                });

                kerbals.Remove(kerbal);
            }
        }

        public override void Update()
        {
            base.Update();
        }

        public override void OnBuildingClicked()
        {
            KSPLog.print("KCCrewQuarters: " + this.ToString());
            crewQuartersWindow.Toggle();
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);
            this.baseGroupName = "KC_CAB";
            this.crewQuartersWindow = new KCCrewQuartersWindow(this);
        }

        public KCCrewQuarters(bool enabled, string facilityData) : base("KCCrewQuarters", true, 16)
        {
        }
    }
}
