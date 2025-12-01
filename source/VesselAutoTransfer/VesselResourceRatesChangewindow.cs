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

namespace KerbalColonies.VesselAutoTransfer
{
    public class VesselResourceRatesChangewindow : KCWindowBase
    {
        private ModuleKCTransfer transferModule = null;

        protected Dictionary<PartResourceDefinition, string> rateStrings = [];
        protected Dictionary<PartResourceDefinition, string> colonyLimitStrings = [];
        protected Dictionary<PartResourceDefinition, string> vesselLimitStrings = [];
        protected Dictionary<PartResourceDefinition, bool> disableIfColonyLimit = [];
        protected Dictionary<PartResourceDefinition, bool> disableIfVesselLimit = [];

        public KCTransferInfo transfer => transferModule.transferInfo;
        private Vector2 scrollPos = Vector2.zero;
        private Vector2 scrollPosTransferMode = Vector2.zero;

        protected override void OnOpen()
        {
            scrollPos = Vector2.zero;
            rateStrings.Clear();
            colonyLimitStrings.Clear();
            vesselLimitStrings.Clear();
            disableIfColonyLimit.Clear();
            disableIfVesselLimit.Clear();

            List<PartResourceDefinition> allResources = [];

            foreach (PartResourceDefinition item in PartResourceLibrary.Instance.resourceDefinitions)
            {
                transferModule.part.GetConnectedResourceTotals(item.id, transferModule.transferMode, out double amount, out double max);

                if (max > 0)
                {
                    allResources.Add(item);
                    if (transfer.resources.Contains(item))
                    {
                        rateStrings.Add(item, transfer.ResourcesTarget[item].ToString());

                        colonyLimitStrings.Add(item, transfer.ColonyTransferLimits[item].ToString());
                        vesselLimitStrings.Add(item, transfer.VesselTransferLimits[item].ToString());
                        disableIfColonyLimit.Add(item, transfer.DisableIfColonyConstrains[item]);
                        disableIfVesselLimit.Add(item, transfer.DisableIfVesselConstrains[item]);
                    }
                    else
                    {
                        transfer.AddResource(item);
                        rateStrings.TryAdd(item, "0");
                        colonyLimitStrings.TryAdd(item, "0.5");
                        vesselLimitStrings.TryAdd(item, "0.5");
                        disableIfColonyLimit.TryAdd(item, false);
                        disableIfVesselLimit.TryAdd(item, false);
                    }
                }
            }
        }

        protected override void CustomWindow()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(20);
                GUILayout.Label("Rate", GUILayout.Width(100));
                GUILayout.Label("Colony Limit", GUILayout.Width(100));
                GUILayout.Label("Vessel Limit", GUILayout.Width(100));
                GUILayout.Space(8);
                GUILayout.Label("Disable if colony constrains", GUILayout.Width(180));
                GUILayout.Label("Disable if vessel constrains", GUILayout.Width(210));
                GUILayout.Label("Confirm", GUILayout.Width(220));
            }
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                rateStrings.ToList().ForEach(kvp =>
                {
                    GUILayout.BeginHorizontal();
                    {
                        rateStrings[kvp.Key] = GUILayout.TextField(rateStrings[kvp.Key], GUILayout.Width(100));
                        colonyLimitStrings[kvp.Key] = GUILayout.TextField(colonyLimitStrings[kvp.Key], GUILayout.Width(100));
                        vesselLimitStrings[kvp.Key] = GUILayout.TextField(vesselLimitStrings[kvp.Key], GUILayout.Width(100));
                        disableIfColonyLimit[kvp.Key] = GUILayout.Toggle(disableIfColonyLimit[kvp.Key], "Disable if colony constrains", GUILayout.Width(210));
                        disableIfVesselLimit[kvp.Key] = GUILayout.Toggle(disableIfVesselLimit[kvp.Key], "Disable if vessel constrains", GUILayout.Width(210));

                        if (GUILayout.Button(kvp.Key.name, GUILayout.Width(220)))
                        {
                            transfer.DisableIfColonyConstrains[kvp.Key] = disableIfColonyLimit[kvp.Key];
                            transfer.DisableIfVesselConstrains[kvp.Key] = disableIfVesselLimit[kvp.Key];

                            if (double.TryParse(colonyLimitStrings[kvp.Key], out double colonyLimit))
                            {
                                Configuration.writeLog($"Changed colony limit of {kvp.Key.name} to {colonyLimit}");
                                colonyLimitStrings[kvp.Key] = colonyLimit.ToString();
                                transfer.ColonyTransferLimits[kvp.Key] = Math.Max(0, Math.Min(1, colonyLimit));
                            }

                            if (double.TryParse(vesselLimitStrings[kvp.Key], out double vesselLimit))
                            {
                                Configuration.writeLog($"Changed colony limit of {kvp.Key.name} to {vesselLimit}");
                                vesselLimitStrings[kvp.Key] = vesselLimit.ToString();
                                transfer.VesselTransferLimits[kvp.Key] = Math.Max(0, Math.Min(1, vesselLimit));
                            }

                            if (double.TryParse(rateStrings[kvp.Key], out double rate))
                            {
                                Configuration.writeLog($"Changed transfer rate of {kvp.Key.name} to {rate} units/second.");
                                rateStrings[kvp.Key] = rate.ToString();

                                transfer.ResourcesTarget[kvp.Key] = rate;
                            }
                            else
                            {
                                Configuration.writeLog($"ERROR: Could not parse {rateStrings[kvp.Key]} as a double.");
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                });
            }
            GUILayout.EndScrollView();

            GUILayout.Space(16);

            GUILayout.Label("Current Transfer Mode: " + transferModule.transferMode.ToString());
            scrollPosTransferMode = GUILayout.BeginScrollView(scrollPosTransferMode, GUILayout.Height(150));
            {
                foreach (ResourceFlowMode mode in Enum.GetValues(typeof(ResourceFlowMode)))
                {
                    if (mode != transferModule.transferMode)
                    {
                        if (GUILayout.Toggle(false, mode.ToString()))
                        {
                            transferModule.transferMode = mode;
                        }
                    }
                    else
                    {
                        GUILayout.Toggle(true, mode.ToString());
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        protected override void OnClose()
        {
            transfer?.CleanResources();
        }

        public VesselResourceRatesChangewindow(ModuleKCTransfer transferModule) : base(Configuration.createWindowID(), "Change resource rates", false)
        {
            this.transferModule = transferModule;
            toolRect = new Rect(100, 100, 1000, 500);
        }
    }
}
