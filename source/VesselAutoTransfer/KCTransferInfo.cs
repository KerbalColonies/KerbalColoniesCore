using KerbalColonies.ResourceManagment;
using System.Collections.Generic;
using System.Linq;
using static KerbalColonies.VesselAutoTransfer.KCColonyTransferBehaviour;

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

namespace KerbalColonies.VesselAutoTransfer
{
    public class KCTransferInfo
    {
        private colonyClass colony;

        public colonyClass Colony => colony;

        public List<PartResourceDefinition> resources = new List<PartResourceDefinition>();

        public Dictionary<PartResourceDefinition, double> ResourcesTarget = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> ResourcesActual = new Dictionary<PartResourceDefinition, double>();

        public Dictionary<PartResourceDefinition, double> ColonyTransferLimits = new Dictionary<PartResourceDefinition, double>();
        public Dictionary<PartResourceDefinition, double> VesselTransferLimits = new Dictionary<PartResourceDefinition, double>();

        public Dictionary<PartResourceDefinition, bool> DisableIfVesselConstrains = new Dictionary<PartResourceDefinition, bool>();
        public Dictionary<PartResourceDefinition, bool> DisableIfColonyConstrains = new Dictionary<PartResourceDefinition, bool>();

        public Dictionary<PartResourceDefinition, bool> VesselConstrained = new Dictionary<PartResourceDefinition, bool>();
        public Dictionary<PartResourceDefinition, bool> ColonyConstrained = new Dictionary<PartResourceDefinition, bool>();

        public KCColonyResourceTransferHandler ResourceTransferHandler = null;

        public bool backgroundVessel;
        public uint partModuleID;
        public uint vesselID;

        public double Efficiency { get; set; } = 1.0;

        public void AddResource(PartResourceDefinition resource)
        {
            if (!resources.Contains(resource))
            {
                resources.Add(resource);

                ResourcesTarget[resource] = 0;
                ResourcesActual[resource] = 0;
                ColonyTransferLimits[resource] = 1;
                VesselTransferLimits[resource] = 1;
                DisableIfVesselConstrains[resource] = false;
                DisableIfColonyConstrains[resource] = false;
                VesselConstrained[resource] = false;
                ColonyConstrained[resource] = false;
            }
        }

        public void CleanResources()
        {
            PartResourceDefinition EC = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

            resources.ToList().ForEach(r =>
            {
                if (ResourcesTarget[r] == 0)
                {
                    resources.Remove(r);
                    ResourcesTarget.Remove(r);
                    ResourcesActual.Remove(r);
                    ColonyTransferLimits.Remove(r);
                    VesselTransferLimits.Remove(r);
                    DisableIfVesselConstrains.Remove(r);
                    DisableIfColonyConstrains.Remove(r);
                    VesselConstrained.Remove(r);
                    ColonyConstrained.Remove(r);
                }
            });

            Efficiency = 1;
        }

        public void Delete()
        {
            ActiveTransfers.Remove(this.partModuleID);
            if (ResourceTransferHandler != null)
            {
                KCResourceManager.otherProducers[colony].Remove(ResourceTransferHandler);
                KCResourceManager.otherConsumers[colony][0].Remove(ResourceTransferHandler);
                ResourceTransferHandler = null;
            }
        }

        public ConfigNode Save()
        {
            CleanResources();

            ConfigNode node = new ConfigNode("KCTransferInfo");

            node.AddValue("colonyID", colony.uniqueID);
            node.AddValue("partModuleID", partModuleID);
            node.AddValue("vesselID", vesselID);

            node.AddValue("efficiency", Efficiency);

            ConfigNode resourceNode = new ConfigNode("TransferResources");

            resources.ForEach(r =>
            {
                string value = $"{ResourcesTarget[r]},{ResourcesActual[r]},{ColonyTransferLimits[r]},{VesselTransferLimits[r]},{DisableIfVesselConstrains[r]},{DisableIfColonyConstrains[r]},{ColonyConstrained[r]},{VesselConstrained[r]}";
                resourceNode.AddValue(r.name, value);
            });

            node.AddNode(resourceNode);

            return node;
        }

        public static KCTransferInfo Load(ConfigNode node)
        {
            node = node.GetNode("KCTransferInfo");

            if (node == null) return null;

            colonyClass colony = Configuration.GetColonyByID(int.Parse(node.GetValue("colonyID")));
            uint partModuleID = uint.Parse(node.GetValue("partModuleID"));
            uint vesselID = uint.Parse(node.GetValue("vesselID"));

            KCTransferInfo info = null;
            if (ActiveTransfers.ContainsKey(partModuleID))
            {
                info = ActiveTransfers[partModuleID];
            }
            else
            {
                info = new KCTransferInfo(colony, partModuleID, vesselID);
            }

            info.Efficiency = double.Parse(node.GetValue("efficiency"));

            ConfigNode resourceNode = node.GetNode("TransferResources");
            foreach (ConfigNode.Value v in resourceNode.values)
            {
                PartResourceDefinition resource = PartResourceLibrary.Instance.GetDefinition(v.name);
                info.AddResource(resource);
                string[] values = v.value.Split(',');
                info.ResourcesTarget[resource] = double.Parse(values[0]);
                info.ResourcesActual[resource] = double.Parse(values[1]);
                info.ColonyTransferLimits[resource] = double.Parse(values[2]);
                info.VesselTransferLimits[resource] = double.Parse(values[3]);
                info.DisableIfVesselConstrains[resource] = bool.Parse(values[4]);
                info.DisableIfColonyConstrains[resource] = bool.Parse(values[5]);
                info.ColonyConstrained[resource] = bool.Parse(values[6]);
                info.VesselConstrained[resource] = bool.Parse(values[7]);
            }

            return info;
        }

        public KCTransferInfo(colonyClass colony, uint partModuleID, uint vesselID)
        {
            this.colony = colony;
            this.partModuleID = partModuleID;
            this.vesselID = vesselID;

            if (ActiveTransfers.ContainsKey(partModuleID)) ResourceTransferHandler = ActiveTransfers[partModuleID].ResourceTransferHandler;
            else
            {
                ResourceTransferHandler = new KCColonyResourceTransferHandler(colony, partModuleID);
                ActiveTransfers.Add(partModuleID, this);
            }

            Configuration.writeDebug($"Created a new Transfer producer. Current count: {KCResourceManager.otherProducers[colony].Count}");
        }
    }

}
