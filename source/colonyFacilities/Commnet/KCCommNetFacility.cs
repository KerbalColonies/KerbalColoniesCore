using KerbalColonies.colonyFacilities.Commnet;
using KerbalColonies.ResourceManagment;
using KerbalColonies.UI;
using Smooth.Collections;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/


namespace KerbalColonies.colonyFacilities
{
    public class KCCommNetWindow : KCFacilityWindowBase
    {
        KCCommNetFacility commNetFacility;

        bool changeNodeName = false;
        KCCommNetNodeInfo targetInstance;
        string newName;
        Vector2 scrollPos = Vector2.zero;
        Vector2 resourceUsageScrollPos = Vector2.zero;
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
                        changeNodeName = true;
                        targetInstance = node;
                        newName = node.Name;
                    }
                });
            }
            GUILayout.EndScrollView();

            if (changeNodeName)
            {
                GUILayout.Label($"Changing name of commnet node {targetInstance.Name}:");
                newName = GUILayout.TextField(newName);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("OK", GUILayout.Height(23)))
                    {
                        targetInstance.SetCustomName(newName);
                        changeNodeName = false;
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Height(23)))
                    {
                        changeNodeName = false;
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (facility.facilityInfo.ResourceUsage[facility.level].Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Resource Consumption Priority: {commNetFacility.ResourceConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) commNetFacility.ResourceConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) commNetFacility.ResourceConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Resource usage:");
                resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(120));
                {
                    commNetFacility.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                        GUILayout.Label($"- {kvp.Key.displayName}: {kvp.Value}/s")
                    );
                }
                GUILayout.EndScrollView();
            }
        }

        public KCCommNetWindow(KCCommNetFacility commNetFacility) : base(commNetFacility, Configuration.createWindowID())
        {
            this.commNetFacility = commNetFacility;

            toolRect = new Rect(100, 100, 400, 600);
        }
    }

    public class KCCommNetFacility : KCFacilityBase, IKCResourceConsumer
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

            if (built && !OutOfResources)
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

        public bool OutOfResources { get; protected set; } = false;
        public int ResourceConsumptionPriority { get; set; } = 0;

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => enabled || OutOfResources ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime) : new Dictionary<PartResourceDefinition, double> { };

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime) => OutOfResources = false;

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            OutOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => enabled || OutOfResources ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value) : new Dictionary<PartResourceDefinition, double> { };

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
