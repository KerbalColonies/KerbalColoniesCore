using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.Settings;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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

/// This file contains parts from the Kerbal Konstructs mod Hangar.cs file which is licensed under the MIT License.
/// The general idea on how to store vessels is also taken from the Kerbal Konstructs mod

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.colonyFacilities.HangarFacility
{
    public class KCHangarFacilityWindow : KCFacilityWindowBase
    {
        private KCHangarFacility hangar;
        private bool CanStoreVessel;
        private double TestTime;
        private int MaxPermutations = 8192;
        private int MaxProcessors;
        private Vector2 scrollPos;
        private Vector2 resourceUsageScrollPos;
        protected override void CustomWindow()
        {
            hangar.Colony.UpdateColony();
            List<StoredVessel> vesselList = hangar.storedVessels.ToList();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            vesselList.ForEach(vessel =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(vessel.vesselName);

                GUILayout.FlexibleSpace();

                if (vessel.vesselBuildTime == null)
                {
                    if (GUILayout.Button("Load", GUILayout.Width(150)))
                    {
                        Vessel v = hangar.RollOutVessel(vessel).vesselRef;
                    }
                }
                else
                {
                    GUILayout.Label($"Build time: {vessel.entireVesselBuildTime - vessel.vesselBuildTime:f2}/{vessel.entireVesselBuildTime:f2}");
                }

                if (GUILayout.Button("<b>x</b>", UIConfig.ButtonRed))
                {
                    Configuration.writeLog($"Removing vessel {vessel.vesselName} from hangar {hangar.name}");
                    hangar.storedVessels.Remove(vessel);
                }
                GUILayout.EndHorizontal();
            });
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (hangar.playerNearFacility())
            {
                GUILayout.Space(10);

                if (!CanStoreVessel || TestTime + 600 < Planetarium.GetUniversalTime())
                {
                    CanStoreVessel = false;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Max permutations to test:");
                    if (int.TryParse(GUILayout.TextField(MaxPermutations.ToString()), out int permutationRes))
                    {
                        MaxPermutations = permutationRes;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Max processors to use:");
                    if (int.TryParse(GUILayout.TextField(MaxProcessors.ToString()), out int procRes))
                    {
                        MaxProcessors = procRes;
                    }
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Test"))
                    {
                        TestTime = Planetarium.GetUniversalTime();
                        CanStoreVessel = hangar.CanStoreVessel(FlightGlobals.ActiveVessel, MaxPermutations);
                    }
                }
                else
                {
                    if (GUILayout.Button("Store vessel"))
                    {
                        hangar.StoreVessel(FlightGlobals.ActiveVessel, null);
                    }
                }
            }

            GUI.enabled = true;

            GUILayout.Space(5);

            hangar.enabled = GUILayout.Toggle(hangar.enabled, "Enable hangar", GUILayout.Height(18));
            GUILayout.Space(10);

            if (facility.facilityInfo.ResourceUsage[facility.level].Count > 0)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Resource Consumption Priority: {hangar.ResourceConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) hangar.ResourceConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) hangar.ResourceConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Resource usage:");
                resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(120));
                {
                    hangar.ResourceConsumptionPerSecond().ToList().ForEach(kvp =>
                        GUILayout.Label($"- {kvp.Key.displayName}: {kvp.Value}/s")
                    );
                }
                GUILayout.EndScrollView();
            }
        }

        public KCHangarFacilityWindow(KCHangarFacility hangar) : base(hangar, Configuration.createWindowID())
        {
            this.hangar = hangar;
            toolRect = new Rect(100, 100, 450, 800);

            MaxProcessors = Environment.ProcessorCount;
        }
    }
}
