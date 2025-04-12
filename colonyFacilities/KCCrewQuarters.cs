using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuartersWindow : KCWindowBase
    {
        KCCrewQuarters CrewQuarterFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUI.enabled = true;

            kerbalGUI.StaffingInterface();

            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(Configuration.createWindowID(CrewQuarterFacility), "Crewquarters")
        {
            this.CrewQuarterFacility = CrewQuarterFacility;
            this.kerbalGUI = new KerbalGUI(CrewQuarterFacility, false);
            toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    internal class KCCrewQuarters : KCKerbalFacilityBase
    {
        public static List<KCCrewQuarters> CrewQuartersInColony(colonyClass colony)
        {
            return colony.Facilities.Where(f => f is KCCrewQuarters).Select(f => (KCCrewQuarters)f).ToList();
        }

        public static int ColonyKerbalCapacity(colonyClass colony)
        {
            return CrewQuartersInColony(colony).Sum(crewQuarter => crewQuarter.MaxKerbals);
        }

        public static KCCrewQuarters FindKerbalInCrewQuarters(colonyClass colony, ProtoCrewMember kerbal)
        {
            List<KCKerbalFacilityBase> facilitiesWithKerbal = KCKerbalFacilityBase.findKerbal(colony, kerbal);
            return (KCCrewQuarters) facilitiesWithKerbal.Where(fac => fac is KCCrewQuarters).FirstOrDefault();
        }

        public static bool AddKerbalToColony(colonyClass colony, ProtoCrewMember kerbal)
        {
            if (FindKerbalInCrewQuarters(colony, kerbal) != null) { return false; }

            foreach (KCCrewQuarters crewQuarter in CrewQuartersInColony(colony))
            {
                if (crewQuarter.kerbals.Count < crewQuarter.MaxKerbals)
                {
                    crewQuarter.AddKerbal(kerbal);
                    return true;
                }
            }

            return false;
        }

        private KCCrewQuartersWindow crewQuartersWindow;

        /// <summary>
        /// Adds the kerbal to this crew quarrter or moves it from another crew quarter over to this one if the kerbal is already assigned to a crew quarter in this Colony
        /// </summary>
        /// <param name="kerbal"></param>
        public override void AddKerbal(ProtoCrewMember kerbal)
        {
            KCCrewQuarters oldCrewQuarter = FindKerbalInCrewQuarters(Colony, kerbal);

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
                KCKerbalFacilityBase.findKerbal(Colony, kerbal).Where(x => !(x is KCCrewQuarters)).ToList().ForEach(facility =>
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
            if (crewQuartersWindow == null) crewQuartersWindow = new KCCrewQuartersWindow(this);

            if (crewQuartersWindow.IsOpen())
            {
                crewQuartersWindow.Close();
                if (FlightGlobals.ActiveVessel != null)
                {
                    crewQuartersWindow.kerbalGUI.ksg.Close();
                    crewQuartersWindow.kerbalGUI.transferWindow = false;
                }
            }
            else
            {
                crewQuartersWindow.Open();
            }
        }

        public KCCrewQuarters(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            this.crewQuartersWindow = null;
        }

        public KCCrewQuarters(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, true)
        {
            this.crewQuartersWindow = null;
        }
    }
}
