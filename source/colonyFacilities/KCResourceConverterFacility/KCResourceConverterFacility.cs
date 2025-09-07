using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.Electricity;
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

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class KCResourceConverterFacility : KCKerbalFacilityBase, KCECConsumer
    {
        public static void LoadResourceConversionLists()
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("KCResourceConversionRate");
            if (nodes != null && nodes.Length > 0)
            {
                foreach (ConfigNode node in nodes)
                {
                    string conversionName = node.GetValue("name");
                    if (string.IsNullOrEmpty(conversionName))
                    {
                        ConfigFacilityLoader.exceptions.Add(new MissingFieldException("Resource conversion rate without a name."));
                        ConfigFacilityLoader.failedConfigs.Add("ResourceConversionRate");
                        continue;
                    }
                    Configuration.writeDebug($"Loading conversion rate {conversionName}");
                    string displayName = node.GetValue("displayName");
                    Dictionary<PartResourceDefinition, double> InputResources = new Dictionary<PartResourceDefinition, double>();
                    Dictionary<PartResourceDefinition, double> OutputResources = new Dictionary<PartResourceDefinition, double>();

                    ConfigNode inputNode = node.GetNode("inputResources");
                    foreach (ConfigNode.Value v in inputNode.values)
                    {
                        Configuration.writeDebug($"Loading input resource {v.name} with amount {v.value}");
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        if (resourceDef == null)
                        {
                            Configuration.writeLog($"The PartResourceLibrary contains no definition for {v.name}.");

                            ConfigFacilityLoader.exceptions.Add(new MissingFieldException($"{conversionName} contains an invalid input resource name {v.name}."));
                            ConfigFacilityLoader.failedConfigs.Add($"ResourceConversionRate: {conversionName}");
                            continue;
                        }
                        double amount = double.Parse(v.value);
                        InputResources.Add(resourceDef, amount);
                    }
                    ConfigNode outputNode = node.GetNode("outputResources");
                    foreach (ConfigNode.Value v in outputNode.values)
                    {
                        Configuration.writeDebug($"Loading output resource {v.name} with amount {v.value}");
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        if (resourceDef == null)
                        {
                            Configuration.writeLog($"The PartResourceLibrary contains no definition for {v.name}.");

                            ConfigFacilityLoader.exceptions.Add(new MissingFieldException($"{conversionName} contains an invalid output resource name {v.name}."));
                            ConfigFacilityLoader.failedConfigs.Add($"ResourceConversionRate: {conversionName}");
                            continue;
                        }
                        double amount = double.Parse(v.value);
                        OutputResources.Add(resourceDef, amount);
                    }
                    try
                    {
                        new ResourceConversionRate(conversionName, displayName, InputResources, OutputResources);
                    }
                    catch (Exception e)
                    {
                        ConfigFacilityLoader.exceptions.Add(e);
                        ConfigFacilityLoader.failedConfigs.Add($"ResourceConversionRate: {conversionName}");
                    }
                }
            }

            ConfigNode[] conversionListNodes = GameDatabase.Instance.GetConfigNodes("KCResourceConversionList");
            if (conversionListNodes != null && conversionListNodes.Length > 0)
            {
                foreach (ConfigNode node in conversionListNodes)
                {
                    string conversionName = node.GetValue("name");
                    if (string.IsNullOrEmpty(conversionName))
                    {
                        ConfigFacilityLoader.exceptions.Add(new MissingFieldException("Resource conversion list without a name."));
                        ConfigFacilityLoader.failedConfigs.Add("ResourceConversionList");
                        continue;
                    }
                    string recipeName = node.GetValue("recipeName");
                    List<string> recipeNames = new List<string> { };
                    if (recipeName != null) recipeNames = recipeName.Split(',').ToList().Select(s => s.Trim()).ToList();

                    string conversionListName = node.GetValue("conversionList");
                    List<string> conversionList = new List<string> { };
                    if (conversionListName != null) conversionList = conversionListName.Split(',').ToList().Select(s => s.Trim()).ToList();

                    if (conversionList.Count == 0 && recipeNames.Count == 0)
                    {
                        ConfigFacilityLoader.exceptions.Add(new MissingFieldException($"The conversionlist {node.GetValue("name")} has no conversion list or recipe names."));
                        ConfigFacilityLoader.failedConfigs.Add($"ResourceConversionList: {conversionName}");
                        continue;
                    }

                    try
                    {
                        new ResourceConversionList(conversionName, conversionList, recipeNames);
                    }
                    catch (Exception e)
                    {
                        ConfigFacilityLoader.exceptions.Add(e);
                        ConfigFacilityLoader.failedConfigs.Add($"ResourceConversionList: {conversionName}");
                    }
                }
            }

        }

        public ResourceConversionList availableRecipes() => ((KCResourceConverterInfo)facilityInfo).availableRecipes[level];
        public ResourceConversionRate activeRecipe;
        public KCResourceConverterInfo info => (KCResourceConverterInfo)facilityInfo;
        public int ISRUcount()
        {
            IEnumerable<KeyValuePair<int, int>> isruCounts = AvailableISRUCounts.Where(kvp => kerbals.Count >= info.minKerbals[kvp.Key]);
            if (isruCounts.Count() > 0) return isruCounts.Max(kvp => kvp.Value);
            else return 0;
        }
        public int LevelISRUcount() => info.ISRUcount[level];
        public SortedDictionary<int, int> AvailableISRUCounts => new SortedDictionary<int, int>(info.ISRUcount.Where(kvp => kvp.Key <= level).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));


        protected KCResourceConverterWindow kCResourceConverterWindow;

        public bool outOfResourceDisable = true; // if true, the facility will disable itself if it cannot execute the recipe due to missing resources
        public bool outOfECDisable = true; // if true, the facility will disable itself if it cannot execute the recipe due to missing EC

        public bool outOfEC { get; protected set; } = false;

        private void executeRecipe(double dTime)
        {
            if (activeRecipe == null) return;
            if (!canExecuteRecipe(dTime)) return;

            double ISRUdTime = this.ISRUcount() * dTime;

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                KCStorageFacility.addResourceToColony(kvp.Key, -remainingResource, Colony);
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.OutputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                KCStorageFacility.addResourceToColony(kvp.Key, remainingResource, Colony);
            }
        }

        public bool canExecuteRecipe(double dTime)
        {
            if (!enabled) return false;
            else if (activeRecipe == null) return false;
            else if (outOfEC) { enabled = enabled && !outOfECDisable; return false; }
            double ISRUdTime = this.ISRUcount() * dTime;

            double availableVolume = KCStorageFacility.GetStoragesInColony(Colony).Sum(f => f.maxVolume - f.currentVolume);

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, Colony);

                if (remainingResource > KCStorageFacility.colonyResources(kvp.Key, Colony))
                {
                    enabled = enabled && !outOfResourceDisable;
                    return false;
                }

                availableVolume += remainingResource * kvp.Key.volume;
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.OutputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                availableVolume -= remainingResource * kvp.Key.volume;

                if (availableVolume < 0)
                {
                    enabled = enabled && !outOfResourceDisable;
                    return false;
                }
            }

            return true;
        }

        public override void Update()
        {
            double dTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            executeRecipe(dTime);

            base.Update();
        }

        public int ECConsumptionPriority { get; set; } = 0;
        public double ExpectedECConsumption(double lastTime, double deltaTime, double currentTime) => enabled ? facilityInfo.ECperSecond[level] * ISRUcount() * deltaTime : 0;

        public void ConsumeEC(double lastTime, double deltaTime, double currentTime) => outOfEC = false;

        public void ÍnsufficientEC(double lastTime, double deltaTime, double currentTime, double remainingEC) => outOfEC = true;

        public double DailyECConsumption() => facilityInfo.ECperSecond[level] * 6 * 3600;


        public override void OnBuildingClicked() => kCResourceConverterWindow.Toggle();

        public override void OnRemoteClicked() => kCResourceConverterWindow.Toggle();

        public override string GetFacilityProductionDisplay() => $"{(enabled ? "Enabled" : "Disabled")}\nRecipe: {activeRecipe.DisplayName}{(facilityInfo.ECperSecond[level] > 0 ? $"\nEC/s: {(enabled ? facilityInfo.ECperSecond[level] * ISRUcount() : 0):f2}" : "")}";

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            if (activeRecipe != null)
                node.AddValue("recipe", activeRecipe.RecipeName);
            node.AddValue("outOfResourceDisable", outOfResourceDisable);
            node.AddValue("outOfECDisable", outOfECDisable);
            node.AddValue("ECConsumptionPriority", ECConsumptionPriority);
            return node;
        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            if (node.GetValue("recipe") == null) activeRecipe = availableRecipes().GetRecipes().FirstOrDefault();
            else activeRecipe = ResourceConversionRate.GetConversionRate(node.GetValue("recipe"));
            if (activeRecipe == null) throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no recipe called {node.GetValue("recipe")}.");
            if (bool.TryParse(node.GetValue("outOfResourceDisable"), out bool outOfResourceDisable)) this.outOfResourceDisable = outOfResourceDisable;
            if (bool.TryParse(node.GetValue("outOfECDisable"), out bool outOfECDisable)) this.outOfECDisable = outOfECDisable;
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecConsumptionPriority)) this.ECConsumptionPriority = ecConsumptionPriority;

        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            HashSet<ResourceConversionRate> rates = availableRecipes().GetRecipes();
            activeRecipe = rates.FirstOrDefault();
        }
    }
}
