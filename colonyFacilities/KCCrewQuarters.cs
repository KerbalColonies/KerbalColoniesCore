using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalColonies.UI;

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuartersWindow : KCWindowBase
    {
        KCCrewQuarters CrewQuarterFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            GUILayout.BeginVertical();
            kerbalGUI.StaffingInterface();
            GUILayout.EndVertical();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(Configuration.createWindowID(), "Crewquarters")
        {
            this.CrewQuarterFacility = CrewQuarterFacility;
            this.kerbalGUI = new KerbalGUI(CrewQuarterFacility, false);
            toolRect = new Rect(100, 100, 400, 600);
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
            return (KCCrewQuarters) (facilitiesWithKerbal.Where(fac => fac is KCCrewQuarters).FirstOrDefault());
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
        /// Adds the member to this crew quarrter or moves it from another crew quarter over to this one if the member is already assigned to a crew quarter in this Colony
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
        /// Removes the member from the crew quarters and all other facilities that the member is assigned to
        /// </summary>
        public override void RemoveKerbal(ProtoCrewMember member)
        {
            if (kerbals.Any(k => k.Key.name == member.name))
            {
                KCKerbalFacilityBase.findKerbal(Colony, member).Where(x => !(x is KCCrewQuarters)).ToList().ForEach(facility =>
                {
                    facility.Update();
                    facility.RemoveKerbal(member);
                });

                foreach (ProtoCrewMember key in kerbals.Where(kv => kv.Key.name == member.name).Select(kv => kv.Key).ToList())
                {
                    kerbals.Remove(key);
                };
            }
        }

        public override void Update()
        {
            base.Update();
        }

        public override void OnBuildingClicked()
        {
            if (crewQuartersWindow == null) crewQuartersWindow = new KCCrewQuartersWindow(this);
            crewQuartersWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            if (crewQuartersWindow == null) crewQuartersWindow = new KCCrewQuartersWindow(this);
            crewQuartersWindow.Toggle();
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
