using KerbalColonies.colonyFacilities.Commnet;
using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System.Collections.Generic;
using UniLinq;
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
    public class KCCommNetWindow : KCFacilityWindowBase
    {
        KCCommNetFacility commNetFacility;

        bool changeLaunchpadName = false;
        KCCommNetNodeInfo targetInstance;
        string newName;
        Vector2 scrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();

            GUILayout.Label($"Commnet nodes from this facility:");
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                commNetFacility.commNetNodes.ToList().ForEach(node =>
                {
                    if (GUILayout.Button($"{node.FacilityLevel}: {node.Name}", UIConfig.ButtonNoBG))
                    {
                        changeLaunchpadName = true;
                        targetInstance = node;
                        newName = node.Name;
                    }
                });
            }
            GUILayout.EndScrollView();

            if (changeLaunchpadName)
            {
                GUILayout.Label($"Changing name of commnet node {targetInstance.Name}:");
                newName = GUILayout.TextField(newName);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("OK", GUILayout.Height(23)))
                    {
                        targetInstance.SetCustomName(newName);
                        changeLaunchpadName = false;
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Height(23)))
                    {
                        changeLaunchpadName = false;
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (facility.facilityInfo.ECperSecond[facility.level] > 0)
            {
                GUILayout.Label($"EC/s: {facility.facilityInfo.ECperSecond[facility.level]}");
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"EC Consumption Priority: {commNetFacility.ECConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) commNetFacility.ECConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) commNetFacility.ECConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
            }
        }

        public KCCommNetWindow(KCCommNetFacility commNetFacility) : base(commNetFacility, Configuration.createWindowID())
        {
            this.commNetFacility = commNetFacility;

            toolRect = new Rect(100, 100, 400, 600);
        }
    }

    public class KCCommNetFacility : KCFacilityBase, KCECConsumer
    {
        public SortedSet<KCCommNetNodeInfo> commNetNodes { get; set; } = new SortedSet<KCCommNetNodeInfo>();
        protected KCCommNetWindow commNetWindow;
        public KCCommnetInfo commNetInfo => (KCCommnetInfo)facilityInfo;
        public bool rebuildCommNetNodes { get; set; } = false;

        public override void OnGroupPlaced(KerbalKonstructs.Core.GroupCenter kkgroup)
        {
            Configuration.writeLog($"KC CommNetFacility: OnGroupPlaced {facilityInfo.BasegroupNames[level]}");

            double newRange = commNetInfo.range[level];

            KCCommNetNodeInfo oldNode = commNetNodes.FirstOrDefault(node => node.GroupCenter == kkgroup);
            if (oldNode != null)
            {
                Configuration.writeLog($"KC CommNetFacility: Found existing CommNet node for {kkgroup.Group}, updating range to {newRange}");
                oldNode.SetRange(newRange);
                return;
            }

            KCCommNetNodeInfo newNode = new KCCommNetNodeInfo(kkgroup, null, newRange, true, level);
            commNetNodes.Add(newNode);

            CommNet.CommNetNetwork.Reset();
        }

        //public override string GetFacilityProductionDisplay() => $"Ground station range: {KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID)?.myFacilities[0]?.GetType().GetProperty("TrackingShort")?.GetValue(KerbalKonstructs.API.getStaticInstanceByUUID(groundstationUUID).myFacilities[0])}m";

        public override void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();

            if (built && !outOfEC)
            {
                if (!enabled)
                {
                    enabled = true;
                    commNetNodes.ToList().ForEach(node => node.Enable());
                }
            }
            else if (enabled)
            {
                enabled = false;
                commNetNodes.ToList().ForEach(node => node.Disable());
            }

            if (rebuildCommNetNodes)
            {
                rebuildCommNetNodes = false;
                commNetNodes.ToList().ForEach(node => node.SetRange(commNetInfo.range[node.FacilityLevel]));
            }
        }

        public override void OnBuildingClicked()
        {
            commNetWindow.Toggle();
        }
        public override void OnRemoteClicked()
        {
            commNetWindow.Toggle();
        }

        public bool outOfEC { get; protected set; } = false;
        public int ECConsumptionPriority { get; set; } = 0;
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => enabled || outOfEC ? facilityInfo.ECperSecond[level] * deltaTime : 0;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => outOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => outOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            commNetNodes.ToList().ForEach(comm => node.AddNode(comm.GetConfigNode()));

            return node;
        }

        public KCCommNetFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            node.GetNodes("CommNetNode").ToList().ForEach(n =>
            {
                KCCommNetNodeInfo commNetNode = new KCCommNetNodeInfo(this, n);
                commNetNodes.Add(commNetNode);
            });

            AllowClick = false;
            AllowRemote = false;

            commNetWindow = new KCCommNetWindow(this);
        }

        public KCCommNetFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            AllowClick = false;
            AllowRemote = false;

            commNetWindow = new KCCommNetWindow(this);
        }
    }
}
