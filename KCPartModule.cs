// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

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

        [KSPEvent(name = "Activate", guiName = "Build colony", active = true, guiActive = true)]
        public void Activate()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel.srfSpeed >= 0.5f && !vessel.Landed)
            {
                ScreenMessages.PostScreenMessage("KC: The current vessel must be landed and have a surface speed slower than 0.5m/s", 10f, ScreenMessageStyle.UPPER_RIGHT);
                return;
            }

            int result = ColonyBuilding.CreateColony();
            switch (result)
            {
                case 0:
                    Configuration.writeLog($"Creating a Colony on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage($"KC: Creating a Colony on {part.vessel.mainBody.name}", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 1:
                    Configuration.writeLog($"Not enough resources to create a colony on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage("KC: Not enough resources", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 2:
                    Configuration.writeLog($"Unable to create a colony because there are too many colonies on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage("KC: Too many colonies on this celestial body.", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 3:
                    Configuration.writeLog($"Unable to create a colony one {part.vessel.mainBody.name} because the cab selector is open");
                    ScreenMessages.PostScreenMessage("KC: cab selector is open", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                default:
                    Configuration.writeLog($"Unknown error in ColonyBuilding.CreateColony(), no colony was built on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage("KC: Unknown error", 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
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
