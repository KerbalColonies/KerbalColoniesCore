using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalColonies.colonyFacilities
{
    internal class KC_CAB_Window : KCWindowBase
    {
        private KC_CAB_Facility facility;

        protected override void CustomWindow()
        {
            GUILayout.BeginScrollView(new Vector2());
            {
                GUILayout.BeginVertical();
                {
                    foreach (Type t in Configuration.BuildableFacilities.Keys)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(t.Name);
                        GUILayout.BeginVertical();
                        {
                            for (int i = 0; i < Configuration.BuildableFacilities[t].resourceCost.Count; i++)
                            {
                                GUILayout.Label($"{Configuration.BuildableFacilities[t].resourceCost.ElementAt(i).Key.displayName}: {Configuration.BuildableFacilities[t].resourceCost.ElementAt(i).Value}");
                            }
                        }
                        GUILayout.EndVertical();

                        if (!Configuration.BuildableFacilities[t].VesselHasRessources(FlightGlobals.ActiveVessel)) { GUI.enabled = false; }
                        if (GUILayout.Button("Build"))
                        {
                            Configuration.BuildableFacilities[t].RemoveVesselRessources(FlightGlobals.ActiveVessel);
                            KCFacilityBase KCFac = Configuration.CreateInstance(t, true, "");

                            KCFacilityBase.GetInformationByUUID(KCFacilityBase.GetUUIDbyFacility(facility), out string saveGame, out int bodyIndex, out string colonyName, out GroupPlaceHolder gph, out List<KCFacilityBase> facilities);

                            string groupName = $"{colonyName}_{t.Name}";

                            KerbalKonstructs.API.CreateGroup(groupName);
                            Colonies.EditorGroupPlace(t, "KC_CAB", groupName, colonyName);
                        }
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }


        public KC_CAB_Window(KC_CAB_Facility facility) : base(Configuration.createWindowID(facility))
        {
            this.facility = facility;
        }
    }

    [System.Serializable]
    internal class KC_CAB_Facility : KCFacilityBase
    {
        private KC_CAB_Window window;

        internal override void OnBuildingClicked()
        {
            window.Toggle();
        }

        internal override void Initialize(string facilityName, int id, string facilityData, bool enabled)
        {
            base.Initialize(facilityName, id, facilityData, enabled);
            window = new KC_CAB_Window(this);
            enabled = true;
        }

        public KC_CAB_Facility() : base("KCCABFacility", true, "")
        {

        }
    }
}
