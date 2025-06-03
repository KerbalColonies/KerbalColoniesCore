using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.Settings
{
    public class KCGameParameters : GameParameters.CustomParameterNode
    {
        public override string Title { get => "Kerbal Colonies Settings"; }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Kerbal Colonies Settings"; } }
        public override string DisplaySection { get { return "Kerbal Colonies Settings"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        [GameParameters.CustomFloatParameterUI("Facility build cost multiplier", toolTip = "Multiplies the build cost for all facilities and new colonies.", addTextField = true, asPercentage = false, autoPersistance = false, gameMode = GameParameters.GameMode.ANY, maxValue = 5f, minValue = 0.1f, newGameOnly = false, displayFormat = "0.00")]
        public float FacilityCostMultiplier = 1.0f;

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
                    break;
                case GameParameters.Preset.Normal:
                    FacilityCostMultiplier = 1.0f;
                    break;
                case GameParameters.Preset.Moderate:
                    FacilityCostMultiplier = 1.5f;
                    break;
                case GameParameters.Preset.Hard:
                    FacilityCostMultiplier = 2.0f;
                    break;
                case GameParameters.Preset.Custom:
                default:
                    FacilityCostMultiplier = 1.0f;
                    break;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            Configuration.FacilityCostMultiplier = FacilityCostMultiplier;
            node.AddValue("FacilityCostMultiplier", FacilityCostMultiplier);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("FacilityCostMultiplier"))
            {
                FacilityCostMultiplier = float.Parse(node.GetValue("FacilityCostMultiplier"));
                Configuration.FacilityCostMultiplier = FacilityCostMultiplier; // Update the static variable
            }
            else
            {
                FacilityCostMultiplier = 1.0f; // Default value if not set
            }
        }
    }
}
