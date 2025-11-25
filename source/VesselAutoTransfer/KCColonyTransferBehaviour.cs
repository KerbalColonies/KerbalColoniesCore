using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.ResourceManagment;
using KerbalColonies.Settings;
using Smooth.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public class KCColonyTransferBehaviour
    {
        /// <summary>
        /// Key: PartModuleID
        /// Value: Transfer Info
        /// </summary>
        public static Dictionary<uint, KCTransferInfo> ActiveTransfers { get; protected set; } = [];

        public class KCColonyResourceTransferHandler : IKCResourceProducer, IKCResourceConsumer
        {
            public uint partModuleID { get; private set; }

            public Dictionary<PartResourceDefinition, double> ResourceRates { get; set; } = [];
            public List<PartResourceDefinition> LimitedResources { get; set; } = [];

            public int ResourceConsumptionPriority { get; set; } = 0;

            public Dictionary<PartResourceDefinition, double> ResourceProduction(double lastTime, double deltaTime, double currentTime) => ResourceRates.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value * deltaTime);

            public Dictionary<PartResourceDefinition, double> ResourcesPerSecond() => ResourceRates.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            public void ConsumeResources(double lastTime, double deltaTime, double currentTime)
            {
                LimitedResources.Clear();
            }

            public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => ResourceRates.Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => -kvp.Value * deltaTime);

            public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
            {
                LimitedResources.Clear();
                LimitedResources.AddAll(limitingResources.Keys);
                limitingResources.Clear();
                return new Dictionary<PartResourceDefinition, double>(sufficientResources);
            }

            public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond() => ResourceRates.Where(kvp => kvp.Value < 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            public KCColonyResourceTransferHandler(colonyClass colony, uint partModuleID)
            {
                Configuration.writeDebug($"Creating EC transfer handler with partModuleID {partModuleID}");

                if (!KCResourceManager.otherProducers.ContainsKey(colony)) KCResourceManager.otherProducers.Add(colony, []);
                if (!KCResourceManager.otherConsumers.ContainsKey(colony)) KCResourceManager.otherConsumers.Add(colony, []);
                if (!KCResourceManager.otherConsumers[colony].ContainsKey(0)) KCResourceManager.otherConsumers[colony].Add(0, []);

                Configuration.writeDebug($"Existing EC transfer handlers count: {KCResourceManager.otherProducers[colony].OfType<KCColonyResourceTransferHandler>().Count()}");
                //foreach (KCColonyTransferECHandler item in KCECManager.otherProducers[colony].ToList().OfType<KCColonyTransferECHandler>())
                //{
                //    Configuration.writeDebug($"Found EC transfer handler with partModuleID {item.partModuleID}");
                //    if (item.partModuleID == partModuleID)
                //    {
                //        Configuration.writeDebug($"EC transfer handler for partModuleID {partModuleID} already exists in otherProducers, removing it first");
                //        KCECManager.otherProducers[colony].Remove(item);
                //    }
                //}
                //Configuration.writeDebug($"New EC transfer handlers count: {KCECManager.otherProducers[colony].OfType<KCColonyTransferECHandler>().Count()}");


                KCResourceManager.otherProducers[colony].OfType<KCColonyResourceTransferHandler>().Where(prod => prod.partModuleID == partModuleID).ToList().ForEach(prod => KCResourceManager.otherProducers[colony].Remove(prod));
                KCResourceManager.otherConsumers[colony][0].OfType<KCColonyResourceTransferHandler>().Where(prod => prod.partModuleID == partModuleID).ToList().ForEach(prod => KCResourceManager.otherConsumers[colony][0].Remove(prod));

                this.partModuleID = partModuleID;

                KCResourceManager.otherProducers[colony].Add(this);
                KCResourceManager.otherConsumers[colony][0].Add(this);
            }
        }

        public static void ColonyLoadAction(colonyClass colony)
        {
            ActiveTransfers.ToList().ForEach(kvp =>
            {
                if (KCResourceManager.otherProducers.ContainsKey(colony))
                {
                    KCResourceManager.otherProducers[colony].OfType<KCColonyResourceTransferHandler>().Where(prod => prod.partModuleID == kvp.Key).ToList().ForEach(prod => KCResourceManager.otherProducers[colony].Remove(prod));
                }
                if (KCResourceManager.otherConsumers.ContainsKey(colony))
                {
                    KCResourceManager.otherConsumers[colony].ToList().ForEach(kvp2 => KCResourceManager.otherConsumers[colony][kvp2.Key].OfType<KCColonyResourceTransferHandler>().Where(prod => prod.partModuleID == kvp.Key).ToList().ForEach(prod => KCResourceManager.otherConsumers[colony][kvp2.Key].Remove(prod)));
                }

                if (kvp.Value.Colony.uniqueID == colony.uniqueID)
                {
                    kvp.Value.Delete();
                }
            });
        }


        public static void ColonyUpdateTransferAction(colonyClass colony)
        {
            ActiveTransfers.Values.Where(t => t.Colony == colony).ToList().ForEach(t =>
            {
                t.resources.ForEach(res =>
                {
                    double maxColonyRes = KCUnifiedColonyStorage.colonyStorages[colony].ResourceVolume(res) / res.volume;
                    double currentColonyRes = KCUnifiedColonyStorage.colonyStorages[colony].Resources.GetValueOrDefault(res);
                    double colonyResRatio = currentColonyRes / maxColonyRes;

                    KCColonyResourceData colonyresData = KCResourceManager.colonyResources[colony];
                    double resourceDelta = colonyresData.ResourceDelta(res) / KCResourceManager.colonyResources[colony].deltaTime;

                    double transferAmount = 0;

                    if (t.ResourcesTarget[res] > 0)
                    {
                        if (colonyResRatio < t.ColonyTransferLimits[res])
                        {
                            transferAmount = t.ResourcesTarget[res] * t.Efficiency;

                            double newRatio = (currentColonyRes + transferAmount + resourceDelta) / maxColonyRes;

                            if (newRatio > t.ColonyTransferLimits[res])
                            {
                                double limitPercent = Math.Abs(t.ColonyTransferLimits[res] - colonyResRatio);
                                transferAmount = Math.Min(Math.Max((t.ColonyTransferLimits[res] - colonyResRatio) * maxColonyRes, 0) + (resourceDelta < 0 && limitPercent > 0.99 ? resourceDelta : 0), t.ResourcesTarget[res]);
                            }
                        }

                        if (t.VesselConstrained[res])
                        {
                            if (t.ResourcesActual[res] > transferAmount)
                            {
                                t.VesselConstrained[res] = false;
                                t.ColonyConstrained[res] = true;

                                if (t.DisableIfColonyConstrains[res])
                                {
                                    transferAmount = 0;
                                    t.CleanResources();
                                }

                                t.ResourcesActual[res] = transferAmount;
                                t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                            }
                            else
                            {
                                t.ResourceTransferHandler.ResourceRates[res] = t.ResourcesActual[res];
                            }
                        }
                        else if (transferAmount != t.ResourcesActual[res])
                        {
                            if (transferAmount != t.ResourcesTarget[res])
                            {
                                t.VesselConstrained[res] = false;
                                t.ColonyConstrained[res] = true;

                                if (t.DisableIfColonyConstrains[res])
                                {
                                    transferAmount = 0;
                                    t.CleanResources();
                                }
                            }
                            else
                            {
                                t.ColonyConstrained[res] = false;
                            }

                            t.ResourcesActual[res] = transferAmount;
                            t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                        }
                        else
                        {
                            t.ResourcesActual[res] = transferAmount;
                            t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                        }
                    }
                    else if (t.ResourcesTarget[res] < 0)
                    {
                        if (t.ResourceTransferHandler.LimitedResources.Contains(res) || currentColonyRes < 0.5)
                        {
                            transferAmount = 0;
                        }
                        else if (colonyResRatio > t.ColonyTransferLimits[res])
                        {
                            transferAmount = t.ResourcesTarget[res] * t.Efficiency;

                            double newRatio = (currentColonyRes + transferAmount + resourceDelta) / maxColonyRes;

                            if (newRatio < t.ColonyTransferLimits[res])
                            {
                                double limitPercent = Math.Abs(t.ColonyTransferLimits[res] - colonyResRatio);
                                transferAmount = Math.Max(Math.Min((t.ColonyTransferLimits[res] - colonyResRatio) * maxColonyRes, 0) + (resourceDelta > 0 && limitPercent < 0.01 ? resourceDelta : 0), t.ResourcesTarget[res]);
                            }
                        }

                        if (-transferAmount > colonyresData.ResourcesStored.GetValueOrDefault(res) / colonyresData.deltaTime) transferAmount = 0;

                        if (t.VesselConstrained[res])
                        {
                            if (t.ResourcesActual[res] < transferAmount)
                            {
                                t.VesselConstrained[res] = false;
                                t.ColonyConstrained[res] = true;

                                if (t.DisableIfColonyConstrains[res])
                                {
                                    transferAmount = 0;
                                    t.CleanResources();
                                }

                                t.ResourcesActual[res] = transferAmount;
                                t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                            }
                            else
                            {
                                t.ResourceTransferHandler.ResourceRates[res] = t.ResourcesActual[res];
                            }
                        }
                        else if (transferAmount != t.ResourcesActual[res])
                        {
                            if (transferAmount != t.ResourcesTarget[res])
                            {
                                t.VesselConstrained[res] = false;
                                t.ColonyConstrained[res] = true;

                                if (t.DisableIfColonyConstrains[res])
                                {
                                    transferAmount = 0;
                                    t.CleanResources();
                                }
                            }
                            else
                            {
                                t.ColonyConstrained[res] = false;
                            }

                            t.ResourcesActual[res] = transferAmount;
                            t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                        }
                        else if (transferAmount == t.ResourcesTarget[res])
                        {
                            t.ColonyConstrained[res] = false;

                            t.ResourcesActual[res] = transferAmount;
                            t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                        }
                        else
                        {
                            t.ResourcesActual[res] = transferAmount;
                            t.ResourceTransferHandler.ResourceRates[res] = transferAmount;
                        }
                    }
                    else
                    {
                        t.ResourcesActual[res] = 0;
                        t.ResourceTransferHandler.ResourceRates[res] = 0;
                    }
                });
            });
        }
    }
}
