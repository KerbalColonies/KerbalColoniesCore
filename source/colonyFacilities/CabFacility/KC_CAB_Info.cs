using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.CabFacility
{
    public class KC_CAB_Info : KCFacilityInfoClass
    {
        /// <summary>
        /// All of the default facilties that are queued to be placed after the cab is placed.
        /// </summary>
        public Dictionary<KCFacilityInfoClass, int> defaultFacilities { get; protected set; } = new Dictionary<KCFacilityInfoClass, int>();
        /// <summary>
        /// All of the default facilties that are queued to be placed before the cab is placed.
        /// </summary>
        public Dictionary<KCFacilityInfoClass, int> priorityDefaultFacilities { get; protected set; } = new Dictionary<KCFacilityInfoClass, int>();
        public void addDefaultFacility(KCFacilityInfoClass facilityInfo, int amount)
        {
            if (!defaultFacilities.Any(c => facilityInfo.name == c.Key.name))
            {
                defaultFacilities.Add(facilityInfo, amount);
            }
        }
        public void addPriorityDefaultFacility(KCFacilityInfoClass facilityInfo, int amount)
        {
            if (!priorityDefaultFacilities.Any(c => facilityInfo.name == c.Key.name))
            {
                priorityDefaultFacilities.Add(facilityInfo, amount);
            }
        }

        public Dictionary<string, int> defaultFacilityStrings { get; protected set; } = new Dictionary<string, int>();
        public Dictionary<string, int> priorityDefaultFacilityStrings { get; protected set; } = new Dictionary<string, int>();
        
        public SortedDictionary<int, float> EditorRange { get; protected set; } = new SortedDictionary<int, float>();

        public override void lateInit()
        {
            priorityDefaultFacilityStrings.ToList().ForEach(priorityFac =>
            {
                KCFacilityInfoClass priorityInfo = Configuration.GetInfoClass(priorityFac.Key);
                if (priorityInfo == null) throw new MissingFieldException($"The priority facility type {priorityFac.Key} was not found");
                else addPriorityDefaultFacility(priorityInfo, priorityFac.Value);
            });

            defaultFacilityStrings.ToList().ForEach(defaultFac =>
            {
                KCFacilityInfoClass defaultInfo = Configuration.GetInfoClass(defaultFac.Key);
                if (defaultInfo == null) throw new MissingFieldException($"The default facility type {defaultFac.Key} was not found");
                else addDefaultFacility(defaultInfo, defaultFac.Value);
            });
        }

        public KC_CAB_Info(ConfigNode node) : base(node)
        {
            if (node.HasNode("priorityDefaultFacilities"))
            {
                ConfigNode priorityNode = node.GetNode("priorityDefaultFacilities");
                foreach (ConfigNode.Value v in priorityNode.values)
                {
                    priorityDefaultFacilityStrings.Add(v.name, int.Parse(v.value));

                }
            }

            if (node.HasNode("defaultFacilities"))
            {
                ConfigNode defaultNode = node.GetNode("defaultFacilities");
                foreach (ConfigNode.Value v in defaultNode.values)
                {
                    defaultFacilityStrings.Add(v.name, int.Parse(v.value));
                }
            }

            levelNodes.ToList().ForEach(kvp =>
            {
                ConfigNode n = kvp.Value;
                int level = kvp.Key;

                if (n.HasValue("editorRange")) EditorRange.Add(kvp.Key, float.Parse(n.GetValue("editorRange")));
                else if (level > 0) EditorRange.Add(kvp.Key, EditorRange[level - 1]);
                else EditorRange.Add(kvp.Key, 1000.0f);
            });
        }
    }
}
