using KerbalColonies.colonyFacilities.HangarFacility;
using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.Settings;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ProductionFacility
{
    public class KCProductionCosts
    {
        public Dictionary<PartResourceDefinition, double> Resources { get; } = [];
        public double Funds { get; set; }

        public KCProductionCosts() { }

        public KCProductionCosts(Dictionary<PartResourceDefinition, double> resources, double funds)
        {
            Resources = new Dictionary<PartResourceDefinition, double>(resources);
            Funds = funds;
        }
    }

    public abstract class KCProductionQueueItem
    {
        public const string VesselVariant = "vessel";

        public string Variant { get; protected set; }
        public List<string> Constraints { get; protected set; } = [];
        public double Progress { get; protected set; }
        public bool Paid { get; set; }
        protected KCProductionCosts costs;

        public bool IsComplete => Progress >= 1;
        public double RemainingProgress => Math.Max(0, 1 - Progress);

        public abstract double GetBuildTime(colonyClass colony);
        protected abstract KCProductionCosts CalculateCosts(colonyClass colony);
        public abstract bool IsAvailable(colonyClass colony);
        public abstract string GetDisplayName(colonyClass colony);
        public abstract void Complete(colonyClass colony);
        public abstract void Cancel(colonyClass colony);
        protected abstract void SavePayload(ConfigNode node);

        public KCProductionCosts GetCosts(colonyClass colony)
        {
            costs ??= CalculateCosts(colony);
            return costs;
        }

        public void RecalculateCosts(colonyClass colony) => costs = CalculateCosts(colony);

        public bool Matches(KCProductionFacility productionFacility) => Constraints.All(c => productionFacility.KCProductionInfo.ProductionCapabilities[productionFacility.level].Contains(c));

        public double GetAffordableProgress(colonyClass colony, double requestedProgress)
        {
            if (Paid || requestedProgress <= 0) return requestedProgress;

            KCProductionCosts currentCosts = GetCosts(colony);
            double affordableProgress = requestedProgress;
            KCUnifiedColonyStorage storage = KCUnifiedColonyStorage.colonyStorages.GetValueOrDefault(colony);
            if (storage == null && currentCosts.Resources.Count > 0) return 0;

            foreach (KeyValuePair<PartResourceDefinition, double> resource in currentCosts.Resources)
            {
                if (resource.Value <= 0) continue;
                affordableProgress = Math.Min(affordableProgress, storage.Resources.GetValueOrDefault(resource.Key) / resource.Value);
            }

            if (currentCosts.Funds > 0 && Funding.Instance != null)
            {
                affordableProgress = Math.Min(affordableProgress, Funding.Instance.Funds / currentCosts.Funds);
            }

            return Math.Max(0, affordableProgress);
        }

        public bool ApplyProgress(colonyClass colony, double progress)
        {
            progress = Math.Min(progress, RemainingProgress);
            progress = GetAffordableProgress(colony, progress);
            if (progress <= 0) return false;

            if (!Paid)
            {
                KCProductionCosts currentCosts = GetCosts(colony);
                KCUnifiedColonyStorage storage = KCUnifiedColonyStorage.colonyStorages.GetValueOrDefault(colony);
                foreach (KeyValuePair<PartResourceDefinition, double> resource in currentCosts.Resources)
                {
                    if (resource.Value > 0) storage.ChangeResourceStored(resource.Key, -resource.Value * progress);
                }
                if (currentCosts.Funds > 0) Funding.Instance?.AddFunds(-currentCosts.Funds * progress, TransactionReasons.None);
            }

            Progress += progress;
            return true;
        }

        public ConfigNode GetConfigNode()
        {
            ConfigNode node = new("queueItem");
            node.AddValue("variant", Variant);
            node.AddValue("progress", Progress);
            node.AddValue("paid", Paid);
            Constraints.ForEach(c => node.AddValue("constraint", c));
            SavePayload(node);
            return node;
        }

        protected KCProductionQueueItem(string variant, IEnumerable<string> constraints, double progress = 0, bool paid = false)
        {
            Variant = variant;
            Constraints = constraints.Distinct().ToList();
            Progress = Math.Max(0, Math.Min(1, progress));
            Paid = paid;
        }
    }

    public class KCFacilityProductionQueueItem : KCProductionQueueItem
    {
        public int FacilityId { get; }
        public int TargetLevel { get; }
        public bool IsUpgrade { get; }

        public KCFacilityBase Facility => KCFacilityBase.GetFacilityByID(FacilityId);

        public KCFacilityProductionQueueItem(KCFacilityBase facility, int targetLevel, bool isUpgrade, double progress = 0, bool paid = false)
            : base(facility.name, facility.facilityInfo.BuildConstraints[targetLevel], progress, paid)
        {
            FacilityId = facility.id;
            TargetLevel = targetLevel;
            IsUpgrade = isUpgrade;
        }

        public KCFacilityProductionQueueItem(ConfigNode node)
            : base(node.GetValue("variant"), node.GetValues("constraint"), double.Parse(node.GetValue("progress")), bool.Parse(node.GetValue("paid")))
        {
            FacilityId = int.Parse(node.GetValue("facilityId"));
            TargetLevel = int.Parse(node.GetValue("targetLevel"));
            IsUpgrade = bool.Parse(node.GetValue("isUpgrade"));
        }

        public override double GetBuildTime(colonyClass colony) => Facility.facilityInfo.UpgradeTimes[TargetLevel] * Configuration.FacilityTimeMultiplier;

        protected override KCProductionCosts CalculateCosts(colonyClass colony)
        {
            KCFacilityInfoClass info = Facility.facilityInfo;
            return new KCProductionCosts(
                info.resourceCost[TargetLevel].ToDictionary(pair => pair.Key, pair => pair.Value * Configuration.FacilityCostMultiplier),
                info.Funds[TargetLevel] * Configuration.FacilityCostMultiplier);
        }

        public override bool IsAvailable(colonyClass colony) => Facility != null && (IsUpgrade ? Facility.level + 1 == TargetLevel : !Facility.built);

        public override string GetDisplayName(colonyClass colony)
        {
            string facilityName = Facility?.DisplayName ?? Localizer.Format("#LOC_KC_PRODUCTION_MISSING_FACILITY");
            return Localizer.Format(IsUpgrade ? "#LOC_KC_PRODUCTION_UPGRADE_ITEM" : "#LOC_KC_PRODUCTION_CONSTRUCTION_ITEM", facilityName);
        }

        public override void Complete(colonyClass colony)
        {
            KCFacilityBase facility = Facility;
            if (facility == null) return;

            if (!IsUpgrade)
            {
                KCProductionFacility.AddConstructedFacility(facility);
                return;
            }

            switch (facility.facilityInfo.UpgradeTypes[TargetLevel])
            {
                case UpgradeType.withGroupChange:
                    KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                    break;
                case UpgradeType.withoutGroupChange:
                    KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                    break;
                case UpgradeType.withAdditionalGroup:
                    KCProductionFacility.AddUpgradedFacility(facility);
                    break;
            }
        }

        public override void Cancel(colonyClass colony)
        {
            if (!IsUpgrade && Facility != null) colony.Facilities.Remove(Facility);
        }

        protected override void SavePayload(ConfigNode node)
        {
            node.AddValue("facilityId", FacilityId);
            node.AddValue("targetLevel", TargetLevel);
            node.AddValue("isUpgrade", IsUpgrade);
        }
    }

    public class KCVesselProductionQueueItem : KCProductionQueueItem
    {
        public int HangarId { get; }
        public Guid VesselId { get; }
        public double DryMass { get; }
        public int PartCount { get; }

        private KCHangarFacility Hangar => KCFacilityBase.GetFacilityByID(HangarId) as KCHangarFacility;
        private StoredVessel Vessel => Hangar?.storedVessels.FirstOrDefault(v => v.uuid == VesselId);

        public KCVesselProductionQueueItem(KCHangarFacility hangar, StoredVessel vessel, double dryMass, int partCount, double progress = 0, bool paid = false)
            : base(VesselVariant, [VesselVariant], progress, paid)
        {
            HangarId = hangar.id;
            VesselId = vessel.uuid;
            DryMass = dryMass;
            PartCount = partCount;
        }

        public KCVesselProductionQueueItem(ConfigNode node)
            : base(VesselVariant, [VesselVariant], double.Parse(node.GetValue("progress")), bool.Parse(node.GetValue("paid")))
        {
            HangarId = int.Parse(node.GetValue("hangarId"));
            VesselId = Guid.Parse(node.GetValue("vesselId"));
            DryMass = double.Parse(node.GetValue("dryMass"));
            PartCount = int.Parse(node.GetValue("partCount"));
        }

        private KCProductionInfo GetRecipe(colonyClass colony, out int recipeLevel)
        {
            recipeLevel = 0;
            ConfigNode recipeNode = colony.sharedColonyNodes.FirstOrDefault(n => n.name == "vesselBuildInfo");
            if (recipeNode == null) return null;
            if (!int.TryParse(recipeNode.GetValue("facilityLevel"), out recipeLevel)) return null;
            return Configuration.GetInfoClass(recipeNode.GetValue("facilityConfig")) as KCProductionInfo;
        }

        public override double GetBuildTime(colonyClass colony) => (PartCount + DryMass) * 10 * Configuration.VesselTimeMultiplier;

        protected override KCProductionCosts CalculateCosts(colonyClass colony)
        {
            KCProductionInfo recipe = GetRecipe(colony, out int recipeLevel);
            return recipe == null
                ? new KCProductionCosts()
                : new KCProductionCosts(recipe.vesselResourceCost[recipeLevel].ToDictionary(pair => pair.Key, pair => pair.Value * DryMass * Configuration.VesselCostMultiplier), 0);
        }

        public override bool IsAvailable(colonyClass colony) => Hangar != null && Vessel != null;

        public override string GetDisplayName(colonyClass colony) => Vessel?.vesselName ?? Localizer.Format("#LOC_KC_PRODUCTION_MISSING_VESSEL");

        public override void Complete(colonyClass colony)
        {
            StoredVessel vessel = Vessel;
            if (vessel == null) return;
            vessel.vesselBuildTime = null;
            vessel.entireVesselBuildTime = null;
            vessel.vesselDryMass = null;
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_PRODUCTION_VESSEL_COMPLETE", vessel.vesselName, colony.DisplayName), 10f, ScreenMessageStyle.UPPER_RIGHT);
        }

        public override void Cancel(colonyClass colony)
        {
            if (Vessel != null) Hangar.storedVessels.Remove(Vessel);
        }

        protected override void SavePayload(ConfigNode node)
        {
            node.AddValue("hangarId", HangarId);
            node.AddValue("vesselId", VesselId);
            node.AddValue("dryMass", DryMass);
            node.AddValue("partCount", PartCount);
        }
    }
}
