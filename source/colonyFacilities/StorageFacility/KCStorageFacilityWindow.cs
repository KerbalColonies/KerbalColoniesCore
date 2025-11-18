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
        public enum ResourceTransferAvailable
        {
            Possible,
            Colony_only,
            Vessel_only,
        }

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
            return vesselAmount >= amount;
        }

        public static double getVesselRessources(Vessel v, PartResourceDefinition resource)
        {
            v.GetConnectedResourceTotals(resource.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselAmount;
        }

        public bool facilityHasRessources(PartResourceDefinition resouce, double amount) => storageFacility.unifiedColonyStorage.Resources[resouce] >= amount;

        public double getFacilityResource(PartResourceDefinition resource)
        {
            return storageFacility.unifiedColonyStorage.Resources[resource];
        }

        public double getFacilitySpace(PartResourceDefinition resource)
        {
            return storageFacility.unifiedColonyStorage.MaxStorable(resource);
        }

        /// <summary>
        /// checks if the vessel v has enough space to add amount of r to it.
        /// </summary>
        public static bool vesselHasSpace(Vessel v, PartResourceDefinition r, double amount)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselMaxAmount - vesselAmount >= amount;
        }

        public static double getVesselSpace(Vessel v, PartResourceDefinition r)
        {
            v.GetConnectedResourceTotals(r.id, false, out double vesselAmount, out double vesselMaxAmount);
            return vesselMaxAmount - vesselAmount;
        }

        SortedDictionary<PartResourceDefinition, ResourceTransferAvailable> AvailableResources;
        protected double transferAmount = 0;
        protected string transferAmountString = "0";
        protected bool trashResources = false;
        protected Vector2 resourceUsageScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            storageFacility.Colony.UpdateColony();
            GUILayout.Label($"MaxVolume (facility): {storageFacility.storageInfo.maxVolume[storageFacility.level]:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MaxVolume (colony): {storageFacility.unifiedColonyStorage.Volume:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"UsedVolume: {storageFacility.unifiedColonyStorage.UsedVolume:f2}", LabelGreen, GUILayout.Height(18));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            List<double> valueList = new List<double> { 0.01, 0.1, 1, 10, 100, 1000, 10000, 100000 };


            KCStorageFacilityInfo info = storageFacility.storageInfo;

            bool canTranfer = FlightGlobals.ActiveVessel != null ? storageFacility.unifiedColonyStorage.VesselInRange(FlightGlobals.ActiveVessel) : false;
            canTranfer |= trashResources;


            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount:");
            transferAmountString = GUILayout.TextField(transferAmountString, GUILayout.Width(100));
            if (GUILayout.Button("Set") && double.TryParse(transferAmountString, out double amountRes)) transferAmount = amountRes;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            foreach (double i in valueList)
            {
                if (GUILayout.Button(i.ToString(), GUILayout.Height(18), GUILayout.Width(32)))
                {
                    transferAmount = i;
                    transferAmountString = i.ToString();
                }
            }
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            AvailableResources.ToList().ForEach(kvp =>
            {
                GUILayout.BeginHorizontal();

                double resourceAmount = storageFacility.unifiedColonyStorage.Resources.GetValueOrDefault(kvp.Key);
                GUILayout.Label($"{kvp.Key.displayName}: {resourceAmount:f2}", GUILayout.Height(18));
                GUILayout.FlexibleSpace();

                if (kvp.Value == ResourceTransferAvailable.Possible)
                {
                    GUI.enabled = canTranfer;
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30)) | GUILayout.Button("-", GUILayout.Width(30)))
                    {
                        if (trashResources)
                        {
                            storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, transferAmount);
                        }
                        else
                        {
                            if (vesselHasSpace(FlightGlobals.ActiveVessel, kvp.Key, -transferAmount))
                            {
                                if (facilityHasRessources(kvp.Key, -transferAmount))
                                {
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, transferAmount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                    storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, transferAmount);
                                }
                                else
                                {
                                    double amount = getFacilityResource(kvp.Key);
                                    storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, -amount);
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                }
                            }
                            else
                            {
                                double amount = getVesselSpace(FlightGlobals.ActiveVessel, kvp.Key);

                                if (facilityHasRessources(kvp.Key, amount))
                                {
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                    storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, -amount);
                                }
                                else
                                {
                                    amount = getFacilityResource(kvp.Key);
                                    storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, -amount);
                                    FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, -amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                }
                            }
                        }
                    }
                    GUI.enabled = canTranfer && !trashResources;
                    if (GUILayout.Button("+", GUILayout.Width(30)) | GUILayout.RepeatButton("++", GUILayout.Width(30)))
                    {
                        if (storageFacility.unifiedColonyStorage.MaxStorable(kvp.Key) >= transferAmount)
                        {
                            if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, transferAmount))
                            {
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, transferAmount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, transferAmount);
                            }
                            else
                            {
                                double amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, amount);
                            }
                        }
                        else
                        {
                            double amount = getFacilitySpace(kvp.Key);
                            if (vesselHasRessources(FlightGlobals.ActiveVessel, kvp.Key, amount))
                            {
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, amount);
                            }
                            else
                            {
                                amount = getVesselRessources(FlightGlobals.ActiveVessel, kvp.Key);
                                FlightGlobals.ActiveVessel.rootPart.RequestResource(kvp.Key.id, amount, ResourceFlowMode.ALL_VESSEL_BALANCE, false);
                                storageFacility.unifiedColonyStorage.ChangeResourceStored(kvp.Key, amount);
                            }
                        }
                    }
                    GUI.enabled = true;
                }
                else if (kvp.Value == ResourceTransferAvailable.Colony_only)
                {
                    GUILayout.Label("No space on vessel", GUILayout.Height(18));
                }
                else if (kvp.Value == ResourceTransferAvailable.Vessel_only)
                {
                    GUILayout.Label("No space in colony", GUILayout.Height(18));
                }

                GUILayout.EndHorizontal();
            });
            GUILayout.EndScrollView();

            GUILayout.Space(2);

            storageFacility.locked = GUILayout.Toggle(storageFacility.locked, "Lock storage", GUILayout.Height(18));

            GUILayout.Label("Resource consumption per second:");
            resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(100));
            {
                storageFacility.facilityInfo.ResourceUsage[storageFacility.level].Where(kvp => kvp.Value < 0).ToList().ForEach(kvp =>
                {
                    GUILayout.Label($"{kvp.Key.displayName}: {kvp.Value}");
                });
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"Resource Consumption Priority: {storageFacility.ResourceConsumptionPriority}", GUILayout.Height(18));
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) storageFacility.ResourceConsumptionPriority--;
                if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) storageFacility.ResourceConsumptionPriority++;
            }
            GUILayout.EndHorizontal();

            trashResources = GUILayout.Toggle(trashResources, "Trash resources", GUILayout.Height(18));
            GUILayout.Label("Warning: enabling the trash resources option will delete the resource instead of transferring it to the vessel.");
        }

        protected override void OnOpen()
        {
            if (FlightGlobals.ActiveVessel == null) return;

            AvailableResources = new SortedDictionary<PartResourceDefinition, ResourceTransferAvailable>(Comparer<PartResourceDefinition>.Create((x, y) => x.displayName.CompareTo(y.displayName)));

            foreach (PartResourceDefinition resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                if (KCStorageFacility.blackListedResources.Contains(resource.name)) continue;

                FlightGlobals.ActiveVessel.GetConnectedResourceTotals(resource.id, true, out double amount, out double max, true);
                if (max > 0)
                {
                    if (storageFacility.unifiedColonyStorage.ResourceVolume(resource) <= 0 && storageFacility.unifiedColonyStorage.Resources.GetValueOrDefault(resource) <= 0)
                    {
                        AvailableResources.Add(resource, ResourceTransferAvailable.Vessel_only);
                    }
                    else
                    {
                        AvailableResources.Add(resource, ResourceTransferAvailable.Possible);
                    }
                }
                else if (storageFacility.unifiedColonyStorage.Resources.GetValueOrDefault(resource) > 0)
                {
                    AvailableResources.Add(resource, ResourceTransferAvailable.Colony_only);
                }
            }
        }

        //protected override void OnClose()
        //{
        //}

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
