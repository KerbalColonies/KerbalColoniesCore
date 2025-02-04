using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalColonies.colonyFacilities
{


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

        public string createResourceString()
        {
            List<string> resourceStrings = new List<string>();
            inputResources.Keys.ToList().ForEach(resource =>
            {
                resourceStrings.Add($"{resource.name}&{inputResources[resource]}&input");
            });
            outputResources.Keys.ToList().ForEach(resource =>
            {
                resourceStrings.Add($"{resource.name}&{outputResources[resource]}&output");
            });
            return string.Join("|", resourceStrings);
        }

        private void parseResourceString(string resourceString)
        {
            string[] resourceStrings = resourceString.Split('|');
            foreach (string resource in resourceStrings)
            {
                string[] resourceData = resource.Split('&');

                if (resourceData[2] == "input")
                {
                    inputResources.Add(PartResourceLibrary.Instance.GetDefinition(resourceData[0]), double.Parse(resourceData[1]));
                }
                else
                {
                    outputResources.Add(PartResourceLibrary.Instance.GetDefinition(resourceData[0]), double.Parse(resourceData[1]));
                }
            }
        }

        public ResourceConversionRate(string reciptName) { this.reciptName = reciptName; }

        public ResourceConversionRate(string reciptName, string resourceString)
        {
            this.reciptName = reciptName;
            inputResources = new Dictionary<PartResourceDefinition, double>();
            outputResources = new Dictionary<PartResourceDefinition, double>();
            parseResourceString(resourceString);
        }

        public ResourceConversionRate(string reciptName, Dictionary<PartResourceDefinition, double> inputResources, Dictionary<PartResourceDefinition, double> outputResources)
        {
            this.reciptName = reciptName;
            this.inputResources = inputResources;
            this.outputResources = outputResources;
        }
    }

    public class ResourceConverter : KCKerbalFacilityBase
    {
        static Dictionary<string, PartResourceDefinition> resourceTypes = new Dictionary<string, PartResourceDefinition>
        {
            { "Ore", PartResourceLibrary.Instance.GetDefinition("Ore") },
            { "LiquidFuel", PartResourceLibrary.Instance.GetDefinition("LiquidFuel") },
            { "Oxidizer", PartResourceLibrary.Instance.GetDefinition("Oxidizer") },
            { "Monopropellant", PartResourceLibrary.Instance.GetDefinition("Monopropellant") },
            { "Oxidizer", PartResourceLibrary.Instance.GetDefinition("Oxidizer") },
            { "MetalOre", PartResourceLibrary.Instance.GetDefinition("MetalOre") },
            { "Metal", PartResourceLibrary.Instance.GetDefinition("Metal") },
            { "ScrapMetal", PartResourceLibrary.Instance.GetDefinition("ScrapMetal") },
            { "RocketParts", PartResourceLibrary.Instance.GetDefinition("RocketParts") },
        };

        /// <summary>
        /// Conversionrate per second + minimum converter level. The conversionrate are the stock ones for the Convert-O-Tron 250 and some from the extraplanetary launchpads mod, a facility has multiple simulated ones
        /// </summary>
        static Dictionary<ResourceConversionRate, int> conversionRates = new Dictionary<ResourceConversionRate, int>
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

        public ResourceConversionRate activeRecipt;
        public int ISRUcount;

        private void executeRecipt(ResourceConversionRate recipt, double dTime)
        {
            KCFacilityBase.GetInformationByFacilty(this, out string saveGame, out int bodyIndex, out string colonyName, out List<GroupPlaceHolder> gphs, out List<string> UUIDs);

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipt.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, saveGame, bodyIndex, colonyName);

                foreach (KCStorageFacility facility in facilitiesWithResource)
                {
                    Dictionary<PartResourceDefinition, float> facilityResources = facility.getRessources();

                    if (remainingResource < facilityResources[kvp.Key])
                    {
                        facility.changeAmount(kvp.Key, -(float)remainingResource);
                        break;
                    }
                    else
                    {
                        remainingResource -= facilityResources[kvp.Key];
                        facility.changeAmount(kvp.Key, -facilityResources[kvp.Key]);
                    }
                }
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipt.OutputResources)
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

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipt.InputResources)
            {
                double remainingResource = kvp.Value * dTime * ISRUcount;

                List<KCStorageFacility> facilitiesWithResource = KCStorageFacility.findFacilityWithResourceType(kvp.Key, saveGame, bodyIndex, colonyName);

                if (facilitiesWithResource.Count == 0)
                {
                    return false;
                }

                foreach (KCStorageFacility facility in facilitiesWithResource)
                {
                    Dictionary<PartResourceDefinition, float> facilityResources = facility.getRessources();
                    remainingResource -= facilityResources[kvp.Key];
                    if (remainingResource < 0) { break; }
                }

                if (remainingResource > 0)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<PartResourceDefinition, double> kvp in activeRecipt.OutputResources)
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

        public override void Update()
        {
            double dTime = Planetarium.GetUniversalTime() - lastUpdateTime;

            if (enabled)
            {
                if (canExecuteRecipt(activeRecipt, dTime))
                {
                    executeRecipt(activeRecipt, dTime);
                }
                else { enabled = false; }
            }

            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public override void EncodeString()
        {
            string kerbalString = CreateKerbalString(kerbals);
            facilityData = $"recipt&{activeRecipt.ReciptName}|maxKerbals&{maxKerbals}{((kerbalString != "") ? $"|{kerbalString}" : "")}";
        }

        public override void DecodeString()
        {
            if (facilityData != "")
            {
                string[] facilityDatas = facilityData.Split(new[] { '|' }, 3);
                activeRecipt = conversionRates.Where(recipt => { return recipt.Key.ReciptName == facilityDatas[0].Split('&')[0]; }).First().Key;
                maxKerbals = Convert.ToInt32(facilityDatas[1].Split('&')[1]);
                if (facilityDatas.Length > 1)
                {
                    kerbals = CreateKerbalList(facilityDatas[2]);
                }
            }
        }

        public override void Initialize(string facilityData)
        {
            base.Initialize(facilityData);

            ISRUcount = new int[2] { 4, 6 }[level];
        }

        public ResourceConverter(bool enabled, string facilityData = "") : base("KCResourceConverter", enabled, 4, facilityData, 0, 1)
        {

        }
    }
}
