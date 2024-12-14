using KerbalColonies.colonyFacilities;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalColonies
{
    internal class ActiveKerbalSelectorGUI : KCWindowBase
    {
        private string fromName;
        private string toName;
        private KerbalGUI kGUI;
        private List<ProtoCrewMember> fromList;
        private List<ProtoCrewMember> toList;
        List<ProtoCrewMember> fromListModifierList = new List<ProtoCrewMember>() { }; // Kerbals to be removed from the vessel
        List<ProtoCrewMember> toListModifierList = new List<ProtoCrewMember>() { }; // Kerbals to be added to the vessel

        protected override void OnClose()
        {
            foreach (ProtoCrewMember member in fromListModifierList)
            {
                kGUI.fac.AddKerbal(member);

                InternalSeat seat = member.seat;
                seat.part.RemoveCrewmember(member); // Remove from seat
                member.seat = null;

                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    if (p.protoModuleCrew.Contains(member))
                    {
                        p.protoModuleCrew.Remove(member);
                        int index = p.protoPartSnapshot.GetCrewIndex(member.name);
                        Configuration.writeDebug(index.ToString());
                        Configuration.writeDebug(member.seatIdx.ToString());
                        p.protoPartSnapshot.RemoveCrew(member);
                        p.RemoveCrewmember(member);
                        p.ModulesOnUpdate();
                        break;
                    }
                }

                FlightGlobals.ActiveVessel.RemoveCrew(member);

                member.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                HighLogic.CurrentGame.CrewRoster.AddCrewMember(member);

                Vessel active = FlightGlobals.ActiveVessel;

                FlightGlobals.ActiveVessel.SpawnCrew();
            }
            fromListModifierList.Clear();

            foreach (ProtoCrewMember member in toListModifierList)
            {
                kGUI.fac.RemoveKerbal(member);

                Vessel active = FlightGlobals.ActiveVessel;

                foreach (Part p in active.Parts)
                {
                    if (p.CrewCapacity > 0)
                    {
                        if (p.protoModuleCrew.Count >= p.CrewCapacity)
                        {
                            continue;
                        }
                        List<int> freeSeats = new List<int>();
                        for (int i = 0; i < p.CrewCapacity; i++) { freeSeats.Add(i); }

                        foreach (ProtoCrewMember pcm in p.protoModuleCrew)
                        {
                            freeSeats.Remove(pcm.seatIdx);
                        }
                        if (freeSeats.Count > 0)
                        {
                            p.AddCrewmemberAt(member, freeSeats[0]);
                            p.RegisterCrew();
                            break;
                        }
                    }
                }

                member.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                //FlightGlobals.ActiveVessel.RebuildCrewList();
                active.RebuildCrewList();
                active.SpawnCrew();
                Game currentGame = HighLogic.CurrentGame.Updated();
            }
            toListModifierList.Clear();

            Configuration.SaveColonies("KCCD");
        }

        protected override void CustomWindow()
        {
            ProtoCrewMember fromListModifier = null;
            ProtoCrewMember toListModifier = null;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(fromName, LabelGreen);
            GUILayout.BeginScrollView(new Vector2());
            foreach (ProtoCrewMember k in fromList)
            {
                if (GUILayout.Button(k.name, GUILayout.Height(23)))
                {
                    KSPLog.print(k.name);
                    fromListModifier = k;
                    fromListModifierList.Add(k);
                    toList.Add(k);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label(toName, LabelGreen);
            GUILayout.BeginScrollView(new Vector2());
            foreach (ProtoCrewMember k in toList)
            {
                if (GUILayout.Button(k.name, GUILayout.Height(23)))
                {
                    //HighLogic.CurrentGame.CrewRoster.Kerbals
                    KSPLog.print(k.name);
                    toListModifier = k;
                    toListModifierList.Add(k);
                    fromList.Add(k);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (fromListModifier != null)
            {
                fromList.Remove(fromListModifier);
            }
            if (toListModifier != null)
            {
                toList.Remove(toListModifier);
            }
        }

        internal ActiveKerbalSelectorGUI(KCFacilityBase fac, KerbalGUI kGUI, string fromName, string toName) : base(Configuration.createWindowID(fac))
        {
            toolRect = new Rect(100, 100, 500, 500);
            this.fromName = fromName;
            this.toName = toName;
            this.fromList = FlightGlobals.ActiveVessel.GetVesselCrew();
            this.kGUI = kGUI;
            this.toList = new List<ProtoCrewMember>(kGUI.fac.getKerbals());
        }
    }

    internal class KerbalGUI
    {
        public float fXP;
        public static GUIStyle LabelInfo;
        public static GUIStyle BoxInfo;
        public static GUIStyle ButtonSmallText;

        public Vector2 scrollPos;
        internal KCKerbalFacilityBase fac;

        public static Texture tKerbal = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/billeted", false);
        public static Texture tNoKerbal = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/unbilleted", false);
        public static Texture tXPGained = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/xpgained", false);
        public static Texture tXPUngained = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/xpungained", false);

        public void StaffingInterface(int maxKerbals)
        {
            ActiveKerbalSelectorGUI ksg = new ActiveKerbalSelectorGUI(fac, this, "current ship", "facility");

            int kerbalCount = fac.getKerbals().Count;
            LabelInfo = new GUIStyle(GUI.skin.label);
            LabelInfo.normal.background = null;
            LabelInfo.normal.textColor = Color.white;
            LabelInfo.fontSize = 13;
            LabelInfo.fontStyle = FontStyle.Bold;
            LabelInfo.padding.left = 3;
            LabelInfo.padding.top = 0;
            LabelInfo.padding.bottom = 0;

            BoxInfo = new GUIStyle(GUI.skin.box);
            BoxInfo.normal.textColor = Color.cyan;
            BoxInfo.fontSize = 13;
            BoxInfo.padding.top = 2;
            BoxInfo.padding.bottom = 1;
            BoxInfo.padding.left = 5;
            BoxInfo.padding.right = 5;
            BoxInfo.normal.background = null;

            ButtonSmallText = new GUIStyle(GUI.skin.button);
            ButtonSmallText.fontSize = 12;
            ButtonSmallText.fontStyle = FontStyle.Normal;

            if (maxKerbals > 0)
            {
                GUILayout.Space(5);

                float CountCurrent = fac.getKerbals().Count;
                float CountEmpty = maxKerbals - CountCurrent;

                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(58));
                {
                    GUILayout.BeginHorizontal();
                    {
                        while (CountCurrent > 0)
                        {
                            GUILayout.Box(tKerbal, GUILayout.Width(23));
                            CountCurrent = CountCurrent - 1;
                        }

                        while (CountEmpty > 0)
                        {
                            GUILayout.Box(tNoKerbal, GUILayout.Width(23));
                            CountEmpty = CountEmpty - 1;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Staff: " + kerbalCount.ToString("#0") + "/" + maxKerbals.ToString("#0"), LabelInfo);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(2);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Assign/Retrive Kerbals", GUILayout.Height(23)))
                    {
                        ksg.Toggle();
                    }
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
        }

        public KerbalGUI(KCKerbalFacilityBase fac)
        {
            this.fac = fac;
        }
    }
}
