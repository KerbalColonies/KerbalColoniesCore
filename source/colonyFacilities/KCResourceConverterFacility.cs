using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class KCResourceConverterInfo : KCKerbalFacilityInfoClass
    {
        public Dictionary<int, ResourceConversionList> availableRecipes { get; private set; } = new Dictionary<int, ResourceConversionList> { };
        public Dictionary<int, int> ISRUcount { get; private set; } = new Dictionary<int, int> { };

        public KCResourceConverterInfo(ConfigNode node) : base(node)
        {
            foreach (KeyValuePair<int, ConfigNode> levelNode in levelNodes)
            {
                if (levelNode.Value.HasValue("conversionList"))
                {
                    string conversionListName = levelNode.Value.GetValue("conversionList");
                    ResourceConversionList conversionList = ResourceConversionList.GetConversionList(conversionListName);
                    if (conversionList != null)
                    {
                        availableRecipes.Add(levelNode.Key, conversionList);
                    }
                    else
                    {
                        throw new MissingFieldException($"The facility {name} (type: {type}) has no conversion list called {conversionListName}.");
                    }
                }
                else if (levelNode.Key > 0) availableRecipes.Add(levelNode.Key, availableRecipes[levelNode.Key - 1]);
                else throw new MissingFieldException($"The facility {name} (type: {type}) has no conversion list (at least for level 0).");

                if (levelNode.Value.HasValue("ISRUcount")) ISRUcount[levelNode.Key] = int.Parse(levelNode.Value.GetValue("ISRUcount"));
                else if (levelNode.Key > 0) ISRUcount[levelNode.Key] = ISRUcount[levelNode.Key - 1];
                else throw new MissingFieldException($"The facility {name} has no ISRUcount (at least for level 0).");
            }
        }
    }

    public class RecipeSelectorWindow : KCWindowBase
    {
        KCResourceConverterFacility resourceConverter;

        Vector2 scrollPos;

        protected override void CustomWindow()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            foreach (ResourceConversionRate recipe in resourceConverter.availableRecipes().GetRecipes())
            {
                GUILayout.Label(recipe.DisplayName);
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.Label("Input:");
                foreach (PartResourceDefinition prd in recipe.InputResources.Keys)
                {
                    GUILayout.Label(prd.displayName);
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("Amount:");
                foreach (double amount in recipe.InputResources.Values)
                {
                    GUILayout.Label(amount.ToString());
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("Output:");
                foreach (PartResourceDefinition prd in recipe.OutputResources.Keys)
                {
                    GUILayout.Label(prd.displayName);
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label("Amount:");
                foreach (double amount in recipe.OutputResources.Values)
                {
                    GUILayout.Label(amount.ToString());
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                if (GUILayout.Button("Use this recipe"))
                {
                    resourceConverter.activeRecipe = recipe;
                    this.Close();
                }
                GUILayout.Space(10);
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(10);
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        internal RecipeSelectorWindow(KCResourceConverterFacility resourceConverter) : base(Configuration.createWindowID(), "Recipe Selector")
        {
            this.resourceConverter = resourceConverter;
            toolRect = new Rect(100, 100, 400, 800);
        }
    }


    internal class KCResourceConverterWindow : KCFacilityWindowBase
    {
        KCResourceConverterFacility resourceConverter;
        private RecipeSelectorWindow recipeSelector;
        public KerbalGUI kerbalGUI;

        protected override void CustomWindow()
        {
            resourceConverter.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(resourceConverter, true);
            }

            ResourceConversionRate recipe = resourceConverter.activeRecipe;
            if (recipe == null)
            {
                GUILayout.Label($"Failed to load a recipe");
                if (GUILayout.Button("Select a new Recipe:"))
                {
                    recipeSelector.Open();
                }
                return;
            }

            GUILayout.Label($"Current recipe: {recipe.DisplayName}");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Input:");
            foreach (PartResourceDefinition prd in recipe.InputResources.Keys)
            {
                GUILayout.Label(prd.displayName);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Amount:");
            foreach (double amount in recipe.InputResources.Values)
            {
                GUILayout.Label(amount.ToString());
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Output:");
            foreach (PartResourceDefinition prd in recipe.OutputResources.Keys)
            {
                GUILayout.Label(prd.displayName);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Amount:");
            foreach (double amount in recipe.OutputResources.Values)
            {
                GUILayout.Label(amount.ToString());
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Select a new Recipe:"))
            {
                recipeSelector.Open();
            }

            if (!resourceConverter.enabled)
            {
                GUILayout.Label("This facility is disabled");

                if (resourceConverter.getKerbals().Count() < resourceConverter.MaxKerbals)
                {
                    GUI.enabled = false;
                }
                else
                {
                    GUI.enabled = true;
                }

                if (GUILayout.Button("enable"))
                {
                    resourceConverter.enabled = true;
                }
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("This facility is enabled");

                if (GUILayout.Button("disable"))
                {
                    resourceConverter.enabled = false;
                }
            }
            if (GUILayout.Button($"Disable facility if resources are missing: {(resourceConverter.outOfResourceDisable ? "Yes" : "No")}")) resourceConverter.outOfResourceDisable = !resourceConverter.outOfResourceDisable;

            kerbalGUI.StaffingInterface();

            GUILayout.Label($"For operations this facility must have the maximum number of kerbals assigned.");
        }

        protected override void OnClose()
        {
            recipeSelector.Close();
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }

        internal KCResourceConverterWindow(KCResourceConverterFacility resourceConverter) : base(resourceConverter, Configuration.createWindowID())
        {
            this.resourceConverter = resourceConverter;
            this.recipeSelector = new RecipeSelectorWindow(resourceConverter);
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 400, 800);

        }
    }

    public class ResourceConversionRate
    {
        public static HashSet<ResourceConversionRate> conversionRates = new HashSet<ResourceConversionRate> { };

        public static ResourceConversionRate GetConversionRate(string name) => conversionRates.FirstOrDefault(rcr => rcr.RecipeName == name);

        public static bool operator ==(ResourceConversionRate a, ResourceConversionRate b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.RecipeName == b.RecipeName;
        }
        public static bool operator !=(ResourceConversionRate a, ResourceConversionRate b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;
            return a.RecipeName != b.RecipeName;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceConversionRate && ((ResourceConversionRate)obj).recipeName == this.recipeName;
        }

        public override int GetHashCode()
        {
            return recipeName.GetHashCode();
        }

        private string recipeName;
        public string RecipeName { get { return recipeName; } }

        public string DisplayName;

        private Dictionary<PartResourceDefinition, double> inputResources;
        private Dictionary<PartResourceDefinition, double> outputResources;

        public Dictionary<PartResourceDefinition, double> InputResources { get => inputResources; }
        public Dictionary<PartResourceDefinition, double> OutputResources { get => outputResources; }

        public ResourceConversionRate(string recipeName, string displayName, Dictionary<PartResourceDefinition, double> inputResources, Dictionary<PartResourceDefinition, double> outputResources)
        {
            this.recipeName = recipeName;
            this.DisplayName = displayName;
            this.inputResources = inputResources;
            this.outputResources = outputResources;

            if (!conversionRates.Contains(this)) conversionRates.Add(this);
            else throw new Exception($"The recipe {recipeName} already exists in the list of conversion rates. Please check your config file.");
        }
    }

    public class ResourceConversionList
    {
        public static HashSet<ResourceConversionList> AllConversionLists = new HashSet<ResourceConversionList> { }; // all the lists of recipes

        public static ResourceConversionList GetConversionList(string name) => AllConversionLists.FirstOrDefault(rcl => rcl.Name == name);

        public static bool operator ==(ResourceConversionList a, ResourceConversionList b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Name == b.Name;
        }
        public static bool operator !=(ResourceConversionList a, ResourceConversionList b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;
            return a.Name != b.Name;
        }
        public override bool Equals(object obj)
        {
            return obj is ResourceConversionList && ((ResourceConversionList)obj).Name == this.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public string Name { get; set; } // the name of the list
        public List<string> ConversionList { get; set; } = new List<string> { }; // the names of other lists
        public List<string> RecipeNames { get; set; } = new List<string> { }; // the names of the recipes

        public HashSet<ResourceConversionRate> GetRecipes()
        {
            HashSet<ResourceConversionRate> resourceConversionRates = new HashSet<ResourceConversionRate>();
            RecipeNames.ForEach(r =>
            {
                ResourceConversionRate rcr = ResourceConversionRate.GetConversionRate(r);
                if (rcr != null) resourceConversionRates.Add(rcr);
            });
            ConversionList.ForEach(l =>
            {
                ResourceConversionList rcl = AllConversionLists.FirstOrDefault(r => r.Name == l);
                if (rcl != null)
                {
                    resourceConversionRates.UnionWith(rcl.GetRecipes());
                }
            });

            return resourceConversionRates;
        }

        public ResourceConversionList(string name, List<string> ConversionList = null, List<string> RecipeNames = null)
        {
            Name = name;
            if (ConversionList != null) this.ConversionList = ConversionList;
            if (RecipeNames != null) this.RecipeNames = RecipeNames;

            if (!AllConversionLists.Contains(this)) AllConversionLists.Add(this);
            else throw new Exception($"The recipe list {name} already exists in the list of conversion lists. Please check your config file.");
        }
    }

    public class KCResourceConverterFacility : KCKerbalFacilityBase
    {
        public static void LoadResourceConversionLists()
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("KCResourceConversionRate");
            if (nodes != null && nodes.Length > 0)
            {
                foreach (ConfigNode node in nodes)
                {
                    string conversionName = node.GetValue("name");
                    if (string.IsNullOrEmpty(conversionName)) throw new MissingFieldException($"The facility {node.GetValue("name")} has no name.");
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
                            Configuration.writeDebug($"The PartResourceLibrary contains no definition for {v.name}.");
                            throw new MissingFieldException($"{conversionName} contains an invalid input resource name {v.name}.");
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
                            Configuration.writeDebug($"The PartResourceLibrary contains no definition for {v.name}.");
                            throw new MissingFieldException($"{conversionName} contains an invalid output resource name {v.name}.");
                        }
                        double amount = double.Parse(v.value);
                        OutputResources.Add(resourceDef, amount);
                    }
                    try
                    {
                        new ResourceConversionRate(conversionName, displayName, InputResources, OutputResources);
                    }
                    catch(Exception e)
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
                    if (string.IsNullOrEmpty(conversionName)) throw new MissingFieldException($"The facility {node.GetValue("name")} has no name.");

                    string recipeName = node.GetValue("recipeName");
                    List<string> recipeNames = new List<string> { };
                    if (recipeName != null) recipeNames = recipeName.Split(',').ToList().Select(s => s.Trim()).ToList();

                    string conversionListName = node.GetValue("conversionList");
                    List<string> conversionList = new List<string> { };
                    if (conversionListName != null) conversionList = conversionListName.Split(',').ToList().Select(s => s.Trim()).ToList();

                    if (conversionList.Count == 0 && recipeNames.Count == 0)
                    {
                        throw new MissingFieldException($"The conversionlist {node.GetValue("name")} has no conversion list or recipe names.");
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
        public int ISRUcount() => ((KCResourceConverterInfo)facilityInfo).ISRUcount[level];
        private KCResourceConverterWindow kCResourceConverterWindow;

        public bool outOfResourceDisable = true; // if true, the facility will disable itself if it cannot execute the recipe due to missing resources

        private void executeRecipe(double dTime)
        {
            if (activeRecipe == null) return;
            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount();

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, Colony);

                foreach (KCStorageFacility facility in facilitiesWithResource)
                {
                    Dictionary<PartResourceDefinition, double> facilityResources = facility.getRessources();

                    if (remainingResource < facilityResources[kvp.Key])
                    {
                        facility.changeAmount(kvp.Key, -remainingResource);
                        break;
                    }
                    else
                    {
                        remainingResource -= facilityResources[kvp.Key];
                        facility.changeAmount(kvp.Key, -facilityResources[kvp.Key]);
                    }
                }
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.OutputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount();

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, Colony);

                foreach (KCStorageFacility facility in facilitiesWithResource)
                {
                    if (remainingResource < facility.getEmptyAmount(kvp.Key))
                    {
                        facility.changeAmount(kvp.Key, (float)remainingResource);
                        break;
                    }
                    else
                    {
                        remainingResource -= facility.getEmptyAmount(kvp.Key);
                        facility.changeAmount(kvp.Key, facility.getEmptyAmount(kvp.Key));
                    }
                }
            }
        }

        public bool canExecuteRecipe(double dTime)
        {
            if (activeRecipe == null) return false;
            bool canExecute = true;

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount();

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, Colony);

                if (facilitiesWithResource.Count == 0)
                {
                    return false;
                }

                foreach (KCStorageFacility facility in facilitiesWithResource)
                {
                    Dictionary<PartResourceDefinition, double> facilityResources = facility.getRessources();
                    remainingResource -= facilityResources[kvp.Key];
                    if (remainingResource < 0) { break; }
                }

                if (remainingResource > 0)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.OutputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount();

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, Colony);
                bool addResource = false;

                if (facilitiesWithResource.Count == 0)
                {
                    facilitiesWithResource = KCStorageFacility.findEmptyStorageFacilities(Colony);
                    addResource = true;
                }

                foreach (KCStorageFacility facility in facilitiesWithResource)
                {
                    if (addResource) { facility.addRessource(kvp.Key); }

                    remainingResource -= facility.getEmptyAmount(kvp.Key);

                    if (remainingResource < 0) { break; }
                }

                if (remainingResource > 0)
                {
                    return false;
                }
            }

            return canExecute;
        }

        public override void Update()
        {
            double dTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            if (getKerbals().Count() < MaxKerbals)
            {
                enabled = false;
            }


            if (enabled)
            {
                if (canExecuteRecipe(dTime)) executeRecipe(dTime);
                else if (outOfResourceDisable) enabled = false;
            }

            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public override void OnBuildingClicked()
        {
            kCResourceConverterWindow.Toggle();
        }

        public override void OnRemoteClicked()
        {
            kCResourceConverterWindow.Toggle();
        }

        public override string GetFacilityProductionDisplay() => $"{(enabled ? "Enabled" : "Disabled")}\nRecipe: {activeRecipe.DisplayName}";

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            if (activeRecipe != null)
                node.AddValue("recipe", activeRecipe.RecipeName);
            node.AddValue("outOfResourceDisable", outOfResourceDisable);
            return node;
        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            if (node.GetValue("recipe") == null) activeRecipe = availableRecipes().GetRecipes().FirstOrDefault();
            else activeRecipe = ResourceConversionRate.GetConversionRate(node.GetValue("recipe"));
            if (activeRecipe == null) throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no recipe called {node.GetValue("recipe")}.");
            if (bool.TryParse(node.GetValue("outOfResourceDisable"), out bool outOfResourceDisable)) this.outOfResourceDisable = outOfResourceDisable;
            else this.outOfResourceDisable = true; // default value if not set

        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            HashSet<ResourceConversionRate> rates = availableRecipes().GetRecipes();
            activeRecipe = rates.FirstOrDefault();
        }
    }
}
