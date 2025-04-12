using KerbalColonies.colonyFacilities;
using KerbalColonies;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CommNet.Network;
using KerbalKonstructs.Modules;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (C) 2024 AMPW, Halengar

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

namespace KerbalColonies
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class KerbalColonies : MonoBehaviour
    {
        double lastTime = 0;
        bool despawned = false;
        int waitCounter = 0;

        protected void Awake()
        {
            KSPLog.print("KC awake");
            Configuration.LoadConfiguration(Configuration.APP_NAME.ToUpper());
            KCFacilityTypeRegistry.RegisterType<KCStorageFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCrewQuarters>();
            KCFacilityTypeRegistry.RegisterType<KCResearchFacility>();
            KCFacilityTypeRegistry.RegisterType<KC_CAB_Facility>();
            KCFacilityTypeRegistry.RegisterType<KCMiningFacility>();
            KCFacilityTypeRegistry.RegisterType<KCProductionFacility>();
            KCFacilityTypeRegistry.RegisterType<KCResourceConverterFacility>();
            KCFacilityTypeRegistry.RegisterType<KCHangarFacility>();
            KCFacilityTypeRegistry.RegisterType<KCLaunchpadFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCommNetFacility>();

            KC_CAB_Facility.addPriorityDefaultFacility("launchpadFacility", 1);
            KC_CAB_Facility.addDefaultFacility("storageFacility", 1);
            KC_CAB_Facility.addDefaultFacility("crewQuarters", 1);
            KC_CAB_Facility.addDefaultFacility("productionFacility", 1);

            KerbalKonstructs.API.RegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
        }

        protected void Start()
        {
            KSPLog.print("KC start");
        }

        public void FixedUpdate()
        {
            if (Planetarium.GetUniversalTime() - lastTime >= 10)
            {
                lastTime = Planetarium.GetUniversalTime();
                Configuration.colonyDictionary.Values.SelectMany(x => x).ToList().ForEach(x => x.UpdateColony());
            }

            if (ColonyBuilding.placedGroup)
            {
                if (!ColonyBuilding.nextFrame)
                {
                    ColonyBuilding.nextFrame = true;
                }
                else
                {
                    ColonyBuilding.QueuePlacer();
                }
            }

            if (waitCounter < 10)
            {
                waitCounter++;
                return;
            }
            else
            {
                if (!despawned)
                {
                    Configuration.KCgroups.Where(x => x.Key != HighLogic.CurrentGame.Seed.ToString())
                        .ToDictionary(x => x.Key, x => x.Value).ToList()
                        .ForEach(kvp =>
                        kvp.Value.ToList().ForEach(bodyKVP =>
                        bodyKVP.Value.ForEach(KKgroup =>
                        KerbalKonstructs.API.GetGroupStatics(KKgroup).ForEach(s =>
                        KerbalKonstructs.API.DeactivateStatic(s.UUID)))));

                    Configuration.KCgroups.Where(x => x.Key == HighLogic.CurrentGame.Seed.ToString())
                        .ToDictionary(x => x.Key, x => x.Value).ToList()
                        .ForEach(kvp =>
                        kvp.Value.ToList().ForEach(bodyKVP =>
                        bodyKVP.Value.ForEach(KKgroup =>
                        KerbalKonstructs.API.GetGroupStatics(KKgroup).ForEach(s =>
                        KerbalKonstructs.API.ActivateStatic(s.UUID)))));

                    despawned = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                KCResourceConverterFacility.resourceTypes.ToString();
            }
        }

        public void LateUpdate()
        {
        }

        protected void OnDestroy()
        {
            Configuration.KCgroups.SelectMany(x => x.Value.Values.SelectMany(y => y)).ToList()
                .ForEach(x => KerbalKonstructs.API.GetGroupStatics(x).ForEach(uuid => KerbalKonstructs.API.ActivateStatic(uuid.UUID)));

            KerbalKonstructs.API.UnRegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
        }
    }
}
