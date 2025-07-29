using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.Settings
{
    public class KCGameParameters : GameParameters.CustomParameterNode
    {
        public override string Title { get => "Kerbal Colonies"; }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Kerbal Colonies"; } }
        public override string DisplaySection { get { return "Kerbal Colonies"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        [GameParameters.CustomFloatParameterUI("Facility build cost multiplier", toolTip = "Multiplies the build cost for all facilities and new colonies.", addTextField = true, asPercentage = false, autoPersistance = false, logBase = 1.5f, gameMode = GameParameters.GameMode.ANY, maxValue = 10f, newGameOnly = false, displayFormat = "0.00")]
        public float FacilityCostMultiplier { get => Configuration.FacilityCostMultiplier; set => Configuration.FacilityCostMultiplier = value; }

        [GameParameters.CustomFloatParameterUI("Facility build time multiplier", toolTip = "Multiplies the build time for all facilities.", addTextField = true, asPercentage = false, autoPersistance = false, logBase = 1.5f, gameMode = GameParameters.GameMode.ANY, maxValue = 10f, newGameOnly = false, displayFormat = "0.00")]
        public float FacilityTimeMultiplier { get => Configuration.FacilityTimeMultiplier; set => Configuration.FacilityTimeMultiplier = value; }

        [GameParameters.CustomFloatParameterUI("Facility range multiplier", toolTip = "Multiplies the range of all facilities.", addTextField = true, asPercentage = false, autoPersistance = false, logBase = 1.5f, gameMode = GameParameters.GameMode.ANY, minValue = 0.1f, maxValue = 10f, newGameOnly = false, displayFormat = "0.00")]
        public float FacilityRangeMultiplier { get => Configuration.FacilityRangeMultiplier; set => Configuration.FacilityRangeMultiplier = value; }

        [GameParameters.CustomFloatParameterUI("Editor range multiplier", toolTip = "Multiplies the range of KCs custom KK editor.", addTextField = true, asPercentage = false, autoPersistance = false, logBase = 1.5f, gameMode = GameParameters.GameMode.ANY, minValue = 0.1f, maxValue = 10f, newGameOnly = false, displayFormat = "0.00")]
        public float EditorRangeMultiplier { get => Configuration.EditorRangeMultiplier; set => Configuration.EditorRangeMultiplier = value; }

        [GameParameters.CustomFloatParameterUI("Vessel build cost multiplier", toolTip = "Multiplies the build cost for all vessel built at colonies.", addTextField = true, asPercentage = false, autoPersistance = false, logBase = 1.5f, gameMode = GameParameters.GameMode.ANY, maxValue = 10f, newGameOnly = false, displayFormat = "0.00")]
        public float VesselCostMultiplier { get => Configuration.VesselCostMultiplier; set => Configuration.VesselCostMultiplier = value; }

        [GameParameters.CustomFloatParameterUI("Vessel build time multiplier", toolTip = "Multiplies the build time for all vessel built at colonies.", addTextField = true, asPercentage = false, autoPersistance = false, logBase = 1.5f, gameMode = GameParameters.GameMode.ANY, maxValue = 10f, newGameOnly = false, displayFormat = "0.00")]
        public float VesselTimeMultiplier { get => Configuration.VesselTimeMultiplier; set => Configuration.VesselTimeMultiplier = value; }

        [GameParameters.CustomIntParameterUI("Max colonies per body", toolTip = "Maximum number of colonies that can be built on a single body. Set to 0 to disable the limit.", autoPersistance = false, gameMode = GameParameters.GameMode.ANY, minValue = 0, maxValue = 15, newGameOnly = false)]
        public int maxColoniesPerBody { get => Configuration.MaxColoniesPerBody; set => Configuration.MaxColoniesPerBody = value; }

        [GameParameters.CustomParameterUI("Enable debug logging", toolTip = "Enables debug logging for Kerbal Colonies.", autoPersistance = false, gameMode = GameParameters.GameMode.ANY, newGameOnly = false)]
        public bool enableLogging { get => Configuration.enableLogging; set => Configuration.enableLogging = value; }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    FacilityCostMultiplier = 0.5f;
                    FacilityTimeMultiplier = 0.5f;
                    FacilityRangeMultiplier = 3.0f;
                    EditorRangeMultiplier = 3.0f;
                    VesselCostMultiplier = 0.5f;
                    VesselTimeMultiplier = 0.5f;
                    maxColoniesPerBody = 0;
                    break;
                case GameParameters.Preset.Normal:
                    FacilityCostMultiplier = 1.0f;
                    FacilityTimeMultiplier = 1.0f;
                    FacilityRangeMultiplier = 1.0f;
                    EditorRangeMultiplier = 1.0f;
                    VesselCostMultiplier = 1.0f;
                    VesselTimeMultiplier = 1.0f;
                    maxColoniesPerBody = 10;
                    break;
                case GameParameters.Preset.Moderate:
                    FacilityCostMultiplier = 1.5f;
                    FacilityTimeMultiplier = 1.5f;
                    FacilityRangeMultiplier = 0.9f;
                    EditorRangeMultiplier = 0.85f;
                    VesselCostMultiplier = 1.5f;
                    VesselTimeMultiplier = 1.5f;
                    maxColoniesPerBody = 5;
                    break;
                case GameParameters.Preset.Hard:
                    FacilityCostMultiplier = 2.0f;
                    FacilityTimeMultiplier = 2.0f;
                    FacilityRangeMultiplier = 0.75f;
                    EditorRangeMultiplier = 0.75f;
                    VesselCostMultiplier = 2.0f;
                    VesselTimeMultiplier = 2.0f;
                    maxColoniesPerBody = 3;
                    break;
                case GameParameters.Preset.Custom:
                default:
                    FacilityCostMultiplier = 1.0f;
                    FacilityTimeMultiplier = 1.0f;
                    FacilityRangeMultiplier = 1.0f;
                    EditorRangeMultiplier = 1.0f;
                    VesselCostMultiplier = 1.0f;
                    VesselTimeMultiplier = 1.0f;
                    maxColoniesPerBody = 10;
                    break;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            //Configuration.FacilityCostMultiplier = FacilityCostMultiplier;
            node.AddValue("FacilityCostMultiplier", FacilityCostMultiplier);
            node.AddValue("FacilityTimeMultiplier", FacilityTimeMultiplier);
            node.AddValue("FacilityRangeMultiplier", FacilityRangeMultiplier);
            node.AddValue("EditorRangeMultiplier", EditorRangeMultiplier);
            node.AddValue("VesselCostMultiplier", VesselCostMultiplier);
            node.AddValue("VesselTimeMultiplier", VesselTimeMultiplier);
            node.AddValue("maxColoniesPerBody", maxColoniesPerBody);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("FacilityCostMultiplier")) FacilityCostMultiplier = float.Parse(node.GetValue("FacilityCostMultiplier"));
            else FacilityCostMultiplier = 1.0f; // Default value if not set
            if (node.HasValue("FacilityTimeMultiplier")) FacilityTimeMultiplier = float.Parse(node.GetValue("FacilityTimeMultiplier"));
            else FacilityTimeMultiplier = 1.0f; // Default value if not set
            if (node.HasValue("FacilityRangeMultiplier")) FacilityRangeMultiplier = float.Parse(node.GetValue("FacilityRangeMultiplier"));
            else FacilityRangeMultiplier = 1.0f; // Default value if not set
            if (node.HasValue("EditorRangeMultiplier")) EditorRangeMultiplier = float.Parse(node.GetValue("EditorRangeMultiplier"));
            else EditorRangeMultiplier = 1.0f; // Default value if not set
            if (node.HasValue("VesselCostMultiplier")) VesselCostMultiplier = float.Parse(node.GetValue("VesselCostMultiplier"));
            else VesselCostMultiplier = 1.0f; // Default value if not set
            if (node.HasValue("VesselTimeMultiplier")) VesselTimeMultiplier = float.Parse(node.GetValue("VesselTimeMultiplier"));
            else VesselTimeMultiplier = 1.0f; // Default value if not set
            if (node.HasValue("maxColoniesPerBody")) maxColoniesPerBody = int.Parse(node.GetValue("maxColoniesPerBody"));
            else maxColoniesPerBody = 10; // Default value if not set
        }
    }

    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class KCGameParameterReset : MonoBehaviour
    {
        protected void Awake()
        {
            Configuration.FacilityCostMultiplier = 1.0f;
            Configuration.FacilityTimeMultiplier = 1.0f;
            Configuration.FacilityRangeMultiplier = 1.0f;
            Configuration.EditorRangeMultiplier = 1.0f;
            Configuration.VesselCostMultiplier = 1.0f;
            Configuration.VesselTimeMultiplier = 1.0f;
            Configuration.MaxColoniesPerBody = 10;
        }
    }
}
