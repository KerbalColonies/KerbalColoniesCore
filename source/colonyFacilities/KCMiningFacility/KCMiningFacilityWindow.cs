using KerbalColonies.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static KSP.UI.Screens.SpaceCenter.BuildingPicker;

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

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacilityWindow : KCFacilityWindowBase
    {
        KCMiningFacility miningFacility;
        public KerbalGUI kerbalGUI;

        private Vector2 resourceScrollPos = new Vector2();
        protected override void CustomWindow()
        {
            miningFacility.Colony.UpdateColony();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(miningFacility, true);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(300));
                {
                    kerbalGUI.StaffingInterface();

                    facility.enabled = GUILayout.Toggle(facility.enabled, "Enabled", GUILayout.Width(100));

                    if (facility.facilityInfo.ECperSecond[facility.level] > 0)
                    {
                        GUILayout.Space(10);
                        GUILayout.Label($"EC/s: {(facility.enabled ? facility.facilityInfo.ECperSecond[facility.level] * miningFacility.getKerbals().Count : 0):f2}");
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"EC Consumption Priority: {miningFacility.ECConsumptionPriority}", GUILayout.Height(18));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) miningFacility.ECConsumptionPriority--;
                            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) miningFacility.ECConsumptionPriority++;
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();

                Dictionary<PartResourceDefinition, double> maxPerResource = new Dictionary<PartResourceDefinition, double> { };
                miningFacility.miningFacilityInfo.rates.Where(kvp => kvp.Key <= miningFacility.level).ToList().ForEach(kvp => kvp.Value.ForEach(rate =>
                {
                    if (!maxPerResource.ContainsKey(rate.resource)) maxPerResource.Add(rate.resource, rate.max);
                    else maxPerResource[rate.resource] += rate.max;
                }));

                resourceScrollPos = GUILayout.BeginScrollView(resourceScrollPos);
                {
                    KCMiningFacilityInfo miningInfo = miningFacility.miningFacilityInfo;

                    miningFacility.storedResoures.ToList().ForEach(res =>
                    {
                        GUILayout.Label($"<size=20><b>{res.Key.displayName}</b></size>");
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical();
                            {
                                GUILayout.Label($"Daily rate: {(miningFacility.groupDensities.Sum(kvp => kvp.Value.ContainsKey(res.Key) ? kvp.Value[res.Key] : 0) * miningFacility.getKerbals().Count):f2}/day");
                                GUILayout.Label($"Stored: {res.Value:f2}");
                                GUILayout.Label($"Max: {maxPerResource[res.Key]:f2}");
                            }
                            GUILayout.EndVertical();
                            GUILayout.BeginVertical(GUILayout.Width(100));
                            {
                                if (GUILayout.Button($"Retrieve {res.Key.displayName}")) miningFacility.RetriveResource(res.Key);
                                if (GUILayout.Button($"Autotransfer {(miningFacility.autoTransferResources[res.Key] ? "on" : "off")}")) miningFacility.autoTransferResources[res.Key] = !miningFacility.autoTransferResources[res.Key];
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();


                        GUILayout.Space(10);
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        GUILayout.Space(10);
                    });
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }


        public KCMiningFacilityWindow(KCMiningFacility miningFacility) : base(miningFacility, Configuration.createWindowID())
        {
            this.miningFacility = miningFacility;
            toolRect = new Rect(100, 100, 800, 600);
            this.kerbalGUI = null;
        }
    }
}
