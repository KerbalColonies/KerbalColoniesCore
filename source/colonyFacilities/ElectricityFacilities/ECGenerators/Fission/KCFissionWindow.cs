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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fission
{
    public class KCFissionWindow : KCFacilityWindowBase
    {
        public KCFissionReactor fissionReactor;
        public KerbalGUI kerbalGUI;

        float manualLevel = 0;
        Vector2 scrollPosPowerLevels = Vector2.zero;
        Vector2 inputScrollPos = Vector2.zero;
        Vector2 outputScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            fissionReactor.Colony.UpdateColony();

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(350));
                {
                    kerbalGUI.StaffingInterface();
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
                GUILayout.BeginVertical(GUILayout.Width(420));
                {
                    GUILayout.Label($"Current power output: {fissionReactor.lastECPerSecond} EC/s");
                    GUILayout.Label($"Current power level: {fissionReactor.lastPowerLevel.Key}");
                    GUILayout.Label($"Current throttle: {fissionReactor.currentThrottle}");

                    SortedDictionary<int, double> powerLevels = fissionReactor.AvailablePowerLevels();

                    GUILayout.Label("Available Power Levels:");
                    scrollPosPowerLevels = GUILayout.BeginScrollView(scrollPosPowerLevels, GUILayout.Height(125));
                    {
                        powerLevels.ToList().ForEach(kvp => GUILayout.Label($"Power Level: {fissionReactor.FissionInfo.MinKerbals[kvp.Key]} Kerbals = {kvp.Value} EC/s"));
                    }
                    GUILayout.EndScrollView();


                    if (GUILayout.Toggle(fissionReactor.ManualControl, "Manual control"))
                    {
                        if (!fissionReactor.ManualControl)
                        {
                            fissionReactor.ManualControl = true;
                            fissionReactor.ManualPowerLevel = fissionReactor.lastPowerLevel.Key;
                            if (fissionReactor.ManualPowerLevel < 0) fissionReactor.ManualPowerLevel = 0;
                            manualLevel = fissionReactor.ManualPowerLevel;
                            fissionReactor.ManualThrottle = fissionReactor.currentThrottle;
                            if (fissionReactor.ManualThrottle == 0) fissionReactor.ManualThrottle = fissionReactor.FissionInfo.MinECThrottle[fissionReactor.ManualPowerLevel];

                        }
                    }
                    else
                    {
                        fissionReactor.ManualControl = false;
                    }
                    GUI.enabled = fissionReactor.ManualControl;
                    SortedDictionary<int, double> possiblePowerLevels = fissionReactor.PossiblePowerLevels();
                    if (possiblePowerLevels.Count == 0)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Level: 0");
                            GUILayout.FlexibleSpace();
                            manualLevel = 0;
                            manualLevel = GUILayout.HorizontalSlider(manualLevel, 0, 0, GUILayout.Width(300));
                            fissionReactor.ManualPowerLevel = (int)Math.Round(manualLevel, 0);
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"Level: {fissionReactor.ManualPowerLevel}");
                            GUILayout.FlexibleSpace();
                            manualLevel = GUILayout.HorizontalSlider(manualLevel, possiblePowerLevels.First().Key, possiblePowerLevels.Last().Key, GUILayout.Width(300));
                            fissionReactor.ManualPowerLevel = (int)Math.Round(manualLevel, 0);
                        }
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"Throttle: {fissionReactor.ManualThrottle}");
                        GUILayout.FlexibleSpace();
                        fissionReactor.ManualThrottle = Math.Round(GUILayout.HorizontalSlider((float)fissionReactor.ManualThrottle, (float)fissionReactor.FissionInfo.MinECThrottle[fissionReactor.ManualPowerLevel], 1, GUILayout.Width(300)), 3);
                        fissionReactor.ManualThrottle = Math.Max(fissionReactor.ManualThrottle, fissionReactor.FissionInfo.MinECThrottle[fissionReactor.ManualPowerLevel]);
                    }
                    GUILayout.EndHorizontal();


                    GUI.enabled = !fissionReactor.Refilling;
                    fissionReactor.Active = GUILayout.Toggle(fissionReactor.Active, "Reactor active");
                    GUI.enabled = !fissionReactor.Active && !fissionReactor.ShuttingDown && !fissionReactor.Refilling;
                    if (GUILayout.Button("Refill Reactor"))
                        fissionReactor.Refill();
                    GUI.enabled = true;
                    GUILayout.BeginHorizontal(GUILayout.Height(155));
                    {
                        GUILayout.BeginVertical();
                        {
                            {
                                GUILayout.Label("Input Resources:");
                                inputScrollPos = GUILayout.BeginScrollView(inputScrollPos);
                                {
                                    fissionReactor.StoredInput.ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.name}: {kvp.Value}/{fissionReactor.FissionInfo.InputStorage[fissionReactor.level][kvp.Key]}"));
                                }
                                GUILayout.EndScrollView();
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();
                        {
                            GUILayout.Label("Output Resources:");
                            outputScrollPos = GUILayout.BeginScrollView(outputScrollPos);
                            {
                                fissionReactor.StoredOutput.ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.name}: {kvp.Value}/{fissionReactor.FissionInfo.OutputStorage[fissionReactor.level][kvp.Key]}"));
                            }
                            GUILayout.EndScrollView();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public KCFissionWindow(KCFissionReactor reactor) : base(reactor, Configuration.createWindowID())
        {
            fissionReactor = reactor;
            kerbalGUI = new KerbalGUI(reactor, true);
            toolRect = new Rect(100, 100, 800, 600);
            manualLevel = fissionReactor.ManualPowerLevel;
        }
    }
}
