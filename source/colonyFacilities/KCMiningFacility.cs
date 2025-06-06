using KerbalColonies.UI;
using KerbalKonstructs;
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

namespace KerbalColonies.colonyFacilities
{
    public class KCMiningFacilityRate
    {
        public PartResourceDefinition resource { get; private set; }
        public double rate { get; private set; }
        public double max { get; private set; }

        public KCMiningFacilityRate(PartResourceDefinition resource, double rate, double max)
        {
            this.resource = resource;
            this.rate = rate;
            this.max = max;
        }
    }

    public class KCMiningFacilityInfo : KCKerbalFacilityInfoClass
    {
        public SortedDictionary<int, List<KCMiningFacilityRate>> rates { get; protected set; } = new SortedDictionary<int, List<KCMiningFacilityRate>> { };

        public KCMiningFacilityInfo(ConfigNode node) : base(node)
        {
            levelNodes.ToList().ForEach(n =>
            {
                if (n.Value.HasNode("resourceProduction"))
                {
                    rates[n.Key] = new List<KCMiningFacilityRate> { };
                    foreach (ConfigNode.Value value in n.Value.GetNode("resourceProduction").values)
                    {
                        PartResourceDefinition res = PartResourceLibrary.Instance.GetDefinition(value.name);
                        if (res == null) throw new NullReferenceException($"The resource {value.name} is not defined in the PartResourceLibrary. Please check your configuration for the facility {name} (type: {type}).");
                        string[] strings = value.value.Split(',');
                        if (strings.Length != 2) throw new FormatException($"The resourceProduction value for {value.name} in the facility {name} (type: {type}) is not in the correct format. It should be 'rate,max'.");
                        if (!double.TryParse(strings[0], out double rate) || !double.TryParse(strings[1], out double max)) throw new FormatException($"The resourceProduction value for {value.name} in the facility {name} (type: {type}) is not in the correct format. It should be 'rate,max'.");

                        rates[n.Key].Add(new KCMiningFacilityRate(res, rate, max));
                    }
                }
                else if (n.Key > 0) rates[n.Key] = rates[n.Key - 1];
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no resourceProduction (at least for level 0).");
            });
        }
    }

    public class KCMiningFacilityWindow : KCFacilityWindowBase
    {
        KCMiningFacility miningFacility;
        public KerbalGUI kerbalGUI;

        private Vector2 resourceScrollPos = new Vector2();
        protected override void CustomWindow()
        {
            miningFacility.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(miningFacility, true);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(toolRect.width / 2 - 10));
                kerbalGUI.StaffingInterface();
                GUILayout.EndVertical();

                resourceScrollPos = GUILayout.BeginScrollView(resourceScrollPos, GUILayout.Width(toolRect.width / 2 - 10));
                {
                    KCMiningFacilityInfo miningInfo = miningFacility.miningFacilityInfo;

                    miningFacility.storedResoures.ToList().ForEach(res =>
                    {
                        GUILayout.Label($"<size=20><b>{res.Key.displayName}</b></size>");
                        GUILayout.Label($"Stored: {res.Value:f2}");
                        GUILayout.Label($"Max: {miningInfo.rates[miningFacility.level].FirstOrDefault(r => r.resource.name == res.Key.name)?.max:f2}");
                        if (GUILayout.Button($"Retrieve {res.Key.displayName}")) miningFacility.RetriveResource(res.Key);

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

    public class KCMiningFacility : KCKerbalFacilityBase
    {
        protected KCMiningFacilityWindow miningFacilityWindow;

        public KCMiningFacilityInfo miningFacilityInfo { get { return (KCMiningFacilityInfo)facilityInfo; } }

        public Dictionary<PartResourceDefinition, double> storedResoures { get; protected set; } = new Dictionary<PartResourceDefinition, double> { };

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

            miningFacilityInfo.rates[level].ForEach(rate =>
            {
                if (storedResoures.ContainsKey(rate.resource)) storedResoures[rate.resource] += ((rate.rate / 6 / 60 / 60) * deltaTime) * kerbals.Count;
                else storedResoures.Add(rate.resource, ((rate.rate / 6 / 60 / 60) * deltaTime) * kerbals.Count);

                storedResoures[rate.resource] = Math.Min(rate.max, storedResoures[rate.resource]);
            });
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
                    sb.AppendLine($"{rate.rate * kerbals.Count} {rate.resource.displayName}/day\n{storedResoures[rate.resource]:f2}/{rate.max:f2} stored");
                else
                    sb.AppendLine($"{rate.rate * kerbals.Count} {rate.resource.displayName}/day\n0/{rate.max:f2} stored");
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

            miningFacilityInfo.rates[0].ForEach(rate =>
            {
                storedResoures.TryAdd(rate.resource, 0);
            });

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
