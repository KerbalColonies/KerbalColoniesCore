using CommNet;
using KerbalKonstructs.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public class KCCommNetNodeInfo : IComparable<KCCommNetNodeInfo>, IComparer<KCCommNetNodeInfo>
    {
        public KerbalKonstructs.Core.GroupCenter GroupCenter { get; protected set; }
        public string Name { get; protected set; }
        public bool CustomName { get; protected set; } = false;
        public double Range { get; protected set; } = 500000d;
        public bool Enabled { get; protected set; } = true;
        public int FacilityLevel { get; protected set; } = 0;

        public void SetCustomName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }
            Name = name;
            CustomName = true;
            GroupCenter.gameObject.GetComponents<CommNetHome>().ToList().ForEach(node => UnityEngine.Object.Destroy(node));
            CommNetHome commNetStation = GroupCenter.gameObject.AddComponent<CommNetHome>();
            commNetStation.nodeName = Name;
            commNetStation.displaynodeName = Name;
            commNetStation.enabled = true;
            commNetStation.antennaPower = Range;
            commNetStation.isKSC = false;
            commNetStation.isPermanent = false;
            CommNet.CommNetNetwork.Reset();

        }

        public void SetRange(double range)
        {
            if (range <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(range), "Range must be greater than zero.");
            }
            Range = range;
            GroupCenter.gameObject.GetComponents<CommNetHome>().ToList().ForEach(node => UnityEngine.Object.Destroy(node));
            CommNetHome commNetStation = GroupCenter.gameObject.AddComponent<CommNetHome>();
            commNetStation.nodeName = Name;
            commNetStation.displaynodeName = Name;
            commNetStation.enabled = true;
            commNetStation.antennaPower = Range;
            commNetStation.isKSC = false;
            commNetStation.isPermanent = false;
            CommNet.CommNetNetwork.Reset();

        }

        public void Enable()
        {
            if (Enabled) return;
            Configuration.writeDebug("Enabling CommNet Node: " + Name);
            Enabled = true;
            GroupCenter.gameObject.GetComponents<CommNetHome>().ToList().ForEach(node => UnityEngine.Object.Destroy(node));
            CommNetHome commNetStation = GroupCenter.gameObject.AddComponent<CommNetHome>();
            commNetStation.nodeName = Name;
            commNetStation.displaynodeName = Name;
            commNetStation.enabled = true;
            commNetStation.antennaPower = Range;
            commNetStation.isKSC = false;
            commNetStation.isPermanent = false;
            CommNet.CommNetNetwork.Reset();
        }

        public void Disable()
        {
            if (!Enabled) return;
            Configuration.writeDebug("Disabling CommNet Node: " + Name);
            Enabled = false;
            GroupCenter.gameObject.GetComponents<CommNetHome>().ToList().ForEach(node => UnityEngine.Object.Destroy(node));
            CommNet.CommNetNetwork.Reset();
        }

        public ConfigNode GetConfigNode()
        {
            ConfigNode node = new ConfigNode("CommNetNode");
            node.AddValue("groupCenter", GroupCenter.Group);
            node.AddValue("name", Name);
            node.AddValue("range", Range);
            node.AddValue("enabled", Enabled);
            if (CustomName) node.AddValue("customName", true);
            node.AddValue("facilityLevel", FacilityLevel);
            return node;
        }

        public int CompareTo(KCCommNetNodeInfo other)
        {
            if (ReferenceEquals(null, other)) return 1; // null is less than any instance
            else if (ReferenceEquals(this, other)) return 0; // same instance
            else return FacilityLevel.CompareTo(other.FacilityLevel);
        }

        public int Compare(KCCommNetNodeInfo x, KCCommNetNodeInfo y)
        {
            if (ReferenceEquals(x, y)) return 0; // same instance
            if (ReferenceEquals(x, null)) return -1; // null is less than any instance
            if (ReferenceEquals(y, null)) return 1; // any instance is greater than null
            return x.FacilityLevel.CompareTo(y.FacilityLevel);
        }

        public KCCommNetNodeInfo(KCFacilityBase facility, ConfigNode node)
        {
            string groupCenterName = node.GetValue("groupCenter");
            GroupCenter = KerbalKonstructs.API.GetGroupCenter(facility.KKgroups.First(g => g == groupCenterName), facility.Colony.BodyName);
            Name = node.GetValue("name");
            Range = double.Parse(node.GetValue("range"));
            Enabled = bool.Parse(node.GetValue("enabled"));
            if (node.HasValue("customName")) CustomName = true;
            else CustomName = false;
            FacilityLevel = int.Parse(node.GetValue("facilityLevel"));

            GroupCenter.gameObject.GetComponents<CommNetHome>().ToList().ForEach(c => UnityEngine.Object.Destroy(c));
            CommNetHome commNetNode = GroupCenter.gameObject.AddComponent<CommNetHome>();
            commNetNode.nodeName = Name;
            commNetNode.displaynodeName = Name;
            commNetNode.enabled = true;
            commNetNode.antennaPower = Range;
            commNetNode.isKSC = false;
            commNetNode.isPermanent = false;
            CommNet.CommNetNetwork.Reset();
        }

        public KCCommNetNodeInfo(KerbalKonstructs.Core.GroupCenter groupCenter, string name = null, double range = 500000d, bool enabled = true, int facilityLevel = 0)
        {
            GroupCenter = groupCenter;
            if (name != null) Name = name;
            else Name = groupCenter.Group;
            Range = range;
            Enabled = enabled;
            FacilityLevel = facilityLevel;

            groupCenter.gameObject.GetComponents<CommNetHome>().ToList().ForEach(node => UnityEngine.Object.Destroy(node));
            CommNetHome commNetNode = groupCenter.gameObject.AddComponent<CommNetHome>();
            commNetNode.nodeName = Name;
            commNetNode.displaynodeName = Name;
            commNetNode.enabled = true;
            commNetNode.antennaPower = Range;
            commNetNode.isKSC = false;
            commNetNode.isPermanent = false;
            CommNet.CommNetNetwork.Reset();
        }
    }
}
