using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECGenerators.Fission
{
    public class KCFissionWindow : KCFacilityWindowBase
    {
        public KCFissionReactor fissionReactor;
        public KerbalGUI kerbalGUI;

        Vector2 scrollPosPowerLevels = Vector2.zero;
        Vector2 inputScrollPos = Vector2.zero;
        Vector2 outputScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            fissionReactor.Colony.UpdateColony();
            kerbalGUI.StaffingInterface();

            GUILayout.Label($"Current power output: {fissionReactor.lastECPerSecond} EC/s");

            GUILayout.Label("Available Power Levels:");
            scrollPosPowerLevels = GUILayout.BeginScrollView(scrollPosPowerLevels);
            {
                fissionReactor.AvailablePowerLevels().ToList().ForEach(kvp => GUILayout.Label($"Power Level: {fissionReactor.FissionInfo.MinKerbals[kvp.Key]} Kerbals = {kvp.Value} EC/s"));
            }
            GUILayout.EndScrollView();

            fissionReactor.Active = GUILayout.Toggle(fissionReactor.Active, "Reactor active");
            if (fissionReactor.Refilling) GUI.enabled = false;
            if (GUILayout.Button("Refill Reactor"))
                fissionReactor.Refill();
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            {
                inputScrollPos = GUILayout.BeginScrollView(inputScrollPos, GUILayout.Height(100));
                {
                    GUILayout.Label("Input Resources:");
                    fissionReactor.StoredInput.ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.name}: {kvp.Value}/{fissionReactor.FissionInfo.InputStorage[fissionReactor.level][kvp.Key]}"));
                }
                GUILayout.EndScrollView();
                outputScrollPos = GUILayout.BeginScrollView(outputScrollPos, GUILayout.Height(100));
                {
                    GUILayout.Label("Output Resources:");
                    fissionReactor.StoredOutput.ToList().ForEach(kvp => GUILayout.Label($"{kvp.Key.name}: {kvp.Value}/{fissionReactor.FissionInfo.OutputStorage[fissionReactor.level][kvp.Key]}"));
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }

        public KCFissionWindow(KCFissionReactor reactor) : base(reactor, Configuration.createWindowID())
        {
            fissionReactor = reactor;
            kerbalGUI = new KerbalGUI(reactor, true);
            toolRect = new Rect(100, 100, 700, 1000);
        }
    }
}
