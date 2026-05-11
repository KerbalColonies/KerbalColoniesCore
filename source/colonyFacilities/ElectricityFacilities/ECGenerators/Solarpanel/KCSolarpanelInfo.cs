using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.SolarPanel
{
    public class KCSolarpanelInfo : KCFacilityInfoClass
    {
        public double MaxEfficiency { get; set; } = 1.0;
        public double PanelAngle { get; set; } = 0.0;
        public bool SunTracking { get; set; } = false;

        public KCSolarpanelInfo(ConfigNode node) : base(node)
        {
            if (node.HasValue("MaxEfficiency")) MaxEfficiency = double.Parse(node.GetValue("MaxEfficiency"));
            if (node.HasValue("PanelAngle")) PanelAngle = double.Parse(node.GetValue("PanelAngle"));
            if (node.HasValue("SunTracking")) SunTracking = bool.Parse(node.GetValue("SunTracking"));
        }
    }
}
