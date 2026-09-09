namespace KerbalColonies.colonyFacilities
{
    public interface IKCColonyBuildable
    {
        colonyClass Colony { get; }
        KCFacilityInfoClass BuildableInfo { get; }
        string Name { get; }
        string DisplayName { get; }
        int Id { get; }
        int Level { get; }
        int MaxLevel { get; }
        int BuildableTypeNumber { get; }
        bool Upgradeable { get; }
        bool Built { get; }

        ConfigNode GetConfigNode();
        bool Upgrade(int level);
        void CompleteConstruction();
        void CompleteUpgrade();
        void CancelConstruction();
    }
}
