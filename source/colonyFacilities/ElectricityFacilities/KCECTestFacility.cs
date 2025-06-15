using KerbalColonies.Electricity;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static KSP.UI.Screens.Settings.SettingsSetup;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities
{
    public class KCECTestWindow : KCFacilityWindowBase
    {
        KCECTestFacility ecFacility => (KCECTestFacility)facility;

        List<double> valueList = new List<double> { -10000, -1000, -100, -10, -1, 1, 10, 100, 1000, 10000 };
        List<int> ints = new List<int> { -10, -5, -1, 1, 5, 10 };
        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();
            GUILayout.Label($"Current production: {ecFacility.ECProduced}");

            GUILayout.BeginHorizontal();
            foreach (double i in valueList)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change ECRate by {i} for {ecFacility.DisplayName} facility");
                    ecFacility.ECProduced += i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Current priority: {ecFacility.ECConsumptionPriority}");

            GUILayout.BeginHorizontal();
            foreach (int i in ints)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change priority by {i} for {ecFacility.DisplayName} facility");
                    ecFacility.ECConsumptionPriority += i;
                }
            }
            GUILayout.EndHorizontal();
        }

        public KCECTestWindow(KCECTestFacility facility) : base(facility, Configuration.createWindowID())
        {
            toolRect = new Rect(100, 100, 400, 800);
        }
    }

    public class KCECTestFacility : KCFacilityBase, KCECProducer, KCECConsumer
    {
        KCECTestWindow window;

        public double ECProduced { get; set; } = 0.0;

        public double ECProduction(double lastTime, double deltaTime, double currentTime) => Math.Max(0, ECProduced) * deltaTime;

        public double DailyECProduction() => ECProduced * 60 * 60 * 6;


        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => Math.Min(ECProduced, 0) * deltaTime;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) { }

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC)
        {
            if (ECProduced >= 0) return;
            Configuration.writeDebug($"Insufficient EC in {DisplayName} facility. Remaining EC: {remainingEC}");
            ECProduced = 0;
        }

        public double DailyECConsumption() => Math.Max(0, ECProduced) * 60 * 60 * 6;

        public int ECConsumptionPriority { get; set; } = 0;



        public override void OnBuildingClicked() => window.Toggle();

        public override void OnRemoteClicked() => window.Toggle();

        public KCECTestFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            window = new KCECTestWindow(this);
        }

        public KCECTestFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            window = new KCECTestWindow(this);
        }
    }
}
