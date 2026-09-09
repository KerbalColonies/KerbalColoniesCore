using KerbalColonies.Settings;
using KerbalColonies.UI;
using KSP.Localization;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar and the KC Team

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/

namespace KerbalColonies
{
    public class KCPartModule : PartModule
    {
        [KSPField]
        public bool IsActivate = false;

        [KSPEvent(name = "Activate", guiName = "#LOC_KC_KCPARTMODULE_BUILD_COLONY", active = true, guiActive = true)]
        public void Activate()
        {
            if (KCLegacySaveWarning.LoadedSaves.ContainsKey(HighLogic.CurrentGame.Seed.ToString()))
            {
                Configuration.writeLog("A legacy save was detected, no colony was created!");
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel.srfSpeed >= 0.5f && !vessel.Landed)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_KCPARTMODULE_MUST_BE_LANDED"), 10f, ScreenMessageStyle.UPPER_RIGHT);
                return;
            }

            int result = ColonyBuilding.CreateColony();
            switch (result)
            {
                case 0:
                    Configuration.writeLog($"Creating a Colony on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_KCPARTMODULE_CREATING_COLONY", part.vessel.mainBody.name), 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 1:
                    Configuration.writeLog($"Not enough resources to create a colony on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_KCPARTMODULE_NOT_ENOUGH_RESOURCES"), 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 2:
                    Configuration.writeLog($"Unable to create a colony because there are too many colonies on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_KCPARTMODULE_TOO_MANY_COLONIES"), 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                case 3:
                    Configuration.writeLog($"Unable to create a colony one {part.vessel.mainBody.name} because the cab selector is open");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_KCPARTMODULE_CAB_SELECTOR_OPEN"), 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
                default:
                    Configuration.writeLog($"Unknown error in ColonyBuilding.CreateColony(), no colony was built on {part.vessel.mainBody.name}");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KC_KCPARTMODULE_UNKNOWN_ERROR"), 10f, ScreenMessageStyle.UPPER_RIGHT);
                    break;
            }
        }

        [KSPAction("Toggle", KSPActionGroup.None, guiName = "#LOC_KC_KCPARTMODULE_CREATE_COLONY")]
        public void ActionActivate(KSPActionParam param)
        {
            Activate();
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KC_KCPARTMODULE_INFO");
        }

        public override void OnStart(StartState state)
        {

        }
    }
}
