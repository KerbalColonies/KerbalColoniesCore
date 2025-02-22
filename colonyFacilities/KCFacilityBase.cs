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
    /// <para>If the facility can be built from the CAB it must be registered with a KCFacilityCostClass in the Configuration.BuildableFacilities dictionary via the RegisterBuildableFacility method.</para>
    /// <para>If the faciltiy can be upgraded from the CAB it must be registered with the inclusive maximum level in the Configuration. UpgradeableFacilities Dictionary</para>
    /// <para>If you have custom fields that you want to save overwrite the encode and decode string methods and save the values of your custom fields in the facilityData string.</para>
    /// <para>Public fields are saved during the serialization but they are NOT loaded.</para>
    /// <para>You can encode the data however you want but you are not allowed to use "{", "}", ",", ":", "=" and "//"</para>
    /// <para>This is because the datastring is saved as a value in a KSP confignode and using these symbols can mess up the loading<para>
    /// <para>I recommend using "|" as seperator and "&" instead of "="</para>
    /// <para>It's NOT checked if the datastring contains any of these symbols!</para>
    /// </summary>
    [System.Serializable]
    public abstract class KCFacilityBase
    {
        public string name;
        public int id;
        public bool enabled;
        public double lastUpdateTime;
        public double creationTime;
        protected bool initialized = false;
        internal bool Initialized { get { return initialized; } set { initialized = value; } }
        public string baseGroupName; // The KC group name that will be copied when creating a new facility
        public int level = 0;
        public int maxLevel = 0;
        public bool upgradeable = false;
        public UpgradeType upgradeType = UpgradeType.withoutGroupChange;

        /// <summary>
        /// This function gets automatically called when the building is clicked, it might get used for custom windows.
        /// <para>The update function will be called BEFORE this one, you don't need to do it manually</para>
        /// </summary>
        public virtual void OnBuildingClicked() { }

        internal static void OnBuildingClickedHandler(KerbalKonstructs.Core.StaticInstance instance)
        {
            if (GetInformationByUUID(instance.UUID, out string sg, out int bI, out string cN, out GroupPlaceHolder gph, out List<KCFacilityBase> facilities))
            {
                if (sg != HighLogic.CurrentGame.Seed.ToString() || bI != FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)) { return; }

                foreach (KCFacilityBase kcFacility in facilities)
                {
                    KSPLog.print(Configuration.APP_NAME + ": " + instance.ToString());
                    kcFacility.Update();
                    kcFacility.OnBuildingClicked();
                }
            }
        }

        internal static bool UpgradeFacilityWithGroupChange(KCFacilityBase facility)
        {
            if (facility.upgradeType != UpgradeType.withGroupChange || !facility.upgradeable) { return false; }

            if (GetInformationByFacilty(facility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs))
            {
                foreach (GroupPlaceHolder gph in gphs)
                {
                    facility.UpgradeFacility(facility.level + 1);

                    Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph] = new Dictionary<string, List<KCFacilityBase>>();
                    KerbalKonstructs.API.GetGroupStatics(gph.GroupName).ToList().ForEach(x => KerbalKonstructs.API.RemoveStatic(x.UUID));

                    KerbalKonstructs.API.CopyGroup(gph.GroupName, facility.baseGroupName);

                    foreach (KerbalKonstructs.Core.StaticInstance staticInstance in KerbalKonstructs.API.GetGroupStatics(gph.GroupName))
                    {
                        Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Add(staticInstance.UUID, new List<KCFacilityBase> { facility });
                    }
                }

                Configuration.saveColonies = true;
                return true;
            }
            return false;
        }

        internal static bool UpgradeFacilityWithoutGroupChange(KCFacilityBase facility)
        {
            if (facility.upgradeType != UpgradeType.withoutGroupChange || !facility.upgradeable || facility.level >= facility.maxLevel) { return false; }

            if (GetInformationByFacilty(facility, out string saveGames, out int bodyIndexes, out string colonyNames, out List<GroupPlaceHolder> gphs, out List<string> UUIDs))
            {
                facility.UpgradeFacility(facility.level + 1);
                Configuration.saveColonies = true;
                return true;
            }
            return false;
        }

        internal static bool UpgradeFacilityWithAdditionalGroup(KCFacilityBase facility)
        {
            if (facility.upgradeType != UpgradeType.withAdditionalGroup || !facility.upgradeable || facility.level >= facility.maxLevel) { return false; }

            if (GetInformationByFacilty(facility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs))
            {
                facility.UpgradeFacility(facility.level + 1);

                KCFacilityBase.CountFacilityType(facility.GetType(), saveGame, bodyIndex, colonyName, out int count);
                Colonies.AddGroupUpdate(facility, facility.baseGroupName, $"{colonyName}_{facility.GetType().Name}_{facility.level}_{count}", colonyName);
                Configuration.saveColonies = true;
                return true;
            }
            return false;
        }

        internal static bool CountFacilityType(Type faciltyType, string saveGame, int bodyIndex, string colonyName, out int count)
        {
            count = -1;
            if (!typeof(KCFacilityBase).IsAssignableFrom(faciltyType))
            {
                return false;
            }
            if (!Configuration.coloniesPerBody.ContainsKey(saveGame))
            {
                return false;
            }
            else if (!Configuration.coloniesPerBody[saveGame].ContainsKey(bodyIndex))
            {
                return false;
            }
            else if (!Configuration.coloniesPerBody[saveGame][bodyIndex].ContainsKey(colonyName))
            {
                return false;
            }

            List<KCFacilityBase> facList = new List<KCFacilityBase>();
            foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Keys)
            {
                foreach (string uuid in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Keys)
                {
                    foreach (KCFacilityBase fac in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph][uuid])
                    {
                        if (fac.GetType() == faciltyType)
                        {
                            if (!facList.Contains(fac))
                            {
                                facList.Add(fac);
                            }
                        }
                    }
                }
            }
            count = facList.Count;
            return true;
        }

        internal static List<KCFacilityBase> GetFacilitiesInColony(string saveGame, int bodyIndex, string colonyName)
        {
            List<KCFacilityBase> facList = new List<KCFacilityBase>();
            foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Keys)
            {
                foreach (string uuid in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Keys)
                {
                    foreach (KCFacilityBase fac in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph][uuid])
                    {
                        if (!facList.Contains(fac))
                        {
                            facList.Add(fac);
                        }
                    }
                }
            }
            return facList;
        }

        internal static bool GetInformationByFacilty(KCFacilityBase facility, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs)
        {
            saveGame = "";
            bodyIndex = -1;
            colonyName = "";
            gph = new List<GroupPlaceHolder>();
            UUIDs = new List<string>();

            foreach (string sg in Configuration.coloniesPerBody.Keys)
            {
                foreach (int bI in Configuration.coloniesPerBody[sg].Keys)
                {
                    foreach (string cN in Configuration.coloniesPerBody[sg][bI].Keys)
                    {
                        foreach (GroupPlaceHolder gp in Configuration.coloniesPerBody[sg][bI][cN].Keys)
                        {
                            foreach (string id in Configuration.coloniesPerBody[sg][bI][cN][gp].Keys)
                            {
                                if (Configuration.coloniesPerBody[sg][bI][cN][gp][id].Contains(facility))
                                {
                                    saveGame = sg;
                                    bodyIndex = bI;
                                    colonyName = cN;
                                    if (!gph.Contains(gp)) { gph.Add(gp); }
                                    if (!UUIDs.Contains(id)) { UUIDs.Add(id); }
                                }
                            }
                        }
                    }
                }
            }

            if (UUIDs.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static bool GetInformationByUUID(string uuid, out string saveGame, out int bodyIndex, out string colonyName, out GroupPlaceHolder gph, out List<KCFacilityBase> facilities)
        {
            saveGame = null;
            bodyIndex = -1;
            colonyName = null;
            gph = null;
            facilities = null;

            foreach (string sg in Configuration.coloniesPerBody.Keys)
            {
                foreach (int bI in Configuration.coloniesPerBody[sg].Keys)
                {
                    foreach (string cN in Configuration.coloniesPerBody[sg][bI].Keys)
                    {
                        foreach (GroupPlaceHolder gp in Configuration.coloniesPerBody[sg][bI][cN].Keys)
                        {
                            foreach (string id in Configuration.coloniesPerBody[sg][bI][cN][gp].Keys)
                            {
                                if (id == uuid)
                                {
                                    saveGame = sg;
                                    bodyIndex = bI;
                                    colonyName = cN;
                                    gph = gp;
                                    facilities = Configuration.coloniesPerBody[sg][bI][cN][gp][id];
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        internal static string[] GetUUIDbyFacility(KCFacilityBase facility)
        {
            List<string> uuids = new List<string>();

            foreach (string saveGame in Configuration.coloniesPerBody.Keys)
            {
                foreach (int bodyId in Configuration.coloniesPerBody[saveGame].Keys)
                {
                    foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyId].Keys)
                    {
                        foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyId][colonyName].Keys)
                        {
                            foreach (string uuid in Configuration.coloniesPerBody[saveGame][bodyId][colonyName][gph].Keys)
                            {
                                if (Configuration.coloniesPerBody[saveGame][bodyId][colonyName][gph][uuid].Contains(facility))
                                {
                                    uuids.Add(uuid);
                                }
                            }
                        }
                    }
                }
            }

            return uuids.ToArray();
        }

        /// <summary>
        /// Returns true if the facility with the corresponding id was found
        /// Returns false if the the id wasn't found.
        /// </summary>
        /// <returns>Important: if the facility wasn't found the outed facility is NULL</returns>
        internal static bool GetFacilityByID(int id, out KCFacilityBase facility)
        {
            foreach (string saveGame in Configuration.coloniesPerBody.Keys)
            {
                foreach (int bodyId in Configuration.coloniesPerBody[saveGame].Keys)
                {
                    foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyId].Keys)
                    {
                        foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyId][colonyName].Keys)
                        {
                            foreach (string uuid in Configuration.coloniesPerBody[saveGame][bodyId][colonyName][gph].Keys)
                            {
                                foreach (KCFacilityBase fac in Configuration.coloniesPerBody[saveGame][bodyId][colonyName][gph][uuid])
                                {
                                    if (fac.id == id)
                                    {
                                        facility = fac;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            facility = null;
            return false;
        }

        internal static bool IDexists(int id)
        {
            foreach (string saveGame in Configuration.coloniesPerBody.Keys)
            {
                foreach (int bodyId in Configuration.coloniesPerBody[saveGame].Keys)
                {
                    foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyId].Keys)
                    {
                        foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyId][colonyName].Keys)
                        {
                            foreach (string uuid in Configuration.coloniesPerBody[saveGame][bodyId][colonyName][gph].Keys)
                            {
                                foreach (KCFacilityBase fac in Configuration.coloniesPerBody[saveGame][bodyId][colonyName][gph][uuid])
                                {
                                    if (fac.id == id)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }


        private static Random random = new Random();
        // TODO: find out why the default facilities have the same id as the cab facility
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

        /// <summary>
        /// This method should return the upgrade time for the specified level
        /// <para>Each engineer reduces the upgrade time by 100 + 5 * (engineer level - 1) * facility bonus / day. If the upgrade time reaches 0 then the upgrade can be placed</para>
        /// </summary>
        /// <returns></returns>
        public virtual int GetUpgradeTime(int level) { return 0; }

        /// <summary>
        /// This function get automatically called, do not call it manually.
        /// The OnBuildingClicked method function should be used for custom windows.
        /// </summary>
        public virtual void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public virtual void UpdateBaseGroupName()
        {
        }

        /// <summary>
        /// A new way of storing custom data, the loadCustomNode will recive the node from this method
        /// </summary>
        public virtual ConfigNode getCustomNode()
        {
            return null;
        }

        /// <summary>
        /// A new way of storing custom data, this method recieves the node from the getCustomNode method
        /// </summary>
        public virtual void loadCustomNode(ConfigNode customNode)
        {

        }


        public virtual bool UpgradeFacility(int level)
        {
            this.level = level;
            this.UpdateBaseGroupName();
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
        /// This method is called when the facilty when an object is created.
        /// During deserialization the constructor is not called, this method is used set up the facility for use during the game.
        /// </summary>
        public virtual void Initialize()
        {
            if (!initialized)
            {
                baseGroupName = "KC_CAB";

                if (this.level < this.maxLevel)
                {
                    this.upgradeable = true;
                }
                else
                {
                    this.upgradeable = false;
                }

                initialized = true;
            }
        }

        /// <summary>
        /// The base constructor of the kc facilities. It only calls the initialize function.
        /// You can use a custom constructor but it should only call an overriden initialize function and not the base constructor
        /// This is necessary because of the serialization.
        /// </summary>
        protected KCFacilityBase(string facilityName, bool enabled, int level = 0, int maxLevel = 0)
        {
            this.name = facilityName;
            this.enabled = enabled;
            this.id = createID();
            this.level = level;
            this.maxLevel = maxLevel;
            creationTime = Planetarium.GetUniversalTime();
            lastUpdateTime = Planetarium.GetUniversalTime();
            Initialize();
        }
    }
}
