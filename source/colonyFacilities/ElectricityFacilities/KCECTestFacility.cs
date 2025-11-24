using KerbalColonies.ResourceManagment;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities.ElectricityFacilities
{
    public class KCECTestWindow : KCFacilityWindowBase
    {
        private KCECTestFacility ecFacility => (KCECTestFacility)facility;

        private List<double> valueList = [-10000, -1000, -100, -10, -1, 1, 10, 100, 1000, 10000];
        private List<int> ints = [-10, -5, -1, 1, 5, 10];
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

            GUILayout.Label($"Current priority: {ecFacility.ResourceConsumptionPriority}");

            GUILayout.BeginHorizontal();
            foreach (int i in ints)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change priority by {i} for {ecFacility.DisplayName} facility");
                    ecFacility.ResourceConsumptionPriority += i;
                }
            }
            GUILayout.EndHorizontal();
        }

        public KCECTestWindow(KCECTestFacility facility) : base(facility, Configuration.createWindowID())
        {
            toolRect = new Rect(100, 100, 400, 800);
        }
    }

    public class KCECTestFacility : KCFacilityBase, IKCResourceProducer, IKCResourceConsumer
    {
        private KCECTestWindow window;

        public double ECProduced { get; set; } = 0.0;

        public double DailyECConsumption() => Math.Max(0, -ECProduced) * 60 * 60 * 6;

        public int ResourceConsumptionPriority { get; set; } = 0;

        public Dictionary<PartResourceDefinition, double> ResourceProduction(double lastTime, double deltaTime, double currentTime) => new() { { PartResourceLibrary.Instance.GetDefinition("ElectricCharge"), Math.Max(0, ECProduced) * deltaTime } };

        public Dictionary<PartResourceDefinition, double> ResourcesPerSecond() => new() { { PartResourceLibrary.Instance.GetDefinition("ElectricCharge"), Math.Max(0, ECProduced) } };

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => new() { { PartResourceLibrary.Instance.GetDefinition("ElectricCharge"), Math.Max(0, -ECProduced) * deltaTime } };

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime)
        {
        }

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            if (ECProduced >= 0) return [];
            Configuration.writeDebug($"Insufficient EC in {DisplayName} facility.");
            ECProduced = 0;

            return [];
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond()
        {
            throw new NotImplementedException();
        }

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
