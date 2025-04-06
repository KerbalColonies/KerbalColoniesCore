using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuarterCost : KCFacilityCostClass
    {
        public KCCrewQuarterCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, double>> {
                { 0, new Dictionary<PartResourceDefinition, double> { { PartResourceLibrary.Instance.GetDefinition("Ore"), 100 } } }
            };

        }
    }

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
            this.kerbalGUI = new KerbalGUI(CrewQuarterFacility);
            toolRect = new Rect(100, 100, 800, 1200);
        }
    }

    [System.Serializable]
    internal class KCCrewQuarters : KCKerbalFacilityBase
    {
        public static List<KCCrewQuarters> CrewQuartersInColony(int bodyIndex, string colonyName)
        {
            if (!Configuration.colonyDictionary.ContainsKey(bodyIndex)) { return new List<KCCrewQuarters> { }; }

            colonyClass colony = Configuration.colonyDictionary[bodyIndex].FirstOrDefault(c => c.Name == colonyName);
            if (colony == null) { return new List<KCCrewQuarters> { }; }

            List<KCCrewQuarters> crewQuarters = new List<KCCrewQuarters>();
            colony.Facilities.Where(f => typeof(KCCrewQuarters).IsAssignableFrom(f.GetType())).ToList().ForEach(f =>
            {
                crewQuarters.Add((KCCrewQuarters)f);
            });
            return crewQuarters;
        }

        public static int ColonyKerbalCapacity(int bodyIndex, string colonyName)
        {
            if (!Configuration.colonyDictionary.ContainsKey(bodyIndex)) { return 0; }
            else if (!Configuration.colonyDictionary[bodyIndex].Exists(c => c.Name == colonyName)) { return 0; }

            int i = 0;
            CrewQuartersInColony(bodyIndex, colonyName).ForEach(crewQuarter =>
            {
                i += crewQuarter.maxKerbals;
            });
            return i;
        }

        public static KCCrewQuarters FindKerbalInCrewQuarters(int bodyIndex, string colonyName, ProtoCrewMember kerbal)
        {
            List<KCKerbalFacilityBase> facilitiesWithKerbal = KCKerbalFacilityBase.findKerbal(bodyIndex, colonyName, kerbal);
            return (KCCrewQuarters) facilitiesWithKerbal.Where(fac => fac is KCCrewQuarters).FirstOrDefault();
        }

        public static bool AddKerbalToColony(int bodyIndex, string colonyName, ProtoCrewMember kerbal)
        {
            if (!Configuration.colonyDictionary.ContainsKey(bodyIndex)) { return false; }
            else if (!Configuration.colonyDictionary[bodyIndex].Exists(c => c.Name == colonyName)) { return false; }

            if (FindKerbalInCrewQuarters(bodyIndex, colonyName, kerbal) != null) { return false; }

            foreach (KCCrewQuarters crewQuarter in CrewQuartersInColony(bodyIndex, colonyName))
            {
                if (crewQuarter.kerbals.Count < crewQuarter.maxKerbals)
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
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
            KCCrewQuarters oldCrewQuarter = FindKerbalInCrewQuarters(bodyIndex, colonyName, kerbal);

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
                KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);

                KCKerbalFacilityBase.findKerbal(bodyIndex, colonyName, kerbal).Where(x => !(x is KCCrewQuarters)).ToList().ForEach(facility =>
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
            if (crewQuartersWindow.IsOpen())
            {
                crewQuartersWindow.Close();
                crewQuartersWindow.kerbalGUI.ksg.Close();
                crewQuartersWindow.kerbalGUI.transferWindow = false;
            }
            else
            {
                crewQuartersWindow.Open();
            }
        }

        public override void Initialize()
        {
            this.maxKerbals = 16;
            base.Initialize();
            this.crewQuartersWindow = new KCCrewQuartersWindow(this);
        }

        public KCCrewQuarters(bool enabled) : base("KCCrewQuarters", true, 16)
        {
        }
    }
}
