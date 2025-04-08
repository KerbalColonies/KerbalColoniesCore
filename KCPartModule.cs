using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using UniLinq;
using static UnityEngine.GraphicsBuffer;

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
    public class KCPartModule : PartModule
    {
        [KSPField]
        public bool IsActivate = false;

        [KSPEvent(name = "Activate", guiName = "Activate", active = true, guiActive = true)]
        public void Activate()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel.srfSpeed >= 0.5f && !vessel.Landed)
            {
                ScreenMessages.PostScreenMessage("The current vessel must be landed and have a surface speed slower than 0.5m/s", 10f, ScreenMessageStyle.UPPER_RIGHT);
                return;
            }

            PartResourceDefinition oreResource = PartResourceLibrary.Instance.GetDefinition("Ore");

            part.vessel.GetConnectedResourceTotals(oreResource.id, false, out double amount, out double maxAmount);
            if (amount >= Configuration.oreRequiredPerColony)
            {
                if (ColonyBuilding.CreateColony())
                {
                    part.RequestResource("Ore", (double)Configuration.oreRequiredPerColony);
                    writeLog("Creating Colony");
                    ScreenMessages.PostScreenMessage($"Creating a Colony on {part.vessel.mainBody.name}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    //FlightGlobals.fetch.SetVesselPosition(FlightGlobals.GetBodyIndex(FlightGlobals.currentMainBody), FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, FlightGlobals.ship_altitude + Configuration.spawnHeight, FlightGlobals.ActiveVessel.ReferenceTransform.eulerAngles, false, easeToSurface: true, 0.01);
                    //FloatingOrigin.ResetTerrainShaderOffset();
                }
            }
            else
            {
                ScreenMessages.PostScreenMessage($"Not enough ore: {amount}/{Configuration.oreRequiredPerColony}", 10f, ScreenMessageStyle.UPPER_RIGHT);
            }
        }

        [KSPAction("Toggle", KSPActionGroup.None, guiName = "Create Colony")]
        public void ActionActivate(KSPActionParam param)
        {
            Activate();
        }

        public override string GetInfo()
        {
            return "The core part of KC, with this part you can start a Colony if you have the requiered ressources.";
        }

        public override void OnStart(StartState state)
        {

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
