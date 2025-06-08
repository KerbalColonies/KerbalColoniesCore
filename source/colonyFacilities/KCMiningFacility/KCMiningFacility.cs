using KerbalColonies.UI;
using KerbalKonstructs;
using KerbalKonstructs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacility : KCKerbalFacilityBase
    {
        protected KCMiningFacilityWindow miningFacilityWindow;

        public KCMiningFacilityInfo miningFacilityInfo { get { return (KCMiningFacilityInfo)facilityInfo; } }

        public Dictionary<PartResourceDefinition, double> storedResoures { get; protected set; } = new Dictionary<PartResourceDefinition, double> { };
        public Dictionary<string, Dictionary<PartResourceDefinition, double>> groupDensities { get; protected set; } = new Dictionary<string, Dictionary<PartResourceDefinition, double>> { };

        public override void WhileBuildingPlaced(GroupCenter kkGroupname)
        {
            KerbalKonstructs.Core.StaticInstance staticInstance = KerbalKonstructs.API.GetGroupStatics(kkGroupname.Group, kkGroupname.CelestialBody.name).First();
            staticInstance.Update();

            KCMiningFacilityPlacementWindow.Instance.newRates.Clear();
            AbundanceRequest request = new AbundanceRequest();
            request.BodyId = Colony.BodyID;
            request.ResourceType = HarvestTypes.Planetary;
            request.Longitude = staticInstance.RefLongitude;
            request.Latitude = staticInstance.RefLatitude;
            request.Altitude = kkGroupname.RadiusOffset;
            miningFacilityInfo.rates[level].ForEach(rate =>
            {
                if (rate.useFixedRate) KCMiningFacilityPlacementWindow.Instance.newRates.Add(rate.resource, rate.rate);
                else
                {
                    request.ResourceName = rate.resource.name;
                    KCMiningFacilityPlacementWindow.Instance.newRates.Add(rate.resource, rate.rate * ResourceMap.Instance.GetAbundance(request) * 2);
                }
            });

            if (!KCMiningFacilityPlacementWindow.Instance.IsOpen()) KCMiningFacilityPlacementWindow.Instance.Open();
        }

        public override void OnGroupPlaced(KerbalKonstructs.Core.GroupCenter kkgroup)
        {
            KCMiningFacilityPlacementWindow.Instance.Close();

            KerbalKonstructs.Core.StaticInstance staticInstance = KerbalKonstructs.API.GetGroupStatics(kkgroup.Group, kkgroup.CelestialBody.name).First();
            staticInstance.Update();

            AbundanceRequest request = new AbundanceRequest();
            request.BodyId = Colony.BodyID;
            request.ResourceType = HarvestTypes.Planetary;
            request.Longitude = staticInstance.RefLongitude;
            request.Latitude = staticInstance.RefLatitude;
            request.Altitude = kkgroup.RadiusOffset;
            groupDensities.Add(kkgroup.Group, new Dictionary<PartResourceDefinition, double> { });

            miningFacilityInfo.rates[level].ForEach(rate =>
            {
                if (rate.useFixedRate) groupDensities[kkgroup.Group].Add(rate.resource, rate.rate);
                else
                {
                    request.ResourceName = rate.resource.name;
                    groupDensities[kkgroup.Group].Add(rate.resource, rate.rate * ResourceMap.Instance.GetAbundance(request) * 2);
                }
            });
        }

        public override void Update()
        {
            /* Pending implementation, needs an window during the placement for the production rate
            KerbalKonstructs.Core.StaticInstance instance = API.GetGroupStatics(KKgroups[0], FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == Colony.BodyID).name).FirstOrDefault();

            AbundanceRequest request = new AbundanceRequest();
            request.BodyId = Colony.BodyID;
            request.ResourceType = HarvestTypes.Planetary;
            request.ResourceName = "Ore"; // Default resource, can be changed based on the facility's configuration
            request.Longitude = instance.RefLongitude;
            request.Latitude = instance.RefLatitude;
            request.Altitude = instance.RadiusOffset;
            Configuration.writeLog($"Requesting abundance for resource {request.ResourceName} at location ({request.Longitude}, {request.Latitude}) on body {Colony.BodyID}");
            Configuration.writeLog($"Abundance: {ResourceMap.Instance.GetAbundance(request)}");
            */

            double deltaTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            lastUpdateTime = Planetarium.GetUniversalTime();

            KCMiningFacilityInfo facilityInfo = miningFacilityInfo;

            groupDensities.ToList().ForEach(kvp => kvp.Value.ToList().ForEach(prdRate =>
            {
                if (storedResoures.ContainsKey(prdRate.Key)) storedResoures[prdRate.Key] += (prdRate.Value / 6 / 60 / 60) * deltaTime * kerbals.Count;
                else storedResoures.Add(prdRate.Key, ((prdRate.Value / 6 / 60 / 60) * deltaTime) * kerbals.Count);
            }));

            Dictionary<PartResourceDefinition, double> maxPerResource = new Dictionary<PartResourceDefinition, double> { };
            miningFacilityInfo.rates.Where(kvp => kvp.Key <= level).ToList().ForEach(kvp => kvp.Value.ForEach(rate =>
            {
                if (!maxPerResource.ContainsKey(rate.resource)) maxPerResource.Add(rate.resource, rate.max);
                else maxPerResource[rate.resource] += rate.max;
            }));

            storedResoures.ToList().ForEach(res =>
            {
                if (maxPerResource.ContainsKey(res.Key)) storedResoures[res.Key] = Math.Min(maxPerResource[res.Key], res.Value);
                else maxPerResource.Add(res.Key, res.Value);
            });

            //miningFacilityInfo.rates[level].ForEach(rate =>
            //{
            //    if (storedResoures.ContainsKey(rate.resource)) storedResoures[rate.resource] += ((rate.rate / 6 / 60 / 60) * deltaTime) * kerbals.Count;
            //    else storedResoures.Add(rate.resource, ((rate.rate / 6 / 60 / 60) * deltaTime) * kerbals.Count);

            //    storedResoures[rate.resource] = Math.Min(rate.max, storedResoures[rate.resource]);
            //});
        }

        public override void OnBuildingClicked()
        {
            miningFacilityWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            miningFacilityWindow.Toggle();
        }

        public override string GetFacilityProductionDisplay()
        {
            StringBuilder sb = new StringBuilder();
            miningFacilityInfo.rates[level].ForEach(rate =>
            {
                if (storedResoures.ContainsKey(rate.resource))
                    sb.AppendLine($"{(groupDensities.Sum(kvp => kvp.Value[rate.resource]) * kerbals.Count):f2} {rate.resource.displayName}/day\n{storedResoures[rate.resource]:f2}/{rate.max:f2} stored");
                else
                    sb.AppendLine($"{(groupDensities.Sum(kvp => kvp.Value[rate.resource]) * kerbals.Count):f2} {rate.resource.displayName}/day\n0/{rate.max:f2} stored");
            });
            return sb.ToString();
        }

        public bool RetriveResource(PartResourceDefinition resource)
        {
            if (storedResoures.ContainsKey(resource) && storedResoures[resource] > 0)
            {
                storedResoures[resource] = KCStorageFacility.addResourceToColony(resource, storedResoures[resource], Colony);
                return true;
            }
            return false;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            ConfigNode resourceNode = new ConfigNode("resourceNode");
            storedResoures.ToList().ForEach(res =>
            {
                ConfigNode resNode = new ConfigNode("resource");
                resNode.AddValue("name", res.Key.name);
                resNode.AddValue("amount", res.Value);
                resourceNode.AddNode(resNode);
            });

            ConfigNode rateNode = new ConfigNode("rates");
            groupDensities.ToList().ForEach(kvp =>
            {
                ConfigNode groupNode = new ConfigNode("group");
                groupNode.AddValue("groupName", kvp.Key);
                kvp.Value.ToList().ForEach(res =>
                {
                    ConfigNode resNode = new ConfigNode("resource");
                    resNode.AddValue("resource", res.Key.name);
                    resNode.AddValue("rate", res.Value);
                    groupNode.AddNode(resNode);
                });
                rateNode.AddNode(groupNode);
            });
            rateNode.AddNode(rateNode);

            node.AddNode(resourceNode);

            return node;
        }

        public KCMiningFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            if (node.HasNode("resourceNode"))
            {
                ConfigNode resourceNode = node.GetNode("resourceNode");
                foreach (ConfigNode resNode in resourceNode.GetNodes("resource"))
                {
                    PartResourceDefinition resDef = PartResourceLibrary.Instance.GetDefinition(resNode.GetValue("name"));
                    if (resDef == null) throw new NullReferenceException($"The resource {resNode.GetValue("name")} is not defined in the PartResourceLibrary. Please check your configuration for the facility {facilityInfo.name} (type: {facilityInfo.type}).");
                    double amount = double.Parse(resNode.GetValue("amount"));
                    storedResoures.Add(resDef, amount);
                }
            }

            miningFacilityInfo.rates.Where(kvp => kvp.Key <= level).ToList().ForEach(kvp => kvp.Value.ForEach(rate =>
            {
                storedResoures.TryAdd(rate.resource, 0);
            }));

            if (node.HasNode("rates"))
            {
                ConfigNode ratesNode = node.GetNode("rates");
                foreach (ConfigNode groupNode in ratesNode.GetNodes("group"))
                {
                    string groupName = groupNode.GetValue("groupName");
                    Dictionary<PartResourceDefinition, double> groupRates = new Dictionary<PartResourceDefinition, double> { };
                    foreach (ConfigNode resNode in groupNode.GetNodes("resource"))
                    {
                        PartResourceDefinition resDef = PartResourceLibrary.Instance.GetDefinition(resNode.GetValue("resource"));
                        if (resDef == null) throw new NullReferenceException($"The resource {resNode.GetValue("resource")} is not defined in the PartResourceLibrary. Please check your configuration for the facility {facilityInfo.name} (type: {facilityInfo.type}).");
                        double rate = double.Parse(resNode.GetValue("rate"));
                        groupRates.Add(resDef, rate);
                    }
                    groupDensities.Add(groupName, groupRates);
                }
            }

            int offset = 0;
            for (int i = 0; i <= level; i++)
            {
                if (miningFacilityInfo.UpgradeTypes[i] != UpgradeType.withAdditionalGroup && i != level) { offset++; continue; }

                if (!groupDensities.ContainsKey(KKgroups[i - offset]))
                {
                    KerbalKonstructs.Core.StaticInstance staticInstance = KerbalKonstructs.API.GetGroupStatics(KKgroups[i - offset], FlightGlobals.Bodies.First(b => FlightGlobals.GetBodyIndex(b) == Colony.BodyID).name).FirstOrDefault();
                    if (staticInstance != null)
                    {
                        groupDensities.Add(KKgroups[i - offset], new Dictionary<PartResourceDefinition, double> { });
                        miningFacilityInfo.rates[i].ForEach(rate =>
                        {
                            if (rate.useFixedRate) groupDensities[KKgroups[i]].Add(rate.resource, rate.rate);
                            else
                            {
                                AbundanceRequest request = new AbundanceRequest
                                {
                                    BodyId = Colony.BodyID,
                                    ResourceType = HarvestTypes.Planetary,
                                    Longitude = staticInstance.RefLongitude,
                                    Latitude = staticInstance.RefLatitude,
                                    Altitude = staticInstance.RadiusOffset,
                                    ResourceName = rate.resource.name
                                };
                                groupDensities[KKgroups[i]].Add(rate.resource, rate.rate * ResourceMap.Instance.GetAbundance(request) * 2);
                            }
                        });
                    }
                }
            }

            miningFacilityWindow = new KCMiningFacilityWindow(this);
        }

        public KCMiningFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            miningFacilityWindow = new KCMiningFacilityWindow(this);

            miningFacilityInfo.rates[0].ForEach(rate =>
            {
                storedResoures.TryAdd(rate.resource, 0);
            });
        }
    }
}
