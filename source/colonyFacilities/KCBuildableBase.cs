using KerbalColonies.colonyFacilities.ProductionFacility;
using KerbalColonies.Settings;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities
{
    public abstract class KCBuildableInfoClass : KCFacilityInfoClass
    {
        public string subgroup { get; protected set; }

        protected KCBuildableInfoClass(ConfigNode node) : base(node)
        {
            subgroup = node.GetValue("subgroup") ?? "Other";
        }
    }

    public class KCGroupedBuildableInfo : KCBuildableInfoClass
    {
        public KCGroupedBuildableInfo(ConfigNode node) : base(node) { }
    }

    public class KCSingleStaticBuildableInfo : KCBuildableInfoClass
    {
        public SortedDictionary<int, string> PointerNames { get; } = [];

        public KCSingleStaticBuildableInfo(ConfigNode node) : base(node)
        {
            foreach (KeyValuePair<int, ConfigNode> levelNode in levelNodes)
            {
                if (levelNode.Value.HasValue("pointername")) PointerNames.Add(levelNode.Key, levelNode.Value.GetValue("pointername"));
                else if (levelNode.Key > 0) PointerNames.Add(levelNode.Key, PointerNames[levelNode.Key - 1]);
                else throw new System.MissingFieldException($"The single-static buildable {name} has no pointername at level 0.");
            }
        }
    }

    public abstract class KCBuildableBase : IKCColonyBuildable
    {
        public colonyClass Colony { get; protected set; }
        public KCFacilityInfoClass BuildableInfo { get; protected set; }
        public string Name => BuildableInfo.name;
        public string DisplayName => $"{BuildableInfo.displayName} {BuildableTypeNumber}";
        public int Id { get; protected set; }
        public int Level { get; protected set; }
        public int MaxLevel => BuildableInfo.levelNodes.Count - 1;
        public int BuildableTypeNumber { get; protected set; }
        public bool Upgradeable => Level < MaxLevel;
        public abstract bool Built { get; }

        public virtual ConfigNode GetConfigNode()
        {
            ConfigNode node = new("buildableNode");
            node.AddValue("name", Name);
            node.AddValue("id", Id);
            node.AddValue("level", Level);
            node.AddValue("buildableTypeNumber", BuildableTypeNumber);
            SavePlacement(node);
            return node;
        }

        protected abstract void SavePlacement(ConfigNode node);

        public virtual bool Upgrade(int level)
        {
            Level = level;
            return true;
        }

        public virtual void CompleteConstruction()
        {
            KCProductionFacility.AddConstructedBuildable(this);
        }

        public virtual void CompleteUpgrade()
        {
            if (BuildableInfo.UpgradeTypes[Level + 1] == UpgradeType.withoutGroupChange)
            {
                Upgrade(Level + 1);
                return;
            }

            KCProductionFacility.AddUpgradedBuildable(this);
        }

        public virtual void CancelConstruction()
        {
            Colony.Buildables.Remove(this);
        }

        protected KCBuildableBase(colonyClass colony, KCFacilityInfoClass info, bool enabled)
        {
            Colony = colony;
            BuildableInfo = info;
            Id = KCFacilityBase.createID();
            Level = 0;
            BuildableTypeNumber = colony.Buildables.Count(buildable => buildable.Name == info.name) + 1;
            colony.AddBuildable(this);
        }

        protected KCBuildableBase(colonyClass colony, KCFacilityInfoClass info, ConfigNode node)
        {
            Colony = colony;
            BuildableInfo = info;
            Id = int.Parse(node.GetValue("id"));
            Level = int.Parse(node.GetValue("level"));
            BuildableTypeNumber = int.Parse(node.GetValue("buildableTypeNumber"));
        }
    }

    public class KCGroupedBuildable : KCBuildableBase
    {
        public List<string> KKGroups { get; } = [];
        public override bool Built => KKGroups.Count > 0;

        protected override void SavePlacement(ConfigNode node)
        {
            KKGroups.ForEach(group => node.AddValue("group", group));
        }

        public override void CompleteUpgrade()
        {
            int targetLevel = Level + 1;
            switch (BuildableInfo.UpgradeTypes[targetLevel])
            {
                case UpgradeType.withoutGroupChange:
                    Upgrade(targetLevel);
                    break;
                case UpgradeType.withGroupChange:
                    string groupName = KKGroups.Last();
                    KerbalKonstructs.API.GetGroupStatics(groupName, Colony.BodyName).ToList()
                        .ForEach(instance => KerbalKonstructs.API.RemoveStatic(instance.UUID));
                    KerbalKonstructs.API.CopyGroup(groupName, BuildableInfo.BasegroupNames[targetLevel], Colony.BodyName, Configuration.baseBody);
                    KerbalKonstructs.API.GetGroupStatics(groupName, Colony.BodyName).ForEach(instance => instance.isInSavegame = true);
                    Upgrade(targetLevel);
                    KerbalKonstructs.API.Save();
                    break;
                case UpgradeType.withAdditionalGroup:
                    KCProductionFacility.AddUpgradedBuildable(this);
                    break;
            }
        }

        public KCGroupedBuildable(colonyClass colony, KCFacilityInfoClass info, bool enabled) : base(colony, info, enabled) { }

        public KCGroupedBuildable(colonyClass colony, KCFacilityInfoClass info, ConfigNode node) : base(colony, info, node)
        {
            KKGroups.AddRange(node.GetValues("group"));
        }
    }

    public class KCSingleStaticBuildable : KCBuildableBase
    {
        public List<string> StaticIds { get; } = [];
        public override bool Built => StaticIds.Count > 0;

        protected override void SavePlacement(ConfigNode node)
        {
            StaticIds.ForEach(uuid => node.AddValue("staticId", uuid));
        }

        public override void CompleteUpgrade()
        {
            if (BuildableInfo.UpgradeTypes[Level + 1] == UpgradeType.withoutGroupChange)
                Upgrade(Level + 1);
            else
                KCProductionFacility.AddUpgradedBuildable(this);
        }

        public KCSingleStaticBuildable(colonyClass colony, KCFacilityInfoClass info, bool enabled) : base(colony, info, enabled) { }

        public KCSingleStaticBuildable(colonyClass colony, KCFacilityInfoClass info, ConfigNode node) : base(colony, info, node)
        {
            StaticIds.AddRange(node.GetValues("staticId"));
        }
    }
}
