using KerbalColonies.ResourceManagment;
using KerbalColonies.UI;
using Smooth.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace KerbalColonies.colonyFacilities.Commnet
{
    public class KCGroundstationWindow : KCFacilityWindowBase
    {
        private KCGroundstationFacility groundStation;
        public KerbalGUI kerbalGUI;

        private bool changeNodeNode = false;
        private KCCommNetNodeInfo targetInstance;
        private string newName;
        private Vector2 scrollPos = Vector2.zero;
        private Vector2 resourceUsageScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            kerbalGUI ??= new KerbalGUI(groundStation, true);

            facility.Colony.UpdateColony();

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width((toolRect.width / 2) - 10));
                {
                    GUILayout.Label($"Commnet nodes from this facility:");
                    scrollPos = GUILayout.BeginScrollView(scrollPos);
                    {
                        groundStation.commNetNodes.ToList().ForEach(node =>
                        {
                            if (GUILayout.Button($"{node.FacilityLevel}: {node.Name}", UIConfig.ButtonNoBG))
                            {
                                changeNodeNode = true;
                                targetInstance = node;
                                newName = node.Name;
                            }
                        });
                    }
                    GUILayout.EndScrollView();

                    if (changeNodeNode)
                    {
                        GUILayout.Label($"Changing name of commnet node {targetInstance.Name}:");
                        newName = GUILayout.TextField(newName);

                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("OK", GUILayout.Height(23)))
                            {
                                targetInstance.SetCustomName(newName);
                                changeNodeNode = false;
                            }
                            if (GUILayout.Button("Cancel", GUILayout.Height(23)))
                            {
                                changeNodeNode = false;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                    if (facility.facilityInfo.ResourceUsage[facility.level].Count > 0)
                    {
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"Resource Consumption Priority: {groundStation.ResourceConsumptionPriority}", GUILayout.Height(18));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) groundStation.ResourceConsumptionPriority--;
                            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) groundStation.ResourceConsumptionPriority++;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Label("Resource usage:");
                        resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(120));
                        {
                            groundStation.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                                GUILayout.Label($"- {kvp.Key.displayName}: {kvp.Value}/s")
                            );
                        }
                        GUILayout.EndScrollView();
                    }
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width((toolRect.width / 2) - 10));
                {
                    kerbalGUI.StaffingInterface();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            groundStation.rebuildCommNetNodes = true;
        }

        public KCGroundstationWindow(KCGroundstationFacility groundStation) : base(groundStation, Configuration.createWindowID())
        {
            this.groundStation = groundStation;

            toolRect = new Rect(100, 100, 600, 600);
        }
    }


    public class KCGroundstationFacility : KCKerbalFacilityBase, IKCResourceConsumer
    {
        public SortedSet<KCCommNetNodeInfo> commNetNodes { get; set; } = [];
        public KCGroundstationWindow groundstationWindow { get; protected set; }
        public KCGroundStationInfo groundStationInfo => (KCGroundStationInfo)facilityInfo;
        public bool rebuildCommNetNodes { get; set; } = false;

        public override void OnGroupPlaced(KerbalKonstructs.Core.GroupCenter kkgroup)
        {
            Configuration.writeLog($"KC CommNetFacility: OnGroupPlaced {facilityInfo.BasegroupNames[level]}");

            double newRange = (groundStationInfo.range[level] + (groundStationInfo.kerbalRange[level] * kerbals.Count)) * (1 + (groundStationInfo.kerbalMultiplier[level] * kerbals.Count));

            KCCommNetNodeInfo oldNode = commNetNodes.FirstOrDefault(node => node.GroupCenter == kkgroup);
            if (oldNode != null)
            {
                Configuration.writeLog($"KC CommNetFacility: Found existing CommNet node for {kkgroup.Group}, updating range to 500000d");
                oldNode.SetRange(newRange);
                return;
            }

            KCCommNetNodeInfo newNode = new(kkgroup, null, newRange, true, facilityLevel: level);
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
                commNetNodes.ToList().ForEach(node =>
                    node.SetRange((groundStationInfo.range[node.FacilityLevel] + (groundStationInfo.kerbalRange[node.FacilityLevel] * kerbals.Count)) * (1 + (groundStationInfo.kerbalMultiplier[node.FacilityLevel] * kerbals.Count)))
                );
            }
        }

        public override void OnBuildingClicked()
        {
            groundstationWindow.Toggle();
        }
        public override void OnRemoteClicked()
        {
            groundstationWindow.Toggle();
        }

        public bool OutOfResources { get; protected set; } = false;
        public int ResourceConsumptionPriority { get; set; } = 0;

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => enabled || OutOfResources ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime) : [];

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime) => OutOfResources = false;

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            OutOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => enabled || OutOfResources ? facilityInfo.ResourceUsage[level].Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value) : [];


        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            commNetNodes.ToList().ForEach(comm => node.AddNode(comm.GetConfigNode()));

            return node;
        }

        public KCGroundstationFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            node.GetNodes("CommNetNode").ToList().ForEach(n =>
            {
                KCCommNetNodeInfo commNetNode = new(this, n);
                commNetNodes.Add(commNetNode);
            });

            AllowClick = false;
            AllowRemote = false;

            groundstationWindow = new KCGroundstationWindow(this);
        }

        public KCGroundstationFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            AllowClick = false;
            AllowRemote = false;

            groundstationWindow = new KCGroundstationWindow(this);
        }
    }
}
