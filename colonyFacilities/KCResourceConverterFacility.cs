using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalColonies.colonyFacilities
{
    internal class KCResourceConverterFacilityCost : KCFacilityCostClass
    {
        public KCResourceConverterFacilityCost()
        {
            resourceCost = new Dictionary<int, Dictionary<PartResourceDefinition, double>> {
                { 0, new Dictionary<PartResourceDefinition, double> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 500 } } },
                { 1, new Dictionary<PartResourceDefinition, double> {
                    { PartResourceLibrary.Instance.GetDefinition("RocketParts"), 1000 } }
                }
            };
        }
    }


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

        internal RecipeSelectorWindow(KCResourceConverterFacility resourceConverter) : base(Configuration.createWindowID(resourceConverter), "Recipe Selector")
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
                KCFacilityBase.GetInformationByFacilty(resourceConverter, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gph, out List<string> UUIDs);
                kerbalGUI = new KerbalGUI(resourceConverter, saveGame, bodyIndex, colonyName);
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

                if (resourceConverter.getKerbals().Count() < resourceConverter.maxKerbals)
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
        }

        internal KCResourceConverterWindow(KCResourceConverterFacility resourceConverter) : base(Configuration.createWindowID(resourceConverter), "Resourceconverter")
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
        public int ISRUcount;
        private KCResourceConverterWindow kCResourceConverterWindow;

        private void executeRecipt(ResourceConversionRate recipt, double dTime)
        {
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs);

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, saveGame, bodyIndex, colonyName);

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
                double remainingResource = kvp.Value * dTime * ISRUcount;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, saveGame, bodyIndex, colonyName);

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

            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs);

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipe.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, saveGame, bodyIndex, colonyName);

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
                double remainingResource = kvp.Value * dTime * ISRUcount;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, saveGame, bodyIndex, colonyName);
                bool addResource = false;

                if (facilitiesWithResource.Count == 0)
                {
                    facilitiesWithResource = KCStorageFacility.findEmptyStorageFacilities(saveGame, bodyIndex, colonyName);
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

        public override List<ProtoCrewMember> filterKerbals(List<ProtoCrewMember> kerbals)
        {
            return kerbals.Where(k => k.experienceTrait.Title == "Engineer").ToList();
        }

        public override int GetUpgradeTime(int level)
        {
            // 1 Kerbin day = 0.25 days
            // 100 per day * 5 engineers = 500 per day
            // 500 per day * 4 kerbin days = 500

            // 1 Kerbin day = 0.25 days
            // 100 per day * 5 engineers = 500 per day
            // 500 per day * 2 kerbin days = 250
            int[] buildTimes = { 500, 500 };
            return buildTimes[level];
        }

        public override void Update()
        {
            double dTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            if (getKerbals().Count() < maxKerbals)
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

        public override ConfigNode getCustomNode()
        {
            ConfigNode node = new ConfigNode();
            node.AddValue("recipt", activeRecipe.ReciptName);

            ConfigNode wrapperNode = new ConfigNode("wrapper");
            wrapperNode.AddNode(base.getCustomNode());
            node.AddNode(wrapperNode);

            return node;
        }

        public override void loadCustomNode(ConfigNode customNode)
        {
            activeRecipe = conversionRates.Where(recipt => { return recipt.Key.ReciptName == customNode.GetValue("recipt"); }).First().Key;

            base.loadCustomNode(customNode.GetNode("wrapper").GetNodes()[0]);
        }

        public override void Initialize()
        {
            base.Initialize();
            this.baseGroupName = "KC_CAB";
            ISRUcount = new int[2] { 2, 4 }[level];
            kCResourceConverterWindow = new KCResourceConverterWindow(this);
        }

        public KCResourceConverterFacility(bool enabled) : base("KCResourceConverterFacility", enabled, 4, 0, 1)
        {
            this.activeRecipe = conversionRates.ElementAt(0).Key;
        }
    }
}
