using KerbalColonies.colonyFacilities.StorageFacility;
using KerbalColonies.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

namespace KerbalColonies.VesselAutoTransfer
{
    public class VesselColonyTransferchangewindow : KCWindowBase
    {
        public ModuleKCTransfer TransferModule = null;

        public Vessel vessel => TransferModule.vessel;
        public Vector3 vesselPos => TransferModule.vessel.transform.position;
        public string VesselPlanet => TransferModule.vessel.mainBody.name;

        Vector2 scrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            List<colonyClass> currentbodyColonies = Configuration.colonyDictionary[VesselPlanet];
            List<colonyClass> possibleTargets = currentbodyColonies.Where(c => KCUnifiedColonyStorage.colonyStorages[c].VesselInRange(vessel)).ToList();

            if (possibleTargets.Count == 0)
            {
                GUILayout.Label("No colonies in range.");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                possibleTargets.ForEach(c =>
                {
                    if (GUILayout.Button(c.DisplayName))
                    {
                        TransferModule.transferInfo = new KCTransferInfo(c, TransferModule.PersistentId, TransferModule.vessel.persistentId);
                        Configuration.writeLog($"Changed target colony of vessel {vessel.vesselName} to colony {c.Name}.");
                        this.Close();
                    }
                });
            }
            GUILayout.EndScrollView();
        }

        public VesselColonyTransferchangewindow(ModuleKCTransfer transferModule) : base(Configuration.createWindowID(), "Change target colony", false)
        {
            this.TransferModule = transferModule;

            this.toolRect = new Rect(100, 100, 300, 250);
        }
    }
}
