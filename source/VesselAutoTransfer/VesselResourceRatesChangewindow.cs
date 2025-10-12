using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.VesselAutoTransfer
{
    public class VesselResourceRatesChangewindow : KCWindowBase
    {
        ModuleKCTransfer transferModule = null;

        protected Dictionary<PartResourceDefinition, string> rateStrings = new Dictionary<PartResourceDefinition, string>();
        protected Dictionary<PartResourceDefinition, string> colonyLimitStrings = new Dictionary<PartResourceDefinition, string>();
        protected Dictionary<PartResourceDefinition, string> vesselLimitStrings = new Dictionary<PartResourceDefinition, string>();
        public KCColonyTransferBehaviour.KCTransferInfo transfer => transferModule.transferInfo;
        Vector2 scrollPos = Vector2.zero;

        protected override void OnOpen()
        {
            scrollPos = Vector2.zero;
            rateStrings.Clear();
            colonyLimitStrings.Clear();
            vesselLimitStrings.Clear();

            List<PartResourceDefinition> allResources = new List<PartResourceDefinition>();

            foreach (PartResourceDefinition item in PartResourceLibrary.Instance.resourceDefinitions)
            {
                transferModule.part.GetConnectedResourceTotals(item.id, transferModule.transferMode, out double amount, out double max);

                if (max > 0)
                {
                    allResources.Add(item);
                    transfer.AddResource(item);
                    rateStrings.TryAdd(item, "0");
                    colonyLimitStrings.TryAdd(item, "0.5");
                    vesselLimitStrings.TryAdd(item, "0.5");
                }
            }
        }

        protected override void CustomWindow()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                rateStrings.ToList().ForEach(kvp =>
                {
                    GUILayout.BeginHorizontal();
                    {
                        rateStrings[kvp.Key] = GUILayout.TextField(rateStrings[kvp.Key], GUILayout.Width(100));
                        colonyLimitStrings[kvp.Key] = GUILayout.TextField(colonyLimitStrings[kvp.Key], GUILayout.Width(100));
                        vesselLimitStrings[kvp.Key] = GUILayout.TextField(vesselLimitStrings[kvp.Key], GUILayout.Width(100));

                        if (GUILayout.Button(kvp.Key.name))
                        {
                            if (double.TryParse(colonyLimitStrings[kvp.Key], out double colonyLimit))
                            {
                                Configuration.writeLog($"Changed colony limit of {kvp.Key.name} to {colonyLimit}");
                                colonyLimitStrings[kvp.Key] = colonyLimit.ToString();
                                transfer.ColonyTransferLimits[kvp.Key] = Math.Max(0, Math.Min(1, colonyLimit));
                            }

                            if (double.TryParse(vesselLimitStrings[kvp.Key], out double vesselLimit))
                            {
                                Configuration.writeLog($"Changed colony limit of {kvp.Key.name} to {vesselLimit}");
                                vesselLimitStrings[kvp.Key] = vesselLimit.ToString();
                                transfer.VesselTransferLimits[kvp.Key] = Math.Max(0, Math.Min(1, vesselLimit));
                            }

                            if (double.TryParse(rateStrings[kvp.Key], out double rate))
                            {
                                Configuration.writeLog($"Changed transfer rate of {kvp.Key.name} to {rate} units/second.");
                                rateStrings[kvp.Key] = rate.ToString();

                                if (rate >= 0)
                                {
                                    transfer.ToColonyResourcesTarget[kvp.Key] = rate;
                                    transfer.ToVesselResourcesTarget[kvp.Key] = 0;

                                    if (transfer.ColonyTransferLimits[kvp.Key] == 0.5)
                                    {
                                        transfer.ColonyTransferLimits[kvp.Key] = 0.8;
                                        colonyLimitStrings[kvp.Key] = "0.8";
                                    }
                                    if (transfer.VesselTransferLimits[kvp.Key] == 0.5)
                                    {
                                        transfer.VesselTransferLimits[kvp.Key] = 0.2;
                                        vesselLimitStrings[kvp.Key] = "0.2";
                                    }
                                }
                                else
                                {
                                    transfer.ToColonyResourcesTarget[kvp.Key] = 0;
                                    transfer.ToVesselResourcesTarget[kvp.Key] = -rate;

                                    if (transfer.ColonyTransferLimits[kvp.Key] == 0.5)
                                    {
                                        transfer.ColonyTransferLimits[kvp.Key] = 0.2;
                                        colonyLimitStrings[kvp.Key] = "0.2";
                                    }
                                    if (transfer.VesselTransferLimits[kvp.Key] == 0.5)
                                    {
                                        transfer.VesselTransferLimits[kvp.Key] = 0.8;
                                        vesselLimitStrings[kvp.Key] = "0.8";
                                    }
                                }
                            }
                            else
                            {
                                Configuration.writeLog($"ERROR: Could not parse {rateStrings[kvp.Key]} as a double.");
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                });
            }
            GUILayout.EndScrollView();
        }

        protected override void OnClose()
        {
            transfer.CleanResources();
        }

        public VesselResourceRatesChangewindow(ModuleKCTransfer transferModule) : base(Configuration.createWindowID(), "Change resource rates", false)
        {
            this.transferModule = transferModule;
            this.toolRect = new Rect(100, 100, 600, 250);
        }
    }
}
