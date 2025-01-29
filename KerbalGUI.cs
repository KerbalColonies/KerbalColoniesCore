using KerbalColonies.colonyFacilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies
{
    internal class KerbalSelectorGUI : KCWindowBase
    {
        public enum SwitchModes
        {
            ActiveVessel,
            Colony,
            Facility
        }
        public SwitchModes mode = SwitchModes.ActiveVessel;

        private KCKerbalFacilityBase fromFac;
        private KCKerbalFacilityBase toFac;
        private string fromName;
        private string toName;
        private KerbalGUI kGUI;
        private List<ProtoCrewMember> fromList;
        private List<ProtoCrewMember> toList;
        Vessel toVessel;
        private int fromCapacity;
        private int toCapacity;
        List<ProtoCrewMember> fromListModifierList = new List<ProtoCrewMember>() { }; // Kerbals to be removed from the vessel
        List<ProtoCrewMember> toListModifierList = new List<ProtoCrewMember>() { }; // Kerbals to be added to the vessel

        string saveGame;
        int bodyIndex;
        string colonyName;

        protected override void OnClose()
        {
            foreach (ProtoCrewMember member in fromListModifierList)
            {
                fromFac.AddKerbal(member);

                if (mode == SwitchModes.ActiveVessel)
                {

                    InternalSeat seat = member.seat;
                    seat.part.RemoveCrewmember(member); // Remove from seat
                    member.seat = null;

                    foreach (Part p in toVessel.Parts)
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

                    toVessel.RemoveCrew(member);

                    member.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                    HighLogic.CurrentGame.CrewRoster.AddCrewMember(member);

                    toVessel.SpawnCrew();
                }
                else if (mode == SwitchModes.Colony)
                {
                    KCCrewQuarters.FindKerbalInCrewQuarters(saveGame, bodyIndex, colonyName, member).modifyKerbal(member, 1);
                }

            }
            fromListModifierList.Clear();

            foreach (ProtoCrewMember member in toListModifierList)
            {
                fromFac.RemoveKerbal(member);

                if (mode == SwitchModes.ActiveVessel)
                {
                    foreach (Part p in toVessel.Parts)
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
                    //toVessel.RebuildCrewList();
                    toVessel.RebuildCrewList();
                    toVessel.SpawnCrew();
                    Vessel.CrewWasModified(toVessel);
                    Game currentGame = HighLogic.CurrentGame.Updated();
                }
                else if (mode == SwitchModes.Colony)
                {
                    KCCrewQuarters.FindKerbalInCrewQuarters(saveGame, bodyIndex, colonyName, member).modifyKerbal(member, 0);
                }
            }
            toListModifierList.Clear();

            kGUI.transferWindow = false;

            Configuration.saveColonies = true;
        }

        protected override void CustomWindow()
        {
            switch (mode)
            {
                case SwitchModes.ActiveVessel:
                    this.fromList = fromFac.filterKerbals(toVessel.GetVesselCrew());
                    this.toList = new List<ProtoCrewMember>(fromFac.getKerbals());
                    break;
                case SwitchModes.Colony:
                    this.fromList = fromFac.filterKerbals(KCKerbalFacilityBase.GetAllKerbalsInColony(saveGame, bodyIndex, colonyName).Where(kvp => kvp.Value == 0).ToDictionary(i => i.Key, i => i.Value).Keys.ToList());
                    this.toList = fromFac.getKerbals();
                    break;
            }


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
                    if (toList.Count + 1 <= fromCapacity)
                    {
                        Configuration.writeDebug(k.name);
                        fromListModifier = k;
                        fromListModifierList.Add(k);
                        toList.Add(k);
                    }
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
                    if (fromList.Count + 1 <= toCapacity)
                    {
                        Configuration.writeDebug(k.name);
                        toListModifier = k;
                        toListModifierList.Add(k);
                        fromList.Add(k);
                    }
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

        internal KerbalSelectorGUI(KCKerbalFacilityBase fac, KerbalGUI kGUI, string fromName, string toName, Vessel fromVessel) : base(Configuration.createWindowID(fac), fac.name)
        {
            this.fromFac = fac;
            toolRect = new Rect(100, 100, 500, 500);
            this.fromName = fromName;
            this.toName = toName;
            this.fromList = fac.filterKerbals(fromVessel.GetVesselCrew());
            this.toVessel = fromVessel;
            this.kGUI = kGUI;
            this.toList = new List<ProtoCrewMember>(fromFac.getKerbals());
            this.mode = SwitchModes.ActiveVessel;
            this.fromCapacity = fac.maxKerbals;
            this.toCapacity = fromVessel.GetCrewCapacity();
        }

        internal KerbalSelectorGUI(KCKerbalFacilityBase fac, KerbalGUI kGUI, string saveGame, int bodyIndex, string colonyName, string fromName, string toName) : base(Configuration.createWindowID(fac), fac.name)
        {
            this.fromFac = fac;
            toolRect = new Rect(100, 100, 500, 500);
            this.fromName = fromName;
            this.toName = toName;
            this.fromList = fac.filterKerbals(KCKerbalFacilityBase.GetAllKerbalsInColony(saveGame, bodyIndex, colonyName).Where(kvp => kvp.Value == 0).ToDictionary(i => i.Key, i => i.Value).Keys.ToList());
            this.toList = fac.getKerbals();
            this.saveGame = saveGame;
            this.bodyIndex = bodyIndex;
            this.colonyName = colonyName;
            this.kGUI = kGUI;
            this.mode = SwitchModes.Colony;
            this.fromCapacity = KCCrewQuarters.ColonyKerbalCapacity(saveGame, bodyIndex, colonyName);
            this.toCapacity = fac.maxKerbals;
        }
    }

    internal class KerbalGUI
    {
        public static GUIStyle LabelInfo;
        public static GUIStyle BoxInfo;
        public static GUIStyle ButtonSmallText;

        private Vector2 scrollPos;
        internal KCKerbalFacilityBase fac;
        public bool transferWindow;

        public static Texture tKerbal = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/billeted", false);
        public static Texture tNoKerbal = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/unbilleted", false);
        public static Texture tXPGained = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/xpgained", false);
        public static Texture tXPUngained = GameDatabase.Instance.GetTexture("KerbalKonstructs/Assets/xpungained", false);
        public KerbalSelectorGUI ksg;

        public void StaffingInterface()
        {
            KSPLog.print("StaffingInterface: " + this.ToString());

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

            if (fac.maxKerbals > 0)
            {
                GUILayout.Space(5);

                float CountCurrent = fac.getKerbals().Count;
                float CountEmpty = fac.maxKerbals - CountCurrent;

                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(400), GUILayout.Height(400));
                {
                    foreach (ProtoCrewMember pcm in fac.getKerbals())
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(80));
                        GUILayout.Box(tKerbal, GUILayout.Width(23), GUILayout.Height(38));

                        GUILayout.BeginVertical();
                        GUILayout.Label(pcm.displayName, LabelInfo);
                        GUILayout.Label(pcm.trait, LabelInfo);
                        GUILayout.Label(pcm.gender.ToString(), LabelInfo);
                        GUILayout.Label($"Experiencelevel: {pcm.experienceLevel}", LabelInfo);
                        GUILayout.EndVertical();

                        GUILayout.EndHorizontal();
                    }

                    while (CountEmpty > 0)
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(80));

                        GUILayout.Box(tNoKerbal, GUILayout.Width(23), GUILayout.Height(38));
                        CountEmpty = CountEmpty - 1;
                        GUILayout.EndHorizontal();

                    }
                }
                GUILayout.EndScrollView();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Staff: " + kerbalCount.ToString("#0") + "/" + fac.maxKerbals.ToString("#0"), LabelInfo);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(2);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Assign/Retrive Kerbals", GUILayout.Height(23)))
                    {
                        if (transferWindow)
                        {
                            ksg.Close();
                            transferWindow = false;
                        }
                        else
                        {
                            ksg.Open();
                            transferWindow = true;
                        }
                    }
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
        }

        public KerbalGUI(KCKerbalFacilityBase fac)
        {
            transferWindow = false;
            this.fac = fac;

            this.ksg = new KerbalSelectorGUI(fac, this, "current ship", fac.name, FlightGlobals.ActiveVessel);
        }

        public KerbalGUI(KCKerbalFacilityBase fac, string savegame, int bodyIndex, string colonyName)
        {
            transferWindow = false;
            this.fac = fac;

            this.ksg = new KerbalSelectorGUI(fac, this, savegame, bodyIndex, colonyName, colonyName, fac.name);
        }
    }
}
