using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.Electricity;
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

namespace KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage
{
    public class KCECStorageWindow : KCFacilityWindowBase
    {
        PartResourceDefinition ec;

        KCECStorageFacility ecStorage => (KCECStorageFacility)facility;

        List<double> valueList = new List<double> { -10000, -1000, -100, -10, -1, 1, 10, 100, 1000, 10000 };
        List<int> ints = new List<int> { -10, -5, -1, 1, 5, 10 };

        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();

            GUILayout.Label($"Stored electric charge in this colony:\n{KCECStorageFacility.ColonyEC(ecStorage.Colony):f2} / {KCECStorageFacility.ColonyECCapacity(ecStorage.Colony):f2} EC");
            GUILayout.Label($"Stored electric charge:\n{ecStorage.ECStored} / {ecStorage.ECCapacity} EC");

            GUILayout.Label("Colony: ");

            KCECStorageInfo info = ecStorage.StorageInfo;

            CelestialBody body = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == ecStorage.Colony.BodyID);

            double radius = facility.KKgroups.Average(g => KerbalKonstructs.API.GetGroupCenter(g, body.bodyName).RadiusOffset) + body.Radius;
            double squareRadius = radius * radius;
            double unMultiplier = body.gMagnitudeAtCenter / squareRadius;

            float multiplier = info.UseGravityMultiplier[facility.level] ? Math.Max(info.MinGravity[facility.level], Math.Min(info.MaxGravity[facility.level], (float)unMultiplier / 9.80665f)) : 1;
            if (info.UseGravityMultiplier[facility.level] && !Configuration.Paused) Configuration.writeDebug($"KCECStorageWindow: radius: {radius}, radius²: {squareRadius}, unMultiplier: {unMultiplier}");


            List<Type> types = info.RangeTypes[facility.level];
            List<string> names = info.RangeFacilities[facility.level];

            float range = info.TransferRange[facility.level] * multiplier * Configuration.FacilityRangeMultiplier;
            bool canTranfer = facility.Colony.Facilities.Where(f => types.Contains(f.GetType()) ^ names.Contains(f.facilityInfo.name)).Any(f => f.playerNearFacility((float)range)) || facility.playerNearFacility((float)range);
            canTranfer &= !ecStorage.locked;
            canTranfer &= FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.LandedOrSplashed && FlightGlobals.ship_srfSpeed <= 0.5; // only allow transfer if the vessel is landed or splashed


            // types | names
            // 0 | 0 -> false
            // 0 | 1 -> true
            // 1 | 0 -> true
            // 1 | 1 -> false
            // xor


            GUILayout.BeginHorizontal();
            {
                GUI.enabled = canTranfer;
                foreach (double i in valueList)
                {
                    if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.MinWidth(24)))
                    {
                        Configuration.writeLog($"Transfering {i} EC from colony {ecStorage.Colony.DisplayName} to vessel {FlightGlobals.ActiveVessel.vesselName}.");

                        if (i < 0)
                        {
                            if (KCStorageFacilityWindow.vesselHasSpace(FlightGlobals.ActiveVessel, ec, -i))
                            {
                                double left = KCECStorageFacility.AddECToColony(ecStorage.Colony, i);

                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, i - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                            else
                            {
                                double amount = KCStorageFacilityWindow.getVesselSpace(FlightGlobals.ActiveVessel, ec);
                                double left = KCECStorageFacility.AddECToColony(ecStorage.Colony, -amount);

                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, -amount - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }

                        }
                        else
                        {
                            if (KCStorageFacilityWindow.vesselHasRessources(FlightGlobals.ActiveVessel, ec, i))
                            {
                                double left = KCECStorageFacility.AddECToColony(ecStorage.Colony, i);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, i - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                            else
                            {
                                double amount = KCStorageFacilityWindow.getVesselRessources(FlightGlobals.ActiveVessel, ec);
                                double left = KCECStorageFacility.AddECToColony(ecStorage.Colony, amount);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, amount - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.Label("Facility: ");
            GUI.enabled = canTranfer;
            GUILayout.BeginHorizontal();
            {
                if (FlightGlobals.ActiveVessel == null) GUI.enabled = false;
                foreach (double i in valueList)
                {
                    if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.MinWidth(24)))
                    {
                        Configuration.writeLog($"Transfering {i} EC from EC storage facility {ecStorage.DisplayName} to vessel {FlightGlobals.ActiveVessel.vesselName}.");

                        if (i < 0)
                        {
                            if (KCStorageFacilityWindow.vesselHasSpace(FlightGlobals.ActiveVessel, ec, -i))
                            {
                                double left = ecStorage.ChangeECStored(i);

                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, i - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                            else
                            {
                                double amount = KCStorageFacilityWindow.getVesselSpace(FlightGlobals.ActiveVessel, ec);
                                double left = ecStorage.ChangeECStored(-amount);

                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, -amount - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }

                        }
                        else
                        {
                            if (KCStorageFacilityWindow.vesselHasRessources(FlightGlobals.ActiveVessel, ec, i))
                            {
                                double left = ecStorage.ChangeECStored(i);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, i - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                            else
                            {
                                double amount = KCStorageFacilityWindow.getVesselRessources(FlightGlobals.ActiveVessel, ec);
                                double left = ecStorage.ChangeECStored(amount);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(ec.id, amount - left, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                            }
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;

            ecStorage.locked = GUILayout.Toggle(ecStorage.locked, "Lock storage", GUILayout.Height(18), GUILayout.Width(100));

            GUILayout.Space(10);

            GUILayout.Label($"Current priority: {ecStorage.ECStoragePriority}");
            GUILayout.BeginHorizontal();
            foreach (int i in ints)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    Configuration.writeDebug($"Change priority by {i} for {ecStorage.DisplayName} facility");
                    ecStorage.ECStoragePriority += i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Current EC delta: {(KCECManager.colonyEC[ecStorage.Colony].lastECDelta / KCECManager.colonyEC[ecStorage.Colony].deltaTime):f2} EC/s");
        }

        public KCECStorageWindow(KCECStorageFacility facility) : base(facility, Configuration.createWindowID())
        {
            toolRect = new Rect(100, 100, 400, 350);
            ec = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
        }
    }
}
