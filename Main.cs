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

// KC: Kerbal Colonies
// This mod aimes to create a colony system with Kerbal Konstructs statics
//Copyright (C) 2024 AMPW, Halengar

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

namespace KerbalColonies
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalColonies : MonoBehaviour
    {
        private string uuid;

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

                    uuid = KerbalKonstructs.API.PlaceStatic("LandingZoneSmall", FlightGlobals.currentMainBody.name, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, vessel.GetHeightFromSurface() - 2, 0f);
                    vessel.SetPosition(new Vector3d(vessel.latitude, vessel.longitude, vessel.GetHeightFromTerrain() + 2));
                    KerbalKonstructs.API.HighLightStatic(uuid, Color.yellow);
                }
            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                writeDebug(KerbalKonstructs.API.CreateGroup("KKK-Group").ToString());
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                writeDebug(KerbalKonstructs.API.RemoveGroup("KKK-Group").ToString());
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                writeDebug(KerbalKonstructs.API.CopyGroup("KKK-Group", "KKK-Group-2").ToString());
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                writeDebug(KerbalKonstructs.API.AddStaticToGroup(uuid, "KKK-Group").ToString());
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                KerbalKonstructs.API.Save();
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
