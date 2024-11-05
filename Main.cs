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
        private string uuid;

        protected void Start()
        {
            writeDebug("Starting KC");
        }

        /// <summary>
        /// </summary>
        public void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                string uuid = KerbalKonstructs.API.PlaceStatic("LandingZoneSmall", FlightGlobals.currentMainBody.name, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, (float) FlightGlobals.ship_altitude - 2, 0f);
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                string uuid = KerbalKonstructs.API.PlaceStatic("Tier4VerticalAssemblyBuilding", FlightGlobals.currentMainBody.name, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, (float)FlightGlobals.ship_altitude - 2, 0f, variant: "Tier4VerticalAssemblyBuilding_Default");
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                string uuid = KerbalKonstructs.API.PlaceStatic("Tier4VerticalAssemblyBuilding", FlightGlobals.currentMainBody.name, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, (float)FlightGlobals.ship_altitude - 2, 0f, variant: "Tier4VerticalAssemblyBuilding_NoHexBase");
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
