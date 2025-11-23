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

namespace KerbalColonies.ResourceManagment
{
    public class KCColonyResourceData
    {
        public double lastTime { get; set; }
        public double deltaTime { get; set; }
        public double currentTime { get; set; }

        public HashSet<PartResourceDefinition> resources { get; set; } = new HashSet<PartResourceDefinition>();
        public Dictionary<PartResourceDefinition, double> ResourcesProduced { get; set; } = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> ResourcesConsumed { get; set; } = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> ResourcesStored { get; set; } = new Dictionary<PartResourceDefinition, double>();

        public double ResourceDelta(PartResourceDefinition resource)
        {
            double produced = ResourcesProduced.GetValueOrDefault(resource);
            double consumed = ResourcesConsumed.GetValueOrDefault(resource);
            return (produced - consumed);
        }
    }

    public static class KCResourceManager
    {
        public static Dictionary<colonyClass, List<IKCResourceProducer>> otherProducers { get; set; } = new Dictionary<colonyClass, List<IKCResourceProducer>>();
        public static Dictionary<colonyClass, SortedDictionary<int, List<IKCResourceConsumer>>> otherConsumers { get; set; } = new Dictionary<colonyClass, SortedDictionary<int, List<IKCResourceConsumer>>>();
        public static Dictionary<colonyClass, SortedDictionary<int, List<IKCResourceStorage>>> otherStorages { get; set; } = new Dictionary<colonyClass, SortedDictionary<int, List<IKCResourceStorage>>>();

        public static Dictionary<colonyClass, KCColonyResourceData> colonyResources = new Dictionary<colonyClass, KCColonyResourceData>();

        private static void GetDeltaTime(colonyClass colony, out double lastTime, out double deltaTime, out double currentTime)
        {
            currentTime = Planetarium.GetUniversalTime();
            ConfigNode timeNode = colony.sharedColonyNodes.FirstOrDefault(node => node.name == "KCELTime");
            if (timeNode == null)
            {
                ConfigNode node = new ConfigNode("KCELTime");
                node.AddValue("lastTime", Planetarium.GetUniversalTime().ToString());
                colony.sharedColonyNodes.Add(node);
                lastTime = currentTime;
                deltaTime = 0;
                return;
            }
            lastTime = double.Parse(timeNode.GetValue("lastTime"));
            deltaTime = currentTime - lastTime;
            timeNode.SetValue("lastTime", currentTime);
            return;
        }

