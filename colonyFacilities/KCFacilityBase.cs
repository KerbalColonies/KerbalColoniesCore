using KerbalKonstructs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities
{
    public enum UpgradeType
    {
        withoutGroupChange,
        withGroupChange,
        withAdditionalGroup,
    }

    /// <summary>
    /// <para>The KCFaciltiyBase class is used to create custom KCFacilities, you must register your types in the typeregistry at startup with the AWAKE method.</para>
    /// <para>If the facility can be built from the CAB it must be registered with a KCFacilityInfoClass in the Configuration.BuildableFacilities dictionary via the RegisterBuildableFacility method.</para>
    /// <para>If the faciltiy can be upgraded from the CAB it must be registered with the inclusive maximum level in the Configuration. UpgradeableFacilities Dictionary</para>
    /// <para>If you have custom fields that you want to save overwrite the encode and decode string methods and save the values of your custom fields in the facilityData string.</para>
    /// <para>Public fields are saved during the serialization but they are NOT loaded.</para>
    /// <para>You can encode the data however you want but you are not allowed to use "{", "}", ",", ":", "=" and "//"</para>
    /// <para>This is because the datastring is saved as a value in a KSP confignode and using these symbols can mess up the loading<para>
    /// <para>I recommend using "|" as seperator and "&" instead of "="</para>
    /// <para>It's NOT checked if the datastring contains any of these symbols!</para>
    /// </summary>
    public abstract class KCFacilityBase
    {
        public colonyClass Colony { get; protected set; }
        public KCFacilityInfoClass facilityInfo { get; protected set; }

        public string displayName;
        public string name;
        public int id;
        public bool enabled;
        public double lastUpdateTime;
        public double creationTime;
        public int level = 0;
        public int maxLevel = 0;
        public bool upgradeable = false;
        public bool built = false;

        /// <summary>
        /// The KK group name and the body id
        /// </summary>
        public List<string> KKgroups = new List<string> { };


        /// <summary>
        /// This function gets automatically called when the building is clicked, it might get used for custom windows.
        /// <para>The update function will be called BEFORE this one, you don't need to do it manually</para>
        /// </summary>
        public virtual void OnBuildingClicked() { }

        /// <summary>
        /// This function gets called when the facility is clicked in the CAB window (if the CAB window was opened through the overview)
        /// </summary>
        public virtual void OnRemoteClicked() { }

        public bool AllowRemote = true;

        internal static void OnBuildingClickedHandler(KerbalKonstructs.Core.StaticInstance instance)
        {
            if (Configuration.GroupFacilities.ContainsKey(instance.Group))
            {
                Configuration.GroupFacilities[instance.Group].Update();
                Configuration.GroupFacilities[instance.Group].OnBuildingClicked();
            }
        }

        public static bool UpgradeFacilityWithGroupChange(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTypes[facility.level + 1] != UpgradeType.withGroupChange || !facility.upgradeable) { return false; }

            facility.UpgradeFacility(facility.level + 1);
            KerbalKonstructs.API.GetGroupStatics(facility.KKgroups.First()).ToList().ForEach(x => KerbalKonstructs.API.RemoveStatic(x.UUID));
            KerbalKonstructs.API.CopyGroup(facility.KKgroups.First(), facility.GetBaseGroupName(facility.level), fromBodyName: "Kerbin");

            return true;
        }

        public static bool UpgradeFacilityWithoutGroupChange(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTypes[facility.level + 1] != UpgradeType.withoutGroupChange || !facility.upgradeable) { return false; }

            facility.UpgradeFacility(facility.level + 1);
            return true;
        }

        public static bool UpgradeFacilityWithAdditionalGroup(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTypes[facility.level + 1] != UpgradeType.withAdditionalGroup || !facility.upgradeable) { return false; }

            facility.UpgradeFacility(facility.level + 1);

            ColonyBuilding.PlaceNewGroup(facility, $"{facility.Colony.Name}_{facility.GetType().Name}_{facility.level}_{KCFacilityBase.CountFacilityType(facility.GetType(), facility.Colony)}");

            return true;
        }

        public static int CountFacilityType(Type faciltyType, colonyClass colony)
        {
            return colony.Facilities.Count(x => faciltyType.IsInstanceOfType(x));
        }

        public static List<string> GetUUIDbyFacility(KCFacilityBase facility)
        {
            List<string> uuids = new List<string>();

            facility.KKgroups.ForEach(groupName => KerbalKonstructs.API.GetGroupStatics(groupName).ForEach(kkStatic => uuids.Add(kkStatic.UUID)));

            return uuids;
        }

        public static KCFacilityBase GetFacilityByID(int id)
        {
            foreach (var colony in Configuration.colonyDictionary.Values.SelectMany(c => c))
            {
                foreach (var facility in colony.Facilities)
                {
                    if (facility.id == id)
                    {
                        return facility;
                    }
                }
            }
            return null;
        }

        public static bool IDexists(int id)
        {
            return Configuration.colonyDictionary.Values.SelectMany(c => c).Any(c => c.Facilities.Any(fac => fac.id == id) || c.CAB.id == id);
        }

        private static Random random = new Random();
        internal static int createID()
        {
            int id = 0;

            while (true)
            {
                for (int i = 0; i < random.Next(10); i++)
                {
                    id = random.Next();
                }

                if (!IDexists(id) && id != 0)
                {
                    return id;
                }
            }

        }

        /// <summary>
        /// This method gets called when the KK group is placed and saved
        /// </summary>
        public virtual void OnGroupPlaced() { }

        public virtual string GetBaseGroupName(int level) => facilityInfo.BasegroupNames[level];

        /// <summary>
        /// This function get automatically called, do not call it manually.
        /// The OnBuildingClicked method function should be used for custom windows.
        /// </summary>
        public virtual void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        /// <summary>
        /// This method is used to save all of the persistent facility data.
        /// <para>It's recommended to call this base method as it adds the necessary metadata like name, creationdate, ...</para>
        /// </summary>
        public virtual ConfigNode getConfigNode()
        {
            ConfigNode node = new ConfigNode("facilityNode");
            node.AddValue("name", name);
            node.AddValue("id", id);
            node.AddValue("enabled", enabled);
            node.AddValue("level", level);
            node.AddValue("creationTime", creationTime);
            node.AddValue("lastUpdateTime", lastUpdateTime);
            node.AddValue("built", built.ToString());

            KKgroups.ForEach(x =>
            {
                ConfigNode groupNode = new ConfigNode("kkGroupNode");
                groupNode.AddValue("groupName", x);
                node.AddNode(groupNode);
            }
            );
            return node;
        }

        public virtual bool UpgradeFacility(int level)
        {
            this.level = level;
            if (this.level >= this.maxLevel) upgradeable = false;
            return true;
        }

        /// <summary>
        /// This method is called in the CAB facility overview window to display custom data about the facility.
        /// </summary>
        public virtual string GetFacilityProductionDisplay()
        {
            return "";
        }

        /// <summary>
        /// This constructor is ONLY meant to be used for the CAB facility
        /// <para>DON'T USE IT</para>
        /// <para>It doesn't add the facility to the facility list of the colony which means it won't get saved.</para>
        /// </summary>
        protected KCFacilityBase(KC_CABInfo CABInfo)
        {
            this.facilityInfo = CABInfo;

            this.name = CABInfo.facilityConfig.GetValue("name");
            this.displayName = CABInfo.facilityConfig.GetValue("displayName");
            this.enabled = true;
            this.id = createID();
            this.level = 0;
            this.maxLevel = 0;
            creationTime = Planetarium.GetUniversalTime();
            lastUpdateTime = Planetarium.GetUniversalTime();

            this.upgradeable = false;
        }

        /// <summary>
        /// This constructor is ONLY meant to be used for the CAB facility
        /// <para>DON'T USE IT</para>
        /// <para>It doesn't add the facility to the facility list of the colony which means it won't get saved.</para>
        /// </summary>
        protected KCFacilityBase(KC_CABInfo CABInfo, ConfigNode node)
        {
            this.facilityInfo = CABInfo;

            this.name = CABInfo.facilityConfig.GetValue("name");
            this.displayName = CABInfo.facilityConfig.GetValue("displayName");
            this.id = int.Parse(node.GetValue("id"));
            this.enabled = bool.Parse(node.GetValue("enabled"));
            this.level = 0;
            this.maxLevel = 0;
            this.creationTime = double.Parse(node.GetValue("creationTime"));
            this.lastUpdateTime = double.Parse(node.GetValue("lastUpdateTime"));
            this.built = bool.Parse(node.GetValue("built"));
            this.KKgroups = new List<string> { };
            node.GetNodes("kkGroupNode").ToList().ForEach(n => KKgroups.Add(n.GetValue("groupName")));

            this.upgradeable = false;
        }

        /// <summary>
        /// This constructor is used for restoring existing facilities during loading.
        /// </summary>
        /// <param name="node"></param>
        protected KCFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node)
        {
            this.Colony = colony;
            this.facilityInfo = facilityInfo;

            this.name = facilityInfo.facilityConfig.GetValue("name");
            this.displayName = facilityInfo.facilityConfig.GetValue("displayName");
            this.id = int.Parse(node.GetValue("id"));
            this.enabled = bool.Parse(node.GetValue("enabled"));
            this.level = int.Parse(node.GetValue("level"));
            this.maxLevel = facilityInfo.BasegroupNames.Count - 1;
            this.creationTime = double.Parse(node.GetValue("creationTime"));
            this.lastUpdateTime = double.Parse(node.GetValue("lastUpdateTime"));
            this.built = bool.Parse(node.GetValue("built"));
            this.KKgroups = new List<string> { };
            node.GetNodes("kkGroupNode").ToList().ForEach(n => KKgroups.Add(n.GetValue("groupName")));

            if (this.level < this.maxLevel)
            {
                this.upgradeable = true;
            }
            else
            {
                this.upgradeable = false;
            }
        }

        /// <summary>
        /// The base constructor of the kc facilities. It only calls the initialize function.
        /// You can use a custom constructor but it should only call an overriden initialize function and not the base constructor
        /// This is necessary because of the serialization.
        /// </summary>
        protected KCFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled)
        {
            this.Colony = colony;
            this.facilityInfo = facilityInfo;

            this.name = facilityInfo.facilityConfig.GetValue("name");
            this.displayName = facilityInfo.facilityConfig.GetValue("displayName");
            this.enabled = enabled;
            this.id = createID();
            this.level = 0;
            this.maxLevel = facilityInfo.BasegroupNames.Count - 1;
            creationTime = Planetarium.GetUniversalTime();
            lastUpdateTime = Planetarium.GetUniversalTime();

            if (this.level < this.maxLevel)
            {
                this.upgradeable = true;
            }
            else
            {
                this.upgradeable = false;
            }

            colony.Facilities.Add(this);
        }
    }
}
