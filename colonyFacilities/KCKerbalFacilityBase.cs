using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities
{
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

        private Dictionary<int, int> maxKerbalsPerLevel = new Dictionary<int, int> { };

        public int MaxKerbals { get { return maxKerbalsPerLevel[level]; } }

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

        public virtual void AddKerbal(ProtoCrewMember member) { if(!kerbals.TryAdd(member, 0)) kerbals[member] = 0; }

        public Dictionary<int, List<string>> forbiddenTraits { get; private set; } = new Dictionary<int, List<string>> { };
        public Dictionary<int, List<string>> allowedTraits { get; private set; } = new Dictionary<int, List<string>> { };

        public virtual List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            // Either the trait is not in forbiddenTraits and it's not set to only allow specific traits or the trait is in allowedTraits
            return kerbals.Where(k => allowedTraits[level].Count == 0 ?
            !forbiddenTraits[level].Any(s => s.Contains(k.experienceTrait.Title.ToLower()))
            : allowedTraits[level].Any(s => s.Contains(k.experienceTrait.Title.ToLower()))
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

        private void configLoader(ConfigNode node)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());

                if (iLevel.HasValue("maxKerbals")) maxKerbalsPerLevel.Add(i, int.Parse(iLevel.GetValue("maxKerbals")));
                else if (i > 0) maxKerbalsPerLevel.Add(i, maxKerbalsPerLevel[i - 1]);
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxKerbals (at least for level 0).");

                if (iLevel.HasValue("allowedTraits")) allowedTraits[i] = iLevel.GetValue("allowedTraits").Split(',').ToList().Select(s => s.Trim().ToLower()).ToList();
                else if (i > 0) allowedTraits[i] = allowedTraits[i - 1];
                else allowedTraits[i] = new List<string> { };

                if (iLevel.HasValue("forbiddenTraits")) forbiddenTraits[i] = iLevel.GetValue("forbiddenTraits").Split(',').ToList().Select(s => s.Trim().ToLower()).ToList();
                else if (i > 0) forbiddenTraits[i] = forbiddenTraits[i - 1];
                else forbiddenTraits[i] = new List<string> { };
            }
        }

        public KCKerbalFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configLoader(facilityInfo.facilityConfig);
            loadKerbalNode(node.GetNode("KerbalNode"));
        }

        public KCKerbalFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configLoader(facilityInfo.facilityConfig);
            kerbals = new Dictionary<ProtoCrewMember, int> { };
        }
    }
}