        public static void ResourceUpdate(colonyClass colony)
        {
            KCColonyResourceData colonyData = new KCColonyResourceData();

            GetDeltaTime(colony, out double lastTime, out double deltaTime, out double currentTime);
            if (deltaTime == 0) return;

            colonyData.lastTime = lastTime;
            colonyData.deltaTime = deltaTime;
            colonyData.currentTime = currentTime;

            colony.Facilities.OfType<IKCResourceProducer>().ToList().ForEach(facility =>
            {
                Dictionary<PartResourceDefinition, double> produced = facility.ResourceProduction(lastTime, deltaTime, currentTime);
                foreach (KeyValuePair<PartResourceDefinition, double> kvp in produced)
                {
                    colonyData.resources.Add(kvp.Key);
                    colonyData.ResourcesProduced[kvp.Key] = colonyData.ResourcesProduced.GetValueOrDefault(kvp.Key) + kvp.Value;
                }
            });
            if (otherProducers.ContainsKey(colony))
            {
                otherProducers[colony].ToList().ForEach(producer =>
                {
                    Dictionary<PartResourceDefinition, double> produced = producer.ResourceProduction(lastTime, deltaTime, currentTime);
                    foreach (KeyValuePair<PartResourceDefinition, double> kvp in produced)
                    {
                        colonyData.resources.Add(kvp.Key);
                        colonyData.ResourcesProduced[kvp.Key] = colonyData.ResourcesProduced.GetValueOrDefault(kvp.Key) + kvp.Value;
                    }
                });
            }

            SortedDictionary<int, List<IKCResourceConsumer>> ResourceConsumers = new SortedDictionary<int, List<IKCResourceConsumer>>();
            Dictionary<IKCResourceConsumer, Dictionary<PartResourceDefinition, double>> ResourcesConsumedPerConsumer = new Dictionary<IKCResourceConsumer, Dictionary<PartResourceDefinition, double>>();
            colony.Facilities.OfType<IKCResourceConsumer>().ToList().ForEach(facility =>
            {
                if (!ResourceConsumers.ContainsKey(facility.ResourceConsumptionPriority))
                    ResourceConsumers[facility.ResourceConsumptionPriority] = new List<IKCResourceConsumer>();
                ResourceConsumers[facility.ResourceConsumptionPriority].Add(facility);

                Dictionary<PartResourceDefinition, double> consumed = facility.ExpectedResourceConsumption(lastTime, deltaTime, currentTime);
                ResourcesConsumedPerConsumer.Add(facility, consumed);
                foreach (KeyValuePair<PartResourceDefinition, double> kvp in consumed)
                {
                    colonyData.resources.Add(kvp.Key);
                    colonyData.ResourcesConsumed[kvp.Key] = colonyData.ResourcesConsumed.GetValueOrDefault(kvp.Key) + kvp.Value;
                }
            });
            if (otherConsumers.ContainsKey(colony))
            {
                otherConsumers[colony].ToList().ForEach(kvp =>
                {
                    ResourceConsumers.TryAdd(kvp.Key, new List<IKCResourceConsumer>());
                    kvp.Value.ForEach(consumer =>
                    {
                        ResourceConsumers[kvp.Key].Add(consumer);
                        Dictionary<PartResourceDefinition, double> consumed = consumer.ExpectedResourceConsumption(lastTime, deltaTime, currentTime);
                        ResourcesConsumedPerConsumer.Add(consumer, consumed);
                        foreach (KeyValuePair<PartResourceDefinition, double> kvp2 in consumed)
                        {
                            colonyData.resources.Add(kvp2.Key);
                            colonyData.ResourcesConsumed[kvp2.Key] = colonyData.ResourcesConsumed.GetValueOrDefault(kvp2.Key) + kvp2.Value;
                        }
                    });
                });
            }
            ResourceConsumers.Reverse();

            SortedDictionary<int, List<IKCResourceStorage>> ResourceStored = new SortedDictionary<int, List<IKCResourceStorage>>();
            colony.Facilities.OfType<IKCResourceStorage>().ToList().ForEach(facility =>
            {
                ResourceStored.TryAdd(facility.Priority, new List<IKCResourceStorage>());

                ResourceStored[facility.Priority].Add(facility);
                SortedDictionary<PartResourceDefinition, double> stored = facility.StoredResources(lastTime, deltaTime, currentTime);
                foreach (KeyValuePair<PartResourceDefinition, double> kvp in stored)
                {
                    colonyData.resources.Add(kvp.Key);
                    colonyData.ResourcesStored[kvp.Key] = colonyData.ResourcesStored.GetValueOrDefault(kvp.Key) + kvp.Value;
                }
            });
            if (otherStorages.ContainsKey(colony))
            {
                otherStorages[colony].ToList().ForEach(kvp =>
                {
                    ResourceStored.TryAdd(kvp.Key, new List<IKCResourceStorage>());
                    kvp.Value.ForEach(storage =>
                    {
                        ResourceStored[kvp.Key].Add(storage);
                        SortedDictionary<PartResourceDefinition, double> stored = storage.StoredResources(lastTime, deltaTime, currentTime);
                        foreach (KeyValuePair<PartResourceDefinition, double> kvp2 in stored)
                        {
                            colonyData.resources.Add(kvp2.Key);
                            colonyData.ResourcesStored[kvp2.Key] = colonyData.ResourcesStored.GetValueOrDefault(kvp2.Key) + kvp2.Value;
                        }
                    });
                });
            }
            ResourceStored.Reverse();

            Dictionary<PartResourceDefinition, double> insufficientResources = new Dictionary<PartResourceDefinition, double>();
            Dictionary<PartResourceDefinition, double> sufficientResources = new Dictionary<PartResourceDefinition, double>();
            Dictionary<PartResourceDefinition, double> storedResourcesUsed = new Dictionary<PartResourceDefinition, double>();

            foreach (PartResourceDefinition res in colonyData.resources)
            {
                double delta = colonyData.ResourceDelta(res);


                if (delta >= 0)
                {
                    sufficientResources[res] = colonyData.ResourcesConsumed.GetValueOrDefault(res);
                    storedResourcesUsed[res] = delta;
                }
                else if (delta + colonyData.ResourcesStored.GetValueOrDefault(res) >= 0)
                {
                    sufficientResources[res] = colonyData.ResourcesConsumed.GetValueOrDefault(res);
                    storedResourcesUsed[res] = delta;
                }
                else
                {
                    insufficientResources[res] = colonyData.ResourcesProduced.GetValueOrDefault(res) + colonyData.ResourcesStored.GetValueOrDefault(res);
                    storedResourcesUsed[res] = -colonyData.ResourcesStored.GetValueOrDefault(res);
                }
            }

            if (insufficientResources.Count == 0)
            {
                ResourceConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(f => f.ConsumeResources(lastTime, deltaTime, currentTime));
                sufficientResources.Clear();
            }
            else
            {
                foreach (KeyValuePair<int, List<IKCResourceConsumer>> kvp in ResourceConsumers)
                {
                    foreach (IKCResourceConsumer item in kvp.Value)
                    {
                        Dictionary<PartResourceDefinition, double> limitingItemResources = new Dictionary<PartResourceDefinition, double>();
                        Dictionary<PartResourceDefinition, double> sufficientItemResources = new Dictionary<PartResourceDefinition, double>();

                        foreach (KeyValuePair<PartResourceDefinition, double> resKvp in ResourcesConsumedPerConsumer[item])
                        {
                            if (insufficientResources.ContainsKey(resKvp.Key))
                            {
                                limitingItemResources[resKvp.Key] = Math.Min(resKvp.Value, insufficientResources[resKvp.Key]);
                                insufficientResources[resKvp.Key] -= limitingItemResources[resKvp.Key];
                            }
                            else if (sufficientResources.ContainsKey(resKvp.Key))
                            {
                                sufficientItemResources[resKvp.Key] = resKvp.Value;
                                sufficientResources[resKvp.Key] -= resKvp.Value;
                            }
                        }

                        if (limitingItemResources.Count == 0)
                        {
                            item.ConsumeResources(lastTime, deltaTime, currentTime);
                        }
                        else
                        {
                            Dictionary<PartResourceDefinition, double> unusedResources = new(item.InsufficientResources(lastTime, deltaTime, currentTime, sufficientItemResources, limitingItemResources));

                            foreach (KeyValuePair<PartResourceDefinition, double> unusedKvp in unusedResources)
                            {
                                if (insufficientResources.ContainsKey(unusedKvp.Key))
                                {
                                    insufficientResources[unusedKvp.Key] += unusedKvp.Value;
                                }
                                else
                                {
                                    sufficientResources[unusedKvp.Key] += unusedKvp.Value;
                                }
                            }
                        }
                    }
                }
            }

            foreach (PartResourceDefinition res in colonyData.resources)
            {
                double amount = storedResourcesUsed.GetValueOrDefault(res);
                amount += insufficientResources.GetValueOrDefault(res);
                amount += sufficientResources.GetValueOrDefault(res);

                foreach (KeyValuePair<int, List<IKCResourceStorage>> storageKVP in ResourceStored)
                {
                    foreach (IKCResourceStorage storage in storageKVP.Value)
                    {
                        amount = storage.ChangeResourceStored(res, amount);

                        if (amount == 0) break;
                    }
                }
            }

            colonyResources.Remove(colony);
            colonyResources.Add(colony, colonyData);
        }
    }
}
