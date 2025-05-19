using System;
using System.Collections.Generic;
using System.Linq;

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
    public class KCKerbalFacilityInfoClass : KCFacilityInfoClass
    {
        public SortedDictionary<int, int> maxKerbalsPerLevel = new SortedDictionary<int, int> { };
        public SortedDictionary<int, List<string>> allowedTraits = new SortedDictionary<int, List<string>> { };
        public SortedDictionary<int, List<string>> forbiddenTraits = new SortedDictionary<int, List<string>> { };

        public KCKerbalFacilityInfoClass(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                if (n.Value.HasValue("maxKerbals")) maxKerbalsPerLevel.Add(n.Key, int.Parse(n.Value.GetValue("maxKerbals")));
                else if (n.Key > 0) maxKerbalsPerLevel.Add(n.Key, maxKerbalsPerLevel[n.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no maxKerbals (at least for level 0).");

                if (n.Value.HasValue("allowedTraits")) allowedTraits[n.Key] = n.Value.GetValue("allowedTraits").Split(',').ToList().Select(s => s.Trim().ToLower()).ToList();
                else if (n.Key > 0) allowedTraits[n.Key] = allowedTraits[n.Key - 1];
                else allowedTraits[n.Key] = new List<string> { };

                if (n.Value.HasValue("forbiddenTraits")) forbiddenTraits[n.Key] = n.Value.GetValue("forbiddenTraits").Split(',').ToList().Select(s => s.Trim().ToLower()).ToList();
                else if (n.Key > 0) forbiddenTraits[n.Key] = forbiddenTraits[n.Key - 1];
                else forbiddenTraits[n.Key] = new List<string> { };
            });
        }
    }

    public class KCProtoCrewMemberComparer : IEqualityComparer<ProtoCrewMember>
    {
        public bool Equals(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) return true;
            else if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            return x.name == y.name;
        }
        public int GetHashCode(ProtoCrewMember obj)
        {
            return obj.name.GetHashCode();
        }
    }

    public abstract class KCKerbalFacilityBase : KCFacilityBase
    {

        /// <summary>
        /// Returns a list of all kerbals in the Colony that are registered in a crew quarter
        /// </summary>
        /// <returns>An empty dictionary if any of the parameters are invalid, no KCCrewQuarter facilities exist or no KCCrewQuarter has any kerbals assigned</returns>
        public static Dictionary<ProtoCrewMember, int> GetAllKerbalsInColony(colonyClass colony)
        {
            Dictionary<ProtoCrewMember, int> kerbals = new Dictionary<ProtoCrewMember, int> { };
            KCCrewQuarters.CrewQuartersInColony(colony).ForEach(crewQuarter =>
            {
                foreach (KeyValuePair<ProtoCrewMember, int> item in crewQuarter.kerbals)
                {
                    if (!kerbals.Any(x => x.Key.name == item.Key.name)) kerbals.Add(item.Key, item.Value);
                }
            });

            return kerbals;
        }

        /// <summary>
        /// Returns a list of all facilities that the kerbal is assigned to
        /// </summary>
        /// <returns>Returns an empty list if any of the parameters are invalid or the kerbal wasn't found</returns>
        public static List<KCKerbalFacilityBase> findKerbal(colonyClass colony, ProtoCrewMember kerbal)
        {
            return colony.Facilities.Where(f => f is KCKerbalFacilityBase).Select(f => (KCKerbalFacilityBase)f).Where(f => f.kerbals.Select(k => k.Key.name).ToList().Contains(kerbal.name)).ToList();
        }

        public int MaxKerbals { get { return kerbalFacilityInfoClass.maxKerbalsPerLevel[level]; } }

        public KCKerbalFacilityInfoClass kerbalFacilityInfoClass { get { return (KCKerbalFacilityInfoClass)facilityInfo; } }

        /// <summary>
        /// A list of kerbals in the facility and their current status
        /// <para>Value: 0 means unassigned, the other values are custom</para>
        /// <para>Kerbals with value 0 can get removed from the facility, e.g. to add them to a different facility or retrive them</para>
        /// <para>Don't remove the kerbals from the crewquarters to assign them, only change the availability in the crewquarters</para>
        /// </summary>
        protected Dictionary<ProtoCrewMember, int> kerbals;

        public bool modifyKerbal(ProtoCrewMember kerbal, int status)
        {
            if (kerbals.Any(x => x.Key.name == kerbal.name))
            {
                kerbals[kerbals.First(x => x.Key.name == kerbal.name).Key] = status;
                return true;
            }
            return false;
        }

        public List<ProtoCrewMember> getKerbals() { return kerbals.Keys.ToList(); }
        public virtual void RemoveKerbal(ProtoCrewMember member)
        {
            foreach (ProtoCrewMember key in kerbals.Where(kv => kv.Key.name == member.name).Select(kv => kv.Key).ToList())
            {
                kerbals.Remove(key);
            };
        }

        public virtual void AddKerbal(ProtoCrewMember member) { if (!kerbals.TryAdd(member, 0)) kerbals[member] = 0; }

        public virtual List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            // Either the trait is not in forbiddenTraits and it's not set to only allow specific traits or the trait is in allowedTraits
            return kerbals.Where(k => kerbalFacilityInfoClass.allowedTraits[level].Count == 0 ?
            !kerbalFacilityInfoClass.forbiddenTraits[level].Any(s => s.Contains(k.experienceTrait.Title.ToLower()))
            : kerbalFacilityInfoClass.allowedTraits[level].Any(s => s.Contains(k.experienceTrait.Title.ToLower()))
            ).ToList();
        }

        public ConfigNode createKerbalNode()
        {
            ConfigNode kerbalsNode = new ConfigNode("KerbalNode");

            foreach (KeyValuePair<ProtoCrewMember, int> kerbal in kerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("Kerbal");

                kerbalNode.AddValue("name", kerbal.Key.name);
                kerbalNode.AddValue("status", kerbal.Value);

                kerbalsNode.AddNode(kerbalNode);
            }

            return kerbalsNode;
        }

        public void loadKerbalNode(ConfigNode kerbalNode)
        {
            if (kerbalNode != null)
            {
                kerbals = new Dictionary<ProtoCrewMember, int> { };

                foreach (ConfigNode kerbal in kerbalNode.GetNodes())
                {
                    string kName = kerbal.GetValue("name");
                    int kStatus = Convert.ToInt32(kerbal.GetValue("status"));
                    foreach (ProtoCrewMember k in HighLogic.CurrentGame.CrewRoster.Crew)
                    {
                        if (k.name == kName)
                        {
                            kerbals.Add(k, kStatus);
                            break;
                        }
                    }
                }
            }
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddNode(createKerbalNode());
            return node;
        }

        public KCKerbalFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            loadKerbalNode(node.GetNode("KerbalNode"));
        }

        public KCKerbalFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            kerbals = new Dictionary<ProtoCrewMember, int> { };
        }
    }
}