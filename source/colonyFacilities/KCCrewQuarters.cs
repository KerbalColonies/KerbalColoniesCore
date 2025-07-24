using Experience;
using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities
{
    internal class KCCrewQuartersWindow : KCFacilityWindowBase
    {
        KCCrewQuarters CrewQuarterFacility;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            GUILayout.BeginVertical();
            kerbalGUI.StaffingInterface();
            if (facility.facilityInfo.ECperSecond[facility.level] > 0)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"EC Consumption Priority: {CrewQuarterFacility.ECConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) CrewQuarterFacility.ECConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) CrewQuarterFacility.ECConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
            }
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

        public KCCrewQuartersWindow(KCCrewQuarters CrewQuarterFacility) : base(CrewQuarterFacility, Configuration.createWindowID())
        {
            this.CrewQuarterFacility = CrewQuarterFacility;
            this.kerbalGUI = new KerbalGUI(CrewQuarterFacility, false);
            toolRect = new Rect(100, 100, 400, 600);
        }
    }

    internal class KCCrewQuarters : KCKerbalFacilityBase, KCECConsumer
    {
        private static Dictionary<colonyClass, Vector2> CABInfoTraitScrollPos = new Dictionary<colonyClass, Vector2>();
        public static void CABDisplay(colonyClass colony)
        {
            if (!CABInfoTraitScrollPos.ContainsKey(colony)) CABInfoTraitScrollPos.Add(colony, Vector2.zero);

            GUILayout.Space(10);

            List<ProtoCrewMember> kerbals = GetAllKerbalsInColony(colony).Keys.ToList();
            Dictionary<ExperienceTraitConfig, int> traitCounts = new Dictionary<ExperienceTraitConfig, int>();
            kerbals.ForEach(k =>
            {
                if (traitCounts.ContainsKey(k.experienceTrait.Config)) traitCounts[k.experienceTrait.Config]++;
                else traitCounts.Add(k.experienceTrait.Config, 1);
            });
            traitCounts.ToList().Sort((x, y) => x.Key.Title.CompareTo(y.Key.Title));


            GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth), GUILayout.Height(traitCounts.Count > 0 ? 100 : 70));
            {
                GUILayout.Label($"<b>Crew Quarters:</b>");

                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth / 2 - 10));
                    {
                        GUILayout.Label($"Kerbals: {ColonyKerbalCount(colony)}/{ColonyKerbalCapacity(colony)}");
                        GUILayout.Label($"Crew quarters: {CrewQuartersInColony(colony).Count}");
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth / 2 - 10));
                    {

                        if (traitCounts.Count > 0)
                        {
                            CABInfoTraitScrollPos[colony] = GUILayout.BeginScrollView(CABInfoTraitScrollPos[colony], GUILayout.Height(100));
                            {
                                traitCounts.ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.Title}: {kvp.Value}"));
                            }
                            GUILayout.EndScrollView();
                        }

                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();
        }

        public static List<KCCrewQuarters> CrewQuartersInColony(colonyClass colony)
        {
            return colony.Facilities.Where(f => f is KCCrewQuarters).Select(f => (KCCrewQuarters)f).ToList();
        }

        public static int ColonyKerbalCapacity(colonyClass colony) => CrewQuartersInColony(colony).Sum(crewQuarter => crewQuarter.MaxKerbals);
        public static int ColonyKerbalCount(colonyClass colony) => CrewQuartersInColony(colony).Sum(crewQuarter => crewQuarter.kerbals.Count);

        public static KCCrewQuarters FindKerbalInCrewQuarters(colonyClass colony, ProtoCrewMember kerbal)
        {
            List<KCKerbalFacilityBase> facilitiesWithKerbal = KCKerbalFacilityBase.findKerbal(colony, kerbal);
            return (KCCrewQuarters)(facilitiesWithKerbal.Where(fac => fac is KCCrewQuarters).FirstOrDefault());
        }

        public static bool AddKerbalToColony(colonyClass colony, ProtoCrewMember kerbal)
        {
            if (FindKerbalInCrewQuarters(colony, kerbal) != null) { return false; }

            foreach (KCCrewQuarters crewQuarter in CrewQuartersInColony(colony))
            {
                if (crewQuarter.kerbals.Count < crewQuarter.MaxKerbals)
                {
                    Configuration.writeDebug($"Adding {kerbal.name} to {crewQuarter.name}");
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

                kerbals.Remove(kerbals.First(kerbal => kerbal.Key.name == member.name).Key);
            }
        }

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
            if (!HighLogic.LoadedSceneIsFlight) kerbals.Keys.ToList().ForEach(kerbal => kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned);

            enabled = built && !OutOfEC;
            if (crewQuartersWindow == null) crewQuartersWindow = new KCCrewQuartersWindow(this);
            crewQuartersWindow.kerbalGUI.DisableTransferWindow = !enabled;
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

        public bool OutOfEC { get; set; }
        public int ECConsumptionPriority { get; set; } = 0;
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => enabled && kerbals.Count > 0 || OutOfEC ? facilityInfo.ECperSecond[level] * deltaTime : 0;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => OutOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => OutOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;

        public override string GetFacilityProductionDisplay() => $"{kerbals.Count} / {MaxKerbals} kerbals assigned{(facilityInfo.ECperSecond[level] > 0 ? $"\nEC/s: {facilityInfo.ECperSecond[level]}" : "")}";

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("ECConsumptionPriority", ECConsumptionPriority);
            return node;
        }

        public KCCrewQuarters(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            this.crewQuartersWindow = null;
            if (HighLogic.LoadedSceneIsFlight) kerbals.Keys.ToList().ForEach(kerbal => kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available);
            else kerbals.Keys.ToList().ForEach(kerbal => kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned);
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecPriority)) ECConsumptionPriority = ecPriority;
        }

        public KCCrewQuarters(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, true)
        {
            this.crewQuartersWindow = null;
        }
    }
}
