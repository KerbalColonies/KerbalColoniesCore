using CommNet.Network;
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
                    if (!kerbals.ContainsKey(item.Key)) kerbals.Add(item.Key, item.Value);
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
            return colony.Facilities.Where(f => f is KCKerbalFacilityBase).Select(f => (KCKerbalFacilityBase)f).Where(f => f.kerbals.Keys.Contains(kerbal)).ToList();
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
            if (kerbals.ContainsKey(kerbal))
            {
                kerbals[kerbal] = status;
                return true;
            }
            return false;
        }

        public List<ProtoCrewMember> getKerbals() { return kerbals.Keys.ToList(); }
        public virtual void RemoveKerbal(ProtoCrewMember member) { kerbals.Remove(member); }
        public virtual void AddKerbal(ProtoCrewMember member) { kerbals.Add(member, 0); }

        public virtual List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals;
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
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= level; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());

                if (iLevel.HasValue("maxKerbals")) maxKerbalsPerLevel[i] = int.Parse(iLevel.GetValue("maxKerbals"));
                else if (i > 0) maxKerbalsPerLevel[i] = maxKerbalsPerLevel[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxKerbals (at least for level 0).");
            }
            loadKerbalNode(node.GetNode("KerbalNode"));
        }

        public KCKerbalFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= level; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());

                if (iLevel.HasValue("maxKerbals")) maxKerbalsPerLevel[i] = int.Parse(iLevel.GetValue("maxKerbals"));
                else if (i > 0) maxKerbalsPerLevel[i] = maxKerbalsPerLevel[i - 1];
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no maxKerbals (at least for level 0).");
            }

            kerbals = new Dictionary<ProtoCrewMember, int> { };
        }
    }
}