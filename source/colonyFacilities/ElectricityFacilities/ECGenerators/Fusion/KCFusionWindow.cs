using KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fission;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fusion
{
    public class KCFusionWindow : KCFacilityWindowBase
    {
        public KCFusionReactor fusionReactor;
        public KerbalGUI kerbalGUI;

        float manualLevel = 0;
        Vector2 scrollPosPowerLevels = Vector2.zero;
        Vector2 inputScrollPos = Vector2.zero;
        Vector2 outputScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            fusionReactor.Colony.UpdateColony();

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
                    GUILayout.Label($"Current power output: {fusionReactor.ActualLastECPerSecond} EC/s");
                    GUILayout.Label($"Current power level: {fusionReactor.lastPowerLevel.Key}");
                    GUILayout.Label($"Current throttle: {fusionReactor.currentThrottle}");

                    SortedDictionary<int, double> powerLevels = fusionReactor.AvailablePowerLevels();

                    GUILayout.Label("Available Power Levels:");
                    scrollPosPowerLevels = GUILayout.BeginScrollView(scrollPosPowerLevels, GUILayout.Height(125));
                    {
                        powerLevels.ToList().ForEach(kvp => GUILayout.Label($"Power Level: {fusionReactor.FusionInfo.MinKerbals[kvp.Key]} Kerbals = {kvp.Value} EC/s"));
                    }
                    GUILayout.EndScrollView();


                    if (GUILayout.Toggle(fusionReactor.ManualControl, "Manual control"))
                    {
                        if (!fusionReactor.ManualControl)
                        {
                            fusionReactor.ManualControl = true;
                            fusionReactor.ManualPowerLevel = fusionReactor.lastPowerLevel.Key;
                            if (fusionReactor.ManualPowerLevel < 0) fusionReactor.ManualPowerLevel = 0;
                            manualLevel = fusionReactor.ManualPowerLevel;
                            fusionReactor.ManualThrottle = fusionReactor.currentThrottle;
                            if (fusionReactor.ManualThrottle == 0) fusionReactor.ManualThrottle = fusionReactor.FusionInfo.MinECThrottle[fusionReactor.ManualPowerLevel];

                        }
                    }
                    else
                    {
                        fusionReactor.ManualControl = false;
                    }
                    GUI.enabled = fusionReactor.ManualControl;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"Level: {fusionReactor.ManualPowerLevel}");
                        GUILayout.FlexibleSpace();
                        manualLevel = GUILayout.HorizontalSlider(manualLevel, powerLevels.First().Key, powerLevels.Last().Key, GUILayout.Width(300));
                        fusionReactor.ManualPowerLevel = (int)Math.Round(manualLevel, 0);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"Throttle: {fusionReactor.ManualThrottle}");
                        GUILayout.FlexibleSpace();
                        fusionReactor.ManualThrottle = Math.Round(GUILayout.HorizontalSlider((float)fusionReactor.ManualThrottle, (float)fusionReactor.FusionInfo.MinECThrottle[fusionReactor.ManualPowerLevel], 1, GUILayout.Width(300)), 3);
                    }
                    GUILayout.EndHorizontal();


                    GUI.enabled = true;
                    fusionReactor.Active = GUILayout.Toggle(fusionReactor.Active, "Reactor active");
                    GUILayout.BeginHorizontal(GUILayout.Height(155));
                    {
                        GUILayout.BeginVertical();
                        {
                            {
                                GUILayout.Label("Input Resources:");
                                inputScrollPos = GUILayout.BeginScrollView(inputScrollPos);
                                {
                                    fusionReactor.FusionInfo.InputResources[fusionReactor.lastPowerLevel.Key].ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value}/s * throttle"));
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
                                fusionReactor.FusionInfo.OutputResources[fusionReactor.lastPowerLevel.Key].ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value}/s * throttle"));
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

        public KCFusionWindow(KCFusionReactor reactor) : base(reactor, Configuration.createWindowID())
        {
            fusionReactor = reactor;
            kerbalGUI = new KerbalGUI(reactor, true);
            toolRect = new Rect(100, 100, 800, 600);
        }
    }
}
