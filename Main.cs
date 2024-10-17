using Smooth.Compare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KerbalKonstructs;
using LibNoise.Models;
using Smooth.Algebraics;
using static FinePrint.ContractDefs;
using Expansions.Missions.Tests;
using KerbalKonstructs.Core;
using LibNoise;
using static EdyCommonTools.RotationController;

// KKK: Kerbal Konstructs Kolonization
// This mod aimes to create a colony system with Kerbal Konstructs statics
//Copyright (C) 2024 AMPW, lolsmcfee

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <https://www.gnu.org/licenses/

namespace KerbalKonstructsKolonization
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalKonstructsKolonization : MonoBehaviour
    {
        const string APP_NAME = "KerbalKonstructsKolonization";

        protected void Start()
        {
            writeDebug("Starting KKK");
        }

        /// <summary>
        /// </summary>
        public void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                writeDebug("Surface:" + vessel.GetHeightFromSurface().ToString());
                writeDebug("Terrain:" + vessel.GetHeightFromTerrain().ToString()); 
                if (vessel.srfSpeed <= 0.5f && vessel.Landed)
                {

                    string uuid = KerbalKonstructs.API.PlaceStatic("LandingZoneSmall", FlightGlobals.currentMainBody.name, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, vessel.GetHeightFromSurface() - 2, 0f);
                    vessel.SetPosition(new Vector3d(vessel.latitude, vessel.longitude, vessel.GetHeightFromTerrain() + 2));
                    KerbalKonstructs.API.HighLightStatic(uuid, Color.yellow);
                }
            }
        }

        void writeDebug(string text)
        {
            if (Configuration.enableLogging)
            {
                writeLog(text);
            }
        }

        void writeLog(string text)
        {
            KSPLog.print(APP_NAME + ": " + text);
        }
    }
}
