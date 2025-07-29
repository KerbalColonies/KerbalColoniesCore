using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies.colonyFacilities.CabFacility
{
    public class KC_CAB_Facility : KCFacilityBase
    {
        public bool PlayerInColony { get; private set; }

        private KC_CAB_Window window;

        private ConfigNode cabNode;

        public KC_CAB_Info cabInfo => (KC_CAB_Info)facilityInfo;

        private bool initialized = false;
        public override void Update()
        {
            if (!initialized)
            {
                initialized = true;
                if (cabNode != null && cabNode.HasNode("constructingFacilities"))
                {
                    foreach (ConfigNode facilityNode in cabNode.GetNode("constructingFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddConstructingFacility(facility, double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("constructedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddConstructedFacility(facility);
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("upgradingFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddUpgradingFacility(facility, double.Parse(facilityNode.GetValue("remainingTime")));
                    }
                    foreach (ConfigNode facilityNode in cabNode.GetNode("upgradedFacilities").GetNodes("facilityNode"))
                    {
                        KCFacilityBase facility = KCFacilityBase.GetFacilityByID(int.Parse(facilityNode.GetValue("facilityID")));
                        if (facility != null) KCProductionFacility.AddUpgradedFacility(facility);
                    }
                }
            }

            PlayerInColony = playerNearFacility();

            base.Update();
        }

        public void AddUpgradeableFacility(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier == 0)
            {
                switch (facility.facilityInfo.UpgradeTypes[facility.level + 1])
                {
                    case UpgradeType.withGroupChange:
                        KCFacilityBase.UpgradeFacilityWithGroupChange(facility);
                        break;
                    case UpgradeType.withoutGroupChange:
                        KCFacilityBase.UpgradeFacilityWithoutGroupChange(facility);
                        break;
                    case UpgradeType.withAdditionalGroup:
                        KCProductionFacility.AddUpgradedFacility(facility);
                        break;
                }
            }
            else
            {
                KCProductionFacility.AddUpgradingFacility(facility, facility.facilityInfo.UpgradeTimes[facility.level + 1] * Configuration.FacilityTimeMultiplier);
            }
        }

        public void AddconstructingFacility(KCFacilityBase facility)
        {
            if (facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier == 0)
            {
                KCProductionFacility.AddConstructedFacility(facility);
            }
            else
            {
                KCProductionFacility.AddConstructingFacility(facility, facility.facilityInfo.UpgradeTimes[0] * Configuration.FacilityTimeMultiplier);
            }
        }

        public override void OnBuildingClicked()
        {
            window.Toggle();
        }

        public override void OnRemoteClicked()
        {
            window.Toggle();
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();

            return node;
        }

        public KC_CAB_Facility(colonyClass colony, ConfigNode node) : base(Configuration.GetCABInfoClass(node.GetValue("name")), node)
        {
            this.Colony = colony;

            cabNode = node;

            window = new KC_CAB_Window(this);
        }

        public KC_CAB_Facility(colonyClass colony, KC_CAB_Info CABInfo) : base(CABInfo)
        {
            this.Colony = colony;

            initialized = true;

            window = new KC_CAB_Window(this);
        }
    }
}
