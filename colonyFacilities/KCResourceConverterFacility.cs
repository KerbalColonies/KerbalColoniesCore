using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KerbalColonies.UI;

namespace KerbalColonies.colonyFacilities
{
    internal class RecipeSelectorWindow : KCWindowBase
    {
        KCResourceConverterFacility resourceConverter;

        Vector2 scrollPos;

        protected override void CustomWindow()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            foreach (ResourceConversionRate recipe in KCResourceConverterFacility.conversionRates.Where(kvp => kvp.Value <= resourceConverter.level).ToDictionary(i => i.Key, i => i.Value).Keys)
            {
                GUILayout.Label(recipe.ReciptName);
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

                if (GUILayout.Button("Use this recipt"))
                {
                    resourceConverter.activeRecipe = recipe;
                    this.Close();
                }
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


    internal class KCResourceConverterWindow : KCWindowBase
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

            GUILayout.Label($"Current recipe: {recipe.ReciptName}");

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

            kerbalGUI.StaffingInterface();
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

        internal KCResourceConverterWindow(KCResourceConverterFacility resourceConverter) : base(Configuration.createWindowID(), "Resourceconverter")
        {
            this.resourceConverter = resourceConverter;
            this.recipeSelector = new RecipeSelectorWindow(resourceConverter);
            this.kerbalGUI = null;
            toolRect = new Rect(100, 100, 400, 800);

        }
    }

    public class ResourceConversionRate
    {
        private string reciptName;
        public string ReciptName { get { return reciptName; } }

        private Dictionary<PartResourceDefinition, double> inputResources;
        private Dictionary<PartResourceDefinition, double> outputResources;

        public Dictionary<PartResourceDefinition, double> InputResources { get => inputResources; }
        public Dictionary<PartResourceDefinition, double> OutputResources { get => outputResources; }

        public void addInputResource(PartResourceDefinition resource, double amount)
        {
            inputResources.Add(resource, amount);
        }
        public void addOutputResource(PartResourceDefinition resource, double amount)
        {
            outputResources.Add(resource, amount);
        }

        public ResourceConversionRate(string reciptName) { this.reciptName = reciptName; }

        public ResourceConversionRate(string reciptName, Dictionary<PartResourceDefinition, double> inputResources, Dictionary<PartResourceDefinition, double> outputResources)
        {
            this.reciptName = reciptName;
            this.inputResources = inputResources;
            this.outputResources = outputResources;
        }
    }

    public class KCResourceConverterFacility : KCKerbalFacilityBase
    {
        public static Dictionary<string, PartResourceDefinition> resourceTypes = new Dictionary<string, PartResourceDefinition>
        {
            { "Ore", PartResourceLibrary.Instance.GetDefinition("Ore") },
            { "LiquidFuel", PartResourceLibrary.Instance.GetDefinition("LiquidFuel") },
            { "Oxidizer", PartResourceLibrary.Instance.GetDefinition("Oxidizer") },
            { "Monopropellant", PartResourceLibrary.Instance.GetDefinition("MonoPropellant") },
            { "MetalOre", PartResourceLibrary.Instance.GetDefinition("MetalOre") },
            { "Metal", PartResourceLibrary.Instance.GetDefinition("Metal") },
            { "ScrapMetal", PartResourceLibrary.Instance.GetDefinition("ScrapMetal") },
            { "RocketParts", PartResourceLibrary.Instance.GetDefinition("RocketParts") },
        };

        /// <summary>
        /// Conversionrate per second + minimum converter level. The conversionrate are the stock ones for the Convert-O-Tron 250 and some from the extraplanetary launchpads mod, a facility has multiple simulated ones
        /// </summary>
        public static Dictionary<ResourceConversionRate, int> conversionRates = new Dictionary<ResourceConversionRate, int>
        {
            {
               new ResourceConversionRate(
                   "Ore2LfOx",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Ore"], 0.5 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["LiquidFuel"], 0.45 }, { resourceTypes["Oxidizer"], 0.55 }, }
               ), 0
            },
            {
                new ResourceConversionRate(
                    "Ore2Monoprop",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Ore"], 0.5 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Monopropellant"], 1 }, }
               ), 0
            },
            {
               new ResourceConversionRate(
                   "Ore2Lf",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Ore"], 0.5 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["LiquidFuel"], 0.9 }, }
               ), 0
            },
            {
               new ResourceConversionRate(
                   "Ore2Ox",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Ore"], 0.5 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Oxidizer"], 1.1 }, }
               ), 0
            },
            {
               new ResourceConversionRate(
                   "MetalOre2Metal",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["MetalOre"], 0.05110022 }, { resourceTypes["LiquidFuel"], 0.00480766 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Metal"], 0.0357408 }, }
               ), 0
            },
            {
               new ResourceConversionRate(
                   "ScrapMetal2Metal",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["ScrapMetal"], 0.05 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Metal"], 0.05 }, }
               ), 0
            },
            {
               new ResourceConversionRate(
                   "Metal2RocketsParts",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["Metal"], 0.0312 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["RocketParts"], 0.7 }, { resourceTypes["ScrapMetal"], 0.295 }, }
               ), 0
            },
            {
               new ResourceConversionRate(
                   "ScrapMetal2RocketsParts",
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["ScrapMetal"], 0.005 }, },
                   new Dictionary<PartResourceDefinition, double> { { resourceTypes["RocketParts"], 0.495 }, }
               ), 0
            },
        };

        public ResourceConversionRate activeRecipe;
        public Dictionary<int, int> ISRUcount { get; private set; } = new Dictionary<int, int> { };
        private KCResourceConverterWindow kCResourceConverterWindow;

        private void executeRecipt(ResourceConversionRate recipt, double dTime)
        {
            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount[level];

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
                double remainingResource = kvp.Value * dTime * ISRUcount[level];

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

        private bool canExecuteRecipt(ResourceConversionRate recipt, double dTime)
        {
            bool canExecute = true;

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount[level];

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
                double remainingResource = kvp.Value * dTime * ISRUcount[level];

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
                if (canExecuteRecipt(activeRecipe, dTime))
                {
                    executeRecipt(activeRecipe, dTime);
                }
                else { enabled = false; }
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

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("recipt", activeRecipe.ReciptName);

            return node;
        }

        private void configNodeLoader(ConfigNode node)
        {
            ConfigNode levelNode = facilityInfo.facilityConfig.GetNode("level");
            for (int i = 0; i <= maxLevel; i++)
            {
                ConfigNode iLevel = levelNode.GetNode(i.ToString());
                if (iLevel.HasValue("ISRUcount")) ISRUcount[level] = int.Parse(iLevel.GetValue("ISRUcount"));
                else if (i > 0) ISRUcount = ISRUcount;
                else throw new MissingFieldException($"The facility {facilityInfo.name} (type: {facilityInfo.type}) has no ISRUcount (at least for level 0).");
            }
        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            activeRecipe = conversionRates.First(recipt => recipt.Key.ReciptName == node.GetValue("recipt")).Key;
        }

        public KCResourceConverterFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            configNodeLoader(facilityInfo.facilityConfig);
            kCResourceConverterWindow = new KCResourceConverterWindow(this);

            this.activeRecipe = conversionRates.ElementAt(0).Key;
        }
    }
}
