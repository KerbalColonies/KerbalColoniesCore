using KerbalColonies.colonyFacilities;
using KerbalColonies.colonyFacilities.ElectricityFacilities.ECStorage;
using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.VesselAutoTransfer
{
    public class VesselColonyTransferchangewindow : KCWindowBase
    {
        public ModuleKCTransfer TransferModule = null;

        public Vessel vessel => TransferModule.vessel;
        public Vector3 vesselPos => TransferModule.vessel.transform.position;
        public int VesselPlanetID => TransferModule.vessel.mainBody.flightGlobalsIndex;

        Vector2 scrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            List<colonyClass> currentbodyColonies = Configuration.colonyDictionary[VesselPlanetID];
            List<colonyClass> possibleTargets = currentbodyColonies.Where(c => KCFacilityBase.GetAllTInColony<KCECStorageFacility>(c).Any(f => f.CanTransferToVessel(vessel))).ToList();

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
                        TransferModule.transferInfo = new KCColonyTransferBehaviour.KCTransferInfo(c, TransferModule.PersistentId, TransferModule.vessel.persistentId);
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
