using KerbalColonies.colonyFacilities.KCMiningFacility;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Windturbine
{
    public class KCWindturbinePlacementWindow : KCWindowBase
    {
        private static KCWindturbinePlacementWindow instance;

        public static KCWindturbinePlacementWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KCWindturbinePlacementWindow();
                }
                return instance;
            }
        }

        public double ECProductionRate { get; set; } = 0.0;
        public double Density { get; set; } = 0.0;
        public double Pressure { get; set; } = 0.0;
        public double Temperature { get; set; } = 0.0;

        protected override void CustomWindow()
        {
            GUILayout.Label($"Wind turbine production rate: {ECProductionRate} EC/s");
            GUILayout.Label($"Density: {Density} kg/m³");
            GUILayout.Label($"Pressure: {Pressure} Pa");
        }

        public KCWindturbinePlacementWindow() : base(Configuration.createWindowID(), "Wind turbine placement info")
        {
            this.toolRect = new Rect(100, 100, 300, 200);
        }
    }
}
