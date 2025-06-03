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
    public enum UpgradeType
    {
        withoutGroupChange,
        withGroupChange,
        withAdditionalGroup,
    }

    /// <summary>
    /// The KCFaciltiyBase class is used to create custom KCFacilities, you must register your types in the typeregistry at the mainmenu Awake
    /// </summary>
    public abstract class KCFacilityBase
    {
        public colonyClass Colony { get; protected set; }
        public KCFacilityInfoClass facilityInfo { get; protected set; }
        public string name;
        private string displayName;
        public string DisplayName { get => useCustomDisplayName ? displayName : $"{facilityInfo.displayName} {facilityTypeNumber}"; set { displayName = value; useCustomDisplayName = true; } }
        public bool useCustomDisplayName;
        public int id;
        public bool enabled;
        public double lastUpdateTime;
        public double creationTime;
        public int level = 0;
        public int maxLevel = 0;
        public bool upgradeable = false;
        public bool built = false;
        /// <summary>
        /// This number is represents when the facility was built, e.g. if it was the first of its type it will be 1, if it was the second it will be 2, ...
        /// <para>It's used for the additional groups</para>
        /// </summary>
        public int facilityTypeNumber { get; protected set; } = 0;

        /// <summary>
        /// The KK group name and the body id
        /// </summary>
        public List<string> KKgroups = new List<string> { };

        public void changeDisplayName(string displayName)
        {
            useCustomDisplayName = true;
            OnDisplayNameChange(displayName);
            this.displayName = displayName;
        }

        public virtual void OnColonyNameChange(string name) { }

        public virtual void OnDisplayNameChange(string displayName) { }

        /// <summary>
        /// This function gets automatically called when the building is clicked, it might get used for custom windows.
        /// <para>The update function will be called BEFORE this one, you don't need to do it manually</para>
        /// </summary>
        public virtual void OnBuildingClicked() { }

        /// <summary>
        /// Used to check if the facility can be clicked in the CAB window, if false then the open button won't be shown in the CAB window.
        /// </summary>
        public bool AllowClick { get; protected set; } = true;

        /// <summary>
        /// This function gets called when the facility is clicked in the CAB window (if the CAB window was opened through the overview)
        /// </summary>
        public virtual void OnRemoteClicked() { }

        /// <summary>
        /// Used to check if the facility can be used remotely, if false then the open button won't be shown in the CAB window.
        /// </summary>
        public bool AllowRemote { get; protected set; } = true;

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
            KerbalKonstructs.API.GetGroupStatics(facility.KKgroups.Last()).ToList().ForEach(x => KerbalKonstructs.API.RemoveStatic(x.UUID));
            KerbalKonstructs.API.CopyGroup(facility.KKgroups.Last(), facility.GetBaseGroupName(facility.level), fromBodyName: Configuration.baseBody);

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

            ColonyBuilding.PlaceNewGroup(facility, $"{facility.Colony.Name}_{facility.name}_{facility.level}_{facility.facilityTypeNumber}");

            return true;
        }

        public static int CountFacilityType(KCFacilityInfoClass faciltyType, colonyClass colony)
        {
            return colony.Facilities.Count(f => f.name == faciltyType.name);
        }

        public static List<KCFacilityBase> GetFacilityTypeInColony(KCFacilityInfoClass facilityType, colonyClass colony)
        {
            return colony.Facilities.Where(f => f.name == facilityType.name).ToList();
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

        public string GetBaseGroupName(int level) => facilityInfo.BasegroupNames[level];

        /// <summary>
        /// This function get automatically called, do not call it manually.
        /// The OnBuildingClicked method function should be used for custom windows.
        /// </summary>
        public virtual void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        /// <summary>
        /// This method is used to store an additional confignode in the ColonyDataV3 file
        /// <para>It may be used to store data that is needed between different savegames, e.g. the launchpad UUIDs to disable the launchpads of other savegames</para>
        /// </summary>
        /// <returns></returns>
        public virtual ConfigNode GetSharedNode() => null;

        /// <summary>
        /// This method is used to save all of the persistent facility data.
        /// <para>It's recommended to call this base method as it adds the necessary metadata like name, creationdate, ...</para>
        /// </summary>
        public virtual ConfigNode getConfigNode()
        {
            ConfigNode node = new ConfigNode("facilityNode");
            node.AddValue("name", name);
            node.AddValue("displayName", DisplayName);
            node.AddValue("useCustomDisplayName", useCustomDisplayName);
            node.AddValue("id", id);
            node.AddValue("enabled", enabled);
            node.AddValue("level", level);
            node.AddValue("creationTime", creationTime);
            node.AddValue("lastUpdateTime", lastUpdateTime);
            node.AddValue("built", built.ToString());
            node.AddValue("facilityTypeNumber", facilityTypeNumber.ToString());

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

        #region cabConstructors
        /// <summary>
        /// This constructor is ONLY meant to be used for the CAB facility
        /// <para>DON'T USE IT</para>
        /// <para>It doesn't add the facility to the facility list of the colony which means it won't get saved.</para>
        /// </summary>
        protected KCFacilityBase(KC_CABInfo CABInfo)
        {
            this.facilityInfo = CABInfo;

            this.name = CABInfo.facilityConfig.GetValue("name");
            this.DisplayName = CABInfo.facilityConfig.GetValue("displayName");
            this.enabled = true;
            this.id = createID();
            this.level = 0;
            this.maxLevel = 0;
            this.facilityTypeNumber = 0;
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
            this.DisplayName = CABInfo.facilityConfig.GetValue("displayName");
            this.id = int.Parse(node.GetValue("id"));
            this.enabled = bool.Parse(node.GetValue("enabled"));
            this.level = 0;
            this.maxLevel = 0;
            this.facilityTypeNumber = 0;
            this.creationTime = double.Parse(node.GetValue("creationTime"));
            this.lastUpdateTime = double.Parse(node.GetValue("lastUpdateTime"));
            this.built = bool.Parse(node.GetValue("built"));
            this.KKgroups = new List<string> { };
            node.GetNodes("kkGroupNode").ToList().ForEach(n => KKgroups.Add(n.GetValue("groupName")));

            this.upgradeable = false;
        }
        #endregion

        /// <summary>
        /// This constructor is used for restoring existing facilities during loading.
        /// </summary>
        /// <param name="node"></param>
        protected KCFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node)
        {
            this.Colony = colony;
            this.facilityInfo = facilityInfo;

            this.name = facilityInfo.name;
            this.displayName = node.GetValue("displayName");
            
            if (Configuration.loadedSaveVersion == new Version(3, 1, 1)) useCustomDisplayName = false;
            else useCustomDisplayName = bool.Parse(node.GetValue("useCustomDisplayName"));

            this.id = int.Parse(node.GetValue("id"));
            this.enabled = bool.Parse(node.GetValue("enabled"));
            this.level = int.Parse(node.GetValue("level"));
            this.maxLevel = facilityInfo.BasegroupNames.Count - 1;
            this.creationTime = double.Parse(node.GetValue("creationTime"));
            this.lastUpdateTime = double.Parse(node.GetValue("lastUpdateTime"));
            this.built = bool.Parse(node.GetValue("built"));
            this.facilityTypeNumber = int.Parse(node.GetValue("facilityTypeNumber"));
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
        /// The base constructor of the kc facilities.
        /// </summary>
        protected KCFacilityBase(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled)
        {
            this.Colony = colony;
            this.facilityInfo = facilityInfo;

            this.name = facilityInfo.name;
            this.DisplayName = "";
            useCustomDisplayName = false;
            this.enabled = enabled;
            this.id = createID();
            this.level = 0;
            this.maxLevel = facilityInfo.BasegroupNames.Count - 1;
            this.facilityTypeNumber = CountFacilityType(facilityInfo, colony) + 1;
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

            colony.AddFacility(this);
        }
    }
}
