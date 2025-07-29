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

namespace KerbalColonies.colonyFacilities.StorageFacility
{
    public class KCStorageFacilityWindow : KCFacilityWindowBase
    {
        KCStorageFacility storageFacility;
        public HashSet<PartResourceDefinition> allResources = new HashSet<PartResourceDefinition>();
        protected Vector2 scrollPos;

        public void GetVesselResources()
        {
            if (FlightGlobals.ActiveVessel == null) { return; }

            double amount = 0;
            double maxAmount = 0;
            foreach (PartResourceDefinition availableResource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (KCStorageFacility.blackListedResources.Contains(availableResource.name)) { continue; }
                foreach (var partSet in FlightGlobals.ActiveVessel.crossfeedSets)
                {
                    partSet.GetConnectedResourceTotals(availableResource.id, out amount, out maxAmount, true);
                    if (maxAmount > 0)
                    {
                        allResources.Add(availableResource);
                        break;
                    }

                }
            }
        }

        public static bool vesselHasRessources(Vessel v, PartResourceDefinition resource, double amount)
        {
            v.GetConnectedResourceTotals(resource.id, false, out double vesselAmount, out double vesselMaxAmount);
            if (vesselAmount >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static double getVesselRessources(Vessel v, PartResourceDefinition resource)
        {
            v.GetConnectedResourceTotals(resource.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselAmount;
        }

        public bool facilityHasRessources(PartResourceDefinition resouce, double amount)
        {
            if (storageFacility.getRessources()[resouce] >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public double getFacilityResource(PartResourceDefinition resource)
        {
            return storageFacility.getRessources()[resource];
        }

        public double getFacilitySpace(PartResourceDefinition resource)
        {
            return storageFacility.getEmptyAmount(resource);
        }

        /// <summary>
        /// checks if the vessel v has enough space to add amount of r to it.
        /// </summary>
        public static bool vesselHasSpace(Vessel v, PartResourceDefinition r, double amount)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            if (vesselMaxAmount - vesselAmount >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static double getVesselSpace(Vessel v, PartResourceDefinition r)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselMaxAmount - vesselAmount;
        }

        public bool facilityHasSpace(PartResourceDefinition resource, double amount)
        {
            if (storageFacility.maxVolume - storageFacility.currentVolume >= KCStorageFacility.getVolumeForAmount(resource, amount))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool trashResources = false;

        protected override void CustomWindow()
        {
            storageFacility.Colony.UpdateColony();
            if (!storageFacility.enabled) GUI.enabled = false;
            else GUI.enabled = true;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MaxVolume: {storageFacility.storageInfo.maxVolume[storageFacility.level]:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"UsedVolume: {storageFacility.currentVolume:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            List<double> valueList = new List<double> { -10000, -1000, -100, -10, -1, 1, 10, 100, 1000, 10000 };


            KCStorageFacilityInfo info = storageFacility.storageInfo;

            CelestialBody body = FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == storageFacility.Colony.BodyID);

            double radius = facility.KKgroups.Average(g => KerbalKonstructs.API.GetGroupCenter(g, body.bodyName).RadiusOffset) + body.Radius;
            double squareRadius = radius * radius;
            double unMultiplier = body.gMagnitudeAtCenter / squareRadius;

            float multiplier = info.UseGravityMultiplier[facility.level] ? Math.Max(info.MinGravity[facility.level], Math.Min(info.MaxGravity[facility.level], (float)unMultiplier / 9.80665f)) : 1;
            if (info.UseGravityMultiplier[facility.level] && !Configuration.Paused) Configuration.writeDebug($"KCECStorageWindow: radius: {radius}, radius²: {squareRadius}, unMultiplier: {unMultiplier}");


            List<Type> types = info.RangeTypes[facility.level];
            List<string> names = info.RangeFacilities[facility.level];

            float range = info.TransferRange[facility.level] * multiplier * Configuration.FacilityRangeMultiplier;
            bool canTranfer = facility.Colony.Facilities.Where(f => types.Contains(f.GetType()) ^ names.Contains(f.facilityInfo.name)).Any(f => f.playerNearFacility((float)range)) || facility.playerNearFacility((float)range);
            canTranfer &= !storageFacility.enabled;
            canTranfer |= trashResources; // allow transfer if trashing resources, so that the user can delete resources from the storage facility
            canTranfer &= FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.LandedOrSplashed && FlightGlobals.ship_srfSpeed <= 0.5; // only allow transfer if the vessel is landed or splashed


            scrollPos = GUILayout.BeginScrollView(scrollPos);
            Dictionary<PartResourceDefinition, double> resourceCopy = storageFacility.getRessources();
            for (int r = 0; r < resourceCopy.Count; r++)
            {
                KeyValuePair<PartResourceDefinition, double> kvp = resourceCopy.ElementAt(r);
                GUILayout.BeginVertical();
                GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value:f2}", GUILayout.Height(18));

                GUI.enabled = canTranfer;
                GUILayout.BeginHorizontal();
                foreach (double i in valueList)
                {
                    if (i < 0 && trashResources || !trashResources)
                    {
                        if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                        {
                            Configuration.writeLog($"Transfering {i} {kvp.Key.displayName} from storage facility {storageFacility.DisplayName} to vessel {FlightGlobals.ActiveVessel.vesselName}.");

                            if (i < 0)
                            {
                                if (!trashResources)
                                {
                                    if (vesselHasSpace(FlightGlobals.ActiveVessel, kvp.Key, -i))
                                    {
                                        if (facilityHasRessources(kvp.Key, -i))
                                        {
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, i, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                            storageFacility.changeAmount(kvp.Key, i);
                                        }
                                        else
                                        {
                                            double amount = getFacilityResource(kvp.Key);
                                            storageFacility.changeAmount(kvp.Key, -amount);
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        }
                                    }
                                    else
                                    {
                                        double amount = getVesselSpace(FlightGlobals.ActiveVessel, kvp.Key);

                                        if (facilityHasRessources(kvp.Key, amount))
                                        {
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                            storageFacility.changeAmount(kvp.Key, -amount);
                                        }
                                        else
                                        {
                                            amount = getFacilityResource(kvp.Key);
                                            storageFacility.changeAmount(kvp.Key, -amount);
                                            FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        }
                                    }
                                }
                                else
                                {
                                    if (facilityHasRessources(kvp.Key, -i))
                                    {
                                        storageFacility.changeAmount(kvp.Key, i);
                                    }
                                    else
                                    {
                                        double amount = getFacilityResource(kvp.Key);
                                        storageFacility.changeAmount(kvp.Key, -amount);
                                    }
                                }
                            }
                            else
                            {
                                if (facilityHasSpace(kvp.Key, i))
                                {
                                    if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, i))
                                    {
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, i, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, i);
                                    }
                                    else
                                    {
                                        double amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, amount);
                                    }
                                }
                                else
                                {
                                    double amount = getFacilitySpace(kvp.Key);
                                    if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, amount))
                                    {
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, amount);
                                    }
                                    else
                                    {
                                        amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                        FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                        storageFacility.changeAmount(kvp.Key, amount);
                                    }
                                }
                            }
                        }
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(2);

            storageFacility.locked = GUILayout.Toggle(storageFacility.locked, "Lock storage", GUILayout.Height(18));

            if (facility.facilityInfo.ECperSecond[facility.level] > 0)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"EC Consumption Priority: {storageFacility.ECConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) storageFacility.ECConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) storageFacility.ECConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
            }

            trashResources = GUILayout.Toggle(trashResources, "Trash resources", GUILayout.Height(18));
            GUILayout.Label("Warning: enabling the trash resources option will delete the resource instead of transferring it to the vessel.");
        }

        protected override void OnOpen()
        {

            if (FlightGlobals.ActiveVessel == null) return;
            storageFacility.addVesselResourceTypes(FlightGlobals.ActiveVessel);
        }

        protected override void OnClose()
        {
            storageFacility.cleanUpResources();
        }

        public KCStorageFacilityWindow(KCStorageFacility storageFacility) : base(storageFacility, Configuration.createWindowID())
        {
            this.storageFacility = storageFacility;
            //GetVesselResources();
            //foreach (PartResourceDefinition resource in allResources)
            //{
            //    storageFacility.addRessource(resource);
            //}
            toolRect = new Rect(100, 100, 400, 600);
        }
    }

}
