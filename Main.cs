using KerbalColonies.colonyFacilities;
using KerbalColonies.Serialization;
using UnityEngine;

// KC: Kerbal Colonies
// This mod aimes to create a colony system with Kerbal Konstructs statics
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
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalColonies : MonoBehaviour
    {
        protected void Awake()
        {
            KSPLog.print("KC awake");
            Configuration.LoadConfiguration(Configuration.APP_NAME.ToUpper());
            KCFacilityTypeRegistry.RegisterType<KCStorageFacility>();
            KCFacilityTypeRegistry.RegisterType<KCCrewQuarters>();
            KCFacilityTypeRegistry.RegisterType<KCResearchFacility>();
            KCFacilityTypeRegistry.RegisterType<KC_CAB_Facility>();
            Configuration.RegisterBuildableFacility(typeof(KCStorageFacility), new KCStorageFacilityCost());
            Configuration.RegisterBuildableFacility(typeof(KCCrewQuarters), new KCCrewQuarterCost());
            Configuration.RegisterBuildableFacility(typeof(KCResearchFacility), new KCResearchFacilityCost());
            KerbalKonstructs.API.RegisterOnBuildingClicked(KCFacilityBase.OnBuildingClickedHandler);
        }

        protected void Start()
        {
            KSPLog.print("KC start");
            Configuration.coloniesPerBody.Clear();
            Configuration.LoadColonies("KCCD");

            foreach (PartResourceDefinition resource in PartResourceLibrary.Instance.resourceDefinitions)
            {
                Configuration.writeDebug($"{resource.displayName}: {resource.name}, {resource.id}");
            }

            foreach (string saveGame in Configuration.coloniesPerBody.Keys)
            {
                if (saveGame == HighLogic.CurrentGame.Seed.ToString()) { continue; }

                foreach (int bodyIndex in Configuration.coloniesPerBody[saveGame].Keys)
                {
                    foreach (string colonyName in Configuration.coloniesPerBody[saveGame][bodyIndex].Keys)
                    {
                        foreach (GroupPlaceHolder gph in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName].Keys)
                        {
                            foreach (string UUID in Configuration.coloniesPerBody[saveGame][bodyIndex][colonyName][gph].Keys)
                            {
                                KerbalKonstructs.API.DeactivateStatic(UUID);
                            }
                        }
                    }
                }
            }
        }

        public void FixedUpdate()
        {
            //HighLogic.CurrentGame.CrewRoster;

            if (Input.GetKeyDown(KeyCode.U))
            {
                writeDebug(Planetarium.GetUniversalTime().ToString());
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                writeDebug(HighLogic.CurrentGame.Seed.ToString());
                writeDebug(Configuration.coloniesPerBody.ContainsKey(HighLogic.CurrentGame.Seed.ToString()).ToString());
                writeDebug(Configuration.coloniesPerBody.Count.ToString());
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                KCStorageFacility facTest = new KCStorageFacility(true, maxVolume: 100);
                writeDebug(facTest.ToString());
                facTest.EncodeString();
                writeDebug(facTest.facilityData);
                //string serialString = KCFacilityClassConverter.SerializeObject(facTest);
                //writeDebug(serialString);
                //KCFacilityBase facTest2 = KCFacilityClassConverter.DeserializeObject(serialString);
                //writeDebug(facTest2.ToString());
                //writeDebug(facTest2.GetType().ToString());
            }
        }
        protected void OnDestroy()
        {
            KerbalKonstructs.API.UnRegisterOnBuildingClicked(KCFacilityBase.OnBuildingClickedHandler);
            Configuration.coloniesPerBody.Clear();
        }

        internal void writeDebug(string text)
        {
            if (Configuration.enableLogging)
            {
                writeLog(text);
            }
        }

        internal void writeLog(string text)
        {
            KSPLog.print(Configuration.APP_NAME + ": " + text);
        }
    }
}
