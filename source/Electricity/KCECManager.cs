using KerbalColonies.colonyFacilities.CabFacility;
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

namespace KerbalColonies.Electricity
{
    public class KCColonyECData
    {
        public double lastTime { get; set; }
        public double deltaTime { get; set; }
        public double currentTime { get; set; }
        public double lastECProduced { get; set; }
        public double lastECConsumed { get; set; }
        public double lastECStored { get; set; }
        public double lastECDelta => lastECProduced - lastECConsumed;
    }

    public class KCECManager
    {
        public static Dictionary<colonyClass, KCColonyECData> colonyEC = new Dictionary<colonyClass, KCColonyECData>();

        public static void CABDisplay(colonyClass colony)
        {
            if (!colonyEC.ContainsKey(colony)) return;

            GUILayout.Space(10);
            GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth), GUILayout.Height(70));
            {
                KCColonyECData colonyData = colonyEC[colony];

                GUILayout.Label($"<b>Electricity:</b>");
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth / 2 - 10));
                    {
                        GUILayout.Label($"Produced: {(colonyData.lastECProduced / colonyData.deltaTime):F2} EC/s");
                        GUILayout.Label($"Consumed: {(colonyData.lastECConsumed / colonyData.deltaTime):F2} EC/s");
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width(KC_CAB_Window.CABInfoWidth / 2 - 10));
                    {
                        GUILayout.Label($"Stored: {(colonyData.lastECStored / colonyData.deltaTime):F2} EC");
                        GUILayout.Label($"Delta: {(colonyData.lastECDelta / colonyData.deltaTime):F2} EC/s");
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private static void getDeltaTime(colonyClass colony, out double lastTime, out double deltaTime, out double currentTime)
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

        public static void ElectricityUpdate(colonyClass colony)
        {
            KCColonyECData colonyData = new KCColonyECData();

            getDeltaTime(colony, out double lastTime, out double deltaTime, out double currentTime);
            if (deltaTime == 0) return;

            colonyData.lastTime = lastTime;
            colonyData.deltaTime = deltaTime;
            colonyData.currentTime = currentTime;

            double ECProduced = colony.Facilities.OfType<KCECProducer>().Sum(facility => facility.ECProduction(lastTime, deltaTime, currentTime));
            colonyData.lastECProduced = ECProduced;

            SortedDictionary<int, List<KCECConsumer>> ECConsumers = new SortedDictionary<int, List<KCECConsumer>>();
            colony.Facilities.OfType<KCECConsumer>().ToList().ForEach(facility =>
            {
                if (!ECConsumers.ContainsKey(facility.ECConsumptionPriority))
                    ECConsumers[facility.ECConsumptionPriority] = new List<KCECConsumer>();
                ECConsumers[facility.ECConsumptionPriority].Add(facility);
            });
            colonyData.lastECConsumed = ECConsumers.SelectMany(kvp => kvp.Value).ToList().Sum(facility => facility.ExpectedECConsumption(lastTime, deltaTime, currentTime));
            ECConsumers.Reverse();

            SortedDictionary<int, List<KCECStorage>> ECStored = new SortedDictionary<int, List<KCECStorage>>();
            colony.Facilities.OfType<KCECStorage>().ToList().ForEach(facility =>
            {
                if (!ECStored.ContainsKey(facility.ECStoragePriority))
                    ECStored[facility.ECStoragePriority] = new List<KCECStorage>();
                ECStored[facility.ECStoragePriority].Add(facility);
            });
            colonyData.lastECStored = ECStored.SelectMany(kvp => kvp.Value).ToList().Sum(facility => facility.StoredEC(lastTime, deltaTime, currentTime));
            ECStored.Reverse();

            Configuration.writeDebug($"KCECManager: colony: {colony.Name}, deltaTime: {deltaTime}, current time: {currentTime}, last ec produced: {ECProduced}, last ec consumed: {colonyData.lastECConsumed}, last ec stored: {colonyData.lastECStored}, ec delta: {colonyData.lastECDelta}, ecDelta/s: {colonyData.lastECDelta / colonyData.deltaTime}");

            if (colonyData.lastECDelta >= 0)
            {
                ECConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(facility => facility.ConsumeEC(lastTime, deltaTime, currentTime));
                double remainingEC = colonyData.lastECDelta;
                ECStored.SelectMany(kvp => kvp.Value).ToList().TakeWhile(facility =>
                {
                    remainingEC = facility.ChangeECStored(remainingEC);
                    return remainingEC > 0;
                }).ToList();
            }
            else if (colonyData.lastECDelta + colonyData.lastECStored > 0)
            {
                ECConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(facility => facility.ConsumeEC(lastTime, deltaTime, currentTime));
                double missingEC = colonyData.lastECDelta;
                ECStored.SelectMany(kvp => kvp.Value).ToList().TakeWhile(facility =>
                {
                    missingEC = facility.ChangeECStored(missingEC);
                    return missingEC < 0;
                }).ToList();
            }
            else
            {
                ECStored.SelectMany(kvp => kvp.Value).ToList().ForEach(facility => facility.SetStoredEC(0));
                double remainingEC = colonyData.lastECStored + colonyData.lastECProduced;
                ECConsumers.SelectMany(kvp => kvp.Value).ToList().ForEach(facility =>
                {
                    double consumed = facility.ExpectedECConsumption(lastTime, deltaTime, currentTime);
                    if (consumed <= remainingEC)
                    {
                        remainingEC -= consumed;
                        facility.ConsumeEC(lastTime, deltaTime, currentTime);
                    }
                    else if (remainingEC > 0)
                    {
                        facility.ÍnsufficientEC(lastTime, deltaTime, currentTime, remainingEC);
                        remainingEC = 0;
                    }
                    else
                    {
                        facility.ÍnsufficientEC(lastTime, deltaTime, currentTime, 0);
                    }
                });
            }

            colonyEC.Remove(colony);
            colonyEC.Add(colony, colonyData);
        }
    }
}
