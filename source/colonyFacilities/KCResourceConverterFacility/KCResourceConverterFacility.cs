using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.ResourceManagment;
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

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class KCResourceConverterFacility : KCKerbalFacilityBase, IKCResourceProducer, IKCResourceConsumer
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
                    Dictionary<PartResourceDefinition, double> InputResources = [];
                    Dictionary<PartResourceDefinition, double> OutputResources = [];

                    ConfigNode inputNode = node.GetNode("inputResources");
                    foreach (ConfigNode.Value v in inputNode.values)
                    {
                        Configuration.writeDebug($"Loading input resource {v.name} with amount {v.value}");
                        PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(v.name);
                        if (resourceDef == null)
                        {
                            Configuration.writeLog(node.ToString());
                            Configuration.writeLog($"ResourceConversionRate {conversionName}: the PartResourceLibrary contains no definition for {v.name}.");

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
                            Configuration.writeLog(node.ToString());
                            Configuration.writeLog($"ResourceConversionRate {conversionName}: the PartResourceLibrary contains no definition for {v.name}.");

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
                        Configuration.writeLog(node.ToString());
                        Configuration.writeLog($"KCResourceConversionList: ResourceConversionRate {conversionName}: resource conversion list without a name.");

                        ConfigFacilityLoader.exceptions.Add(new MissingFieldException("Resource conversion list without a name."));
                        ConfigFacilityLoader.failedConfigs.Add("ResourceConversionList");
                        continue;
                    }
                    string recipeName = node.GetValue("recipeName");
                    List<string> recipeNames = [];
                    if (recipeName != null) recipeNames = recipeName.Split(',').ToList().Select(s => s.Trim()).ToList();

                    string conversionListName = node.GetValue("conversionList");
                    List<string> conversionList = [];
                    if (conversionListName != null) conversionList = conversionListName.Split(',').ToList().Select(s => s.Trim()).ToList();

                    if (conversionList.Count == 0 && recipeNames.Count == 0)
                    {
                        Configuration.writeLog(node.ToString());
                        Configuration.writeLog($"KCResourceConversionList: ResourceConversionRate {conversionName}: has no conversion list or recipe names.");

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
                        Configuration.writeLog(e.InnerException.ToString());
                        Configuration.writeLog(e.ToString());

                        ConfigFacilityLoader.exceptions.Add(e);
                        ConfigFacilityLoader.failedConfigs.Add($"ResourceConversionList: {conversionName}");
                    }
                }
            }

        }

        public ResourceConversionList availableRecipes() => ((KCResourceConverterInfo)facilityInfo).availableRecipes[level];
        public ResourceConversionRate activeRecipe { get; protected set; }
        public Dictionary<PartResourceDefinition, bool> resourceLimitsEnabled = [];
        public Dictionary<PartResourceDefinition, double> resourceLimits = [];

        protected Dictionary<PartResourceDefinition, double> lastResourceProduction = [];
        protected Dictionary<PartResourceDefinition, double> lastResourceConsumption = [];

        public KCResourceConverterInfo info => (KCResourceConverterInfo)facilityInfo;
        public int ISRUcount()
        {
            IEnumerable<KeyValuePair<int, int>> isruCounts = AvailableISRUCounts.Where(kvp => kerbals.Count >= info.minKerbals[kvp.Key]);
            return isruCounts.Count() > 0 ? isruCounts.Max(kvp => kvp.Value) : 0;
        }
        public int LevelISRUcount() => info.ISRUcount[level];
        public SortedDictionary<int, int> AvailableISRUCounts => new(info.ISRUcount.Where(kvp => kvp.Key <= level).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));


        protected KCResourceConverterWindow kCResourceConverterWindow;

        public bool outOfResourceDisable = true; // if true, the facility will disable itself if it cannot execute the recipe due to missing resources

        public bool outOfResources { get; protected set; } = false;
        public bool resourceLimited { get; protected set; } = false;

        public void ChangeRecipe(ResourceConversionRate newRecipe)
        {
            if (newRecipe == null) throw new ArgumentNullException("newRecipe");
            if (!availableRecipes().GetRecipes().Contains(newRecipe)) throw new ArgumentException($"The recipe {newRecipe.RecipeName} is not available for this facility at this level.");
            activeRecipe = newRecipe;

            resourceLimitsEnabled.Clear();
            resourceLimits.Clear();

            activeRecipe.InputResources.Keys.ToList().ForEach(r =>
            {
                resourceLimitsEnabled.TryAdd(r, false);
                resourceLimits.TryAdd(r, 0);
            });
            activeRecipe.OutputResources.Keys.ToList().ForEach(r =>
            {
                resourceLimitsEnabled.TryAdd(r, false);
                resourceLimits.TryAdd(r, 0);
            });
        }

        private void executeRecipe(double dTime)
        {
            if (activeRecipe == null) return;
            if (!canExecuteRecipe(dTime)) return;

            double ISRUdTime = ISRUcount() * dTime;

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                lastResourceConsumption[kvp.Key] = -remainingResource;
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.OutputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                lastResourceProduction[kvp.Key] = remainingResource;
            }
        }

        public bool canExecuteRecipe(double dTime)
        {
            if (!enabled) return false;
            else if (activeRecipe == null) return false;
            else if (outOfResources) { enabled = enabled && !outOfResourceDisable; return false; }
            double ISRUdTime = ISRUcount() * dTime;

            double availableVolume = KCUnifiedColonyStorage.colonyStorages[Colony].FreeVolume;

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                if (KCUnifiedColonyStorage.colonyStorages[Colony].Resources.GetValueOrDefault(kvp.Key) - (resourceLimitsEnabled[kvp.Key] ? resourceLimits[kvp.Key] : 0) - remainingResource < 0)
                {
                    enabled = enabled && !outOfResourceDisable;
                    resourceLimited = true;
                    return false;
                }

                availableVolume += remainingResource * kvp.Key.volume;
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.OutputResources)
            {
                double remainingResource = kvp.Value * ISRUdTime;

                availableVolume -= remainingResource * kvp.Key.volume;

                if (availableVolume < 0 || (resourceLimitsEnabled[kvp.Key] && KCUnifiedColonyStorage.colonyStorages[Colony].Resources.GetValueOrDefault(kvp.Key) + remainingResource > resourceLimits[kvp.Key]))
                {
                    enabled = enabled && !outOfResourceDisable;
                    resourceLimited = true;
                    return false;
                }
            }

            resourceLimited = false;
            return true;
        }

        public int ResourceConsumptionPriority { get; set; } = 0;

        public Dictionary<PartResourceDefinition, double> ResourceProduction(double lastTime, double deltaTime, double currentTime)
        {
            executeRecipe(deltaTime);

            Dictionary<PartResourceDefinition, double> producedResources = new(lastResourceProduction);
            lastResourceProduction.Clear();
            return producedResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourcesPerSecond() => enabled && !resourceLimited ? activeRecipe.OutputResources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * ISRUcount()) : [];

        public Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime) => enabled && !resourceLimited ? activeRecipe.OutputResources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * ISRUcount() * deltaTime) : [];

        public void ConsumeResources(double lastTime, double deltaTime, double currentTime)
        {
            outOfResources = false;
        }

        public Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources)
        {
            outOfResources = true;
            limitingResources.AddAll(sufficientResources);
            return limitingResources;
        }

        public Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond()
        {
            throw new NotImplementedException();
        }



        public override void OnBuildingClicked() => kCResourceConverterWindow.Toggle();

        public override void OnRemoteClicked() => kCResourceConverterWindow.Toggle();

        public override string GetFacilityProductionDisplay() => $"{(enabled ? "Enabled" : "Disabled")}\nRecipe: {activeRecipe.DisplayName}";

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            if (activeRecipe != null)
                node.AddValue("recipe", activeRecipe.RecipeName);
            node.AddValue("outOfResourceDisable", outOfResourceDisable);
            node.AddValue("ECConsumptionPriority", ResourceConsumptionPriority);

            ConfigNode limitsEnabledNode = new("resourceLimitsEnabled");
            ConfigNode limitsNode = new("resourceLimits");
            resourceLimits.ToList().ForEach(kvp =>
            {
                limitsEnabledNode.AddValue(kvp.Key.name, resourceLimitsEnabled[kvp.Key]);
                limitsNode.AddValue(kvp.Key.name, kvp.Value.ToString());
            });
            node.AddNode(limitsEnabledNode);
            node.AddNode(limitsNode);

            return node;
        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            activeRecipe = node.GetValue("recipe") == null
                ? availableRecipes().GetRecipes().FirstOrDefault()
                : ResourceConversionRate.GetConversionRate(node.GetValue("recipe"));
            if (activeRecipe == null) throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no recipe called {node.GetValue("recipe")}.");
            if (bool.TryParse(node.GetValue("outOfResourceDisable"), out bool outOfResourceDisable)) this.outOfResourceDisable = outOfResourceDisable;
            if (int.TryParse(node.GetValue("ECConsumptionPriority"), out int ecConsumptionPriority)) ResourceConsumptionPriority = ecConsumptionPriority;

            if (node.HasNode("resourceLimitsEnabled"))
            {
                ConfigNode limitsEnabledNode = node.GetNode("resourceLimitsEnabled");
                ConfigNode limitsNode = node.GetNode("resourceLimits");

                foreach (ConfigNode.Value v in limitsEnabledNode.values)
                    resourceLimitsEnabled.TryAdd(PartResourceLibrary.Instance.GetDefinition(v.name), bool.Parse(v.value));
                foreach (ConfigNode.Value v in limitsNode.values)
                    resourceLimits.TryAdd(PartResourceLibrary.Instance.GetDefinition(v.name), double.Parse(v.value));
            }


            activeRecipe.InputResources.Keys.ToList().ForEach(r =>
            {
                resourceLimitsEnabled.TryAdd(r, false);
                resourceLimits.TryAdd(r, 0);
            });

            activeRecipe.OutputResources.Keys.ToList().ForEach(r =>
            {
                resourceLimitsEnabled.TryAdd(r, false);
                resourceLimits.TryAdd(r, 0);
            });
        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            HashSet<ResourceConversionRate> rates = availableRecipes().GetRecipes();
            activeRecipe = rates.FirstOrDefault();

            activeRecipe.InputResources.Keys.ToList().ForEach(r =>
            {
                resourceLimitsEnabled.TryAdd(r, false);
                resourceLimits.TryAdd(r, 0);
            });

            activeRecipe.OutputResources.Keys.ToList().ForEach(r =>
            {
                resourceLimitsEnabled.TryAdd(r, false);
                resourceLimits.TryAdd(r, 0);
            });
        }
    }
}
