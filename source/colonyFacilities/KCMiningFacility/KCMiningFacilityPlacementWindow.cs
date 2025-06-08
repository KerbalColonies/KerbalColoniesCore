using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacilityPlacementWindow : KCWindowBase
    {
        private static KCMiningFacilityPlacementWindow instance;
        public static KCMiningFacilityPlacementWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KCMiningFacilityPlacementWindow();
                }
                return instance;
            }
        }

        public Dictionary<PartResourceDefinition, double> newRates { get; set; } = new Dictionary<PartResourceDefinition, double> { };

        Vector2 scrollPos = new Vector2();
        protected override void CustomWindow()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                newRates.ToList().ForEach(rate => GUILayout.Label($"{rate.Key.displayName}: {rate.Value}/day"));
            }
            GUILayout.EndScrollView();
        }

        public KCMiningFacilityPlacementWindow() : base(Configuration.createWindowID(), "Miningfacility placement info")
        {
            this.toolRect = new Rect(100, 100, 400, 300);
        }
    }
}
