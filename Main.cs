using KerbalColonies.colonyFacilities;
using KerbalColonies.Serialization;
using KerbalKonstructs.UI;
using SaveUpgradePipeline;
using System.Collections.Generic;
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
    [KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
    public class GameSateLoad : MonoBehaviour
    {
        public void GetGameNode(ConfigNode node)
        {
            Configuration.gameNode = node;
        }

        protected void Awake()
        {
            GameEvents.onGameStateLoad.Add(GetGameNode);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalColonies : MonoBehaviour
    {
        protected void Awake()
        {
            KCFacilityTypeRegistry.RegisterType<KCStorageFacility>();
            Configuration.LoadConfiguration(Configuration.APP_NAME.ToUpper());
            KerbalKonstructs.API.RegisterOnBuildingClicked(KCFacilityBase.OnBuildingClickedHandler);
        }

        private string uuid;

        protected void Start()
        {
            Configuration.LoadColonies("KCCD");
        }

        public void FixedUpdate()
        {
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                writeDebug(Planetarium.GetUniversalTime().ToString());
                KCUI.instance.Toggle();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                writeDebug(Configuration.gameNode.ToString());
                writeDebug(Configuration.gameNode.name);
                writeDebug(Configuration.coloniesPerBody.ContainsKey(Configuration.gameNode.name).ToString());
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                KCStorageFacility facTest = new KCStorageFacility(true, 100);
                writeDebug(facTest.ToString());
                string serialString = KCFacilityClassConverter.SerializeObject(facTest);
                writeDebug(serialString);
                KCFacilityBase facTest2 = KCFacilityClassConverter.DeserializeObject(serialString);
                writeDebug(facTest2.ToString());
                writeDebug(facTest2.GetType().ToString());
            }
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
