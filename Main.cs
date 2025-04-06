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
            Configuration.RegisterBuildableFacility(typeof(KCStorageFacility), new KCStorageFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCCrewQuarters), new KCCrewQuarterCost());
            Configuration.RegisterBuildableFacility(typeof(KCResearchFacility), new KCResearchFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCMiningFacility), new KCMiningFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCProductionFacility), new KCProductionFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCResourceConverterFacility), new KCResourceConverterFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCHangarFacility), new KCHangarFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCLaunchpadFacility), new KCLaunchPadCost());
            Configuration.RegisterBuildableFacility(typeof(KCCommNetFacility), new KCCommNetCost());

            KC_CAB_Facility.addPriorityDefaultFacility(typeof(KCLaunchpadFacility), 1);
            KC_CAB_Facility.addDefaultFacility(typeof(KCStorageFacility), 1);
            KC_CAB_Facility.addDefaultFacility(typeof(KCCrewQuarters), 1);
            KC_CAB_Facility.addDefaultFacility(typeof(KCProductionFacility), 1);

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
                    if (ColonyBuilding.buildQueue.Count() > 0)
                    {
                        KerbalKonstructs.API.CreateGroup(ColonyBuilding.buildQueue.Peek().groupName);
                        KerbalKonstructs.API.CopyGroup(ColonyBuilding.buildQueue.Peek().groupName, ColonyBuilding.buildQueue.Peek().fromGroupName, fromBodyName: "Kerbin");
                        KerbalKonstructs.API.GetGroupStatics(ColonyBuilding.buildQueue.Peek().groupName).ForEach(instance => instance.ToggleAllColliders(false));
                        KerbalKonstructs.API.OpenGroupEditor(ColonyBuilding.buildQueue.Peek().groupName);
                        KerbalKonstructs.API.RegisterOnGroupSaved(ColonyBuilding.PlaceNewGroupSave);
                        ColonyBuilding.buildQueue.Peek().Facility.KKgroups.Add(ColonyBuilding.buildQueue.Peek().groupName); // add the group to the facility groups
                    }
                    ColonyBuilding.placedGroup = false;
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
                        .ToDictionary(x => x.Key, x => x.Value)
                        .SelectMany(x => x.Value.Values.SelectMany(y => y)).ToList()
                        .ForEach(x => KerbalKonstructs.API.DeactivateStatic(x));

                    Configuration.KCgroups.Where(x => x.Key == HighLogic.CurrentGame.Seed.ToString())
                        .ToDictionary(x => x.Key, x => x.Value)
                        .SelectMany(x => x.Value.Values.SelectMany(y => y)).ToList()
                        .ForEach(x => KerbalKonstructs.API.ActivateStatic(x));

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
                .ForEach(x => KerbalKonstructs.API.ActivateStatic(x));

            KerbalKonstructs.API.UnRegisterOnStaticClicked(KCFacilityBase.OnBuildingClickedHandler);
        }
    }
}
