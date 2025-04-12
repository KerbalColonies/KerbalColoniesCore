using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

/// This file contains parts from the Kerbal Konstructs mod Hangar.cs file which is licensed under the MIT License.
/// The general idea on how to store vessels is also taken from the Kerbal Konstructs mod

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.colonyFacilities
{
    public class KCHangarFacilityWindow : KCWindowBase
    {
        KCHangarFacility hangar;
        private Vector2 scrollPos;

        protected override void CustomWindow()
        {
            List<StoredVessel> vesselList = hangar.storedVessels.ToList();
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            vesselList.ForEach(vessel =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(vessel.vesselName);

                if (GUILayout.Button("Load"))
                {
                    Vessel v = hangar.RollOutVessel(vessel).vesselRef;
                }
                GUILayout.EndHorizontal();
            });
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (FlightGlobals.ActiveVessel != null)
            {
                GUILayout.Space(10);
                if (hangar.CanStoreVessel(FlightGlobals.ActiveVessel))
                {
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = false;
                }

                if (GUILayout.Button("Store vessel"))
                {
                    hangar.StoreVessel(FlightGlobals.ActiveVessel);
                }
            }

            GUI.enabled = true;
        }

        public KCHangarFacilityWindow(KCHangarFacility hangar) : base(Configuration.createWindowID(hangar), "Hangar")
        {
            this.hangar = hangar;
            toolRect = new Rect(100, 100, 400, 800);
        }
    }

    public struct StoredVessel
    {
        internal string vesselName;
        internal Guid uuid;
        internal ConfigNode vesselNode;

        internal double vesselVolume;
    }

    public class KCHangarFacility : KCFacilityBase
    {
        KCHangarFacilityWindow hangarWindow;

        public double x;
        public double y;
        public double z;
        public double Volume { get { return x * y * z; } }

        public int vesselCapacity;

        internal List<StoredVessel> storedVessels = new List<StoredVessel>();

        public double getStoredVolume()
        {
            double volume = 0;

            foreach (StoredVessel vessel in storedVessels)
            {
                volume += vessel.vesselVolume;
            }

            return volume;
        }

        // TODO: make it so the vessel oriantation doesn't matter, e.g. if the hangar has dimensions of 10, 5, 5 and a vessel with 4, 8, 4 (x, y, z in meters) it should work
        public bool CanStoreVessel(Vessel vessel)
        {
            if (vesselCapacity <= storedVessels.Count)
            {
                return false;
            }

            Vector3 vesselSize = vessel.vesselSize;
            if (vesselSize.x > x || vesselSize.y > y || vesselSize.z > z)
            {
                return false;
            }

            double vesselVolume = (vesselSize.x * vesselSize.y * vesselSize.z) * 0.8;
            if (vesselVolume > Volume - getStoredVolume())
            {
                return false;
            }

            if (KCCrewQuarters.ColonyKerbalCapacity(Colony) - KCCrewQuarters.GetAllKerbalsInColony(Colony).Count < vessel.GetCrewCount())
            {
                return false;
            }

            return true;
        }

        public bool StoreVessel(Vessel vessel)
        {
            if (CanStoreVessel(vessel))
            {
                Vector3 vesselSize = vessel.vesselSize;

                StoredVessel storedVessel = new StoredVessel
                {
                    uuid = vessel.protoVessel.vesselID,
                    vesselName = vessel.GetDisplayName(),

                    vesselVolume = (vesselSize.x * vesselSize.y * vesselSize.z) * 0.8
                };

                //get the experience and assign the crew to the rooster
                foreach (Part part in vessel.parts)
                {
                    int count = part.protoModuleCrew.Count;

                    if (count != 0)
                    {
                        ProtoCrewMember[] crewList = part.protoModuleCrew.ToArray();

                        for (int i = 0; i < count; i++)
                        {
                            crewList[i].flightLog.AddEntryUnique(FlightLog.EntryType.Recover);
                            crewList[i].flightLog.AddEntryUnique(FlightLog.EntryType.Land, FlightGlobals.currentMainBody.name);
                            crewList[i].ArchiveFlightLog();

                            // remove the crew from the ship
                            part.RemoveCrewmember(crewList[i]);
                            KCCrewQuarters.AddKerbalToColony(Colony, crewList[i]);
                        }
                    }
                }

                // save the ship
                storedVessel.vesselNode = new ConfigNode("VESSEL");

                //create a backup of the current state, then save that state
                ProtoVessel backup = vessel.BackupVessel();
                backup.Save(storedVessel.vesselNode);

                // save the stored information in the hangar
                storedVessels.Add(storedVessel);

                // remove the stored vessel from the game
                vessel.MakeInactive();
                vessel.Unload();

                FlightGlobals.RemoveVessel(vessel);
                if (vessel != null)
                {
                    vessel.protoVessel.Clean();
                }

                List<KCLaunchpadFacility> facilities = KCLaunchpadFacility.getLaunchPadsInColony(Colony);

                // TODO: the set space center cam doesn't work
                if (facilities.Count > 0)
                {
                    KerbalKonstructs.Core.CameraController.SetSpaceCenterCam(KerbalKonstructs.API.getStaticInstanceByUUID(facilities[0].launchSiteUUID).launchSite);
                }
                KerbalKonstructs.KerbalKonstructs.instance.UpdateCache();

                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                HighLogic.LoadScene(GameScenes.SPACECENTER);

                return true;
            }

            return false;
        }


        public ProtoVessel RollOutVessel(StoredVessel storedVessel)
        {
            if (!storedVessels.Contains(storedVessel))
            {
                Configuration.writeLog("no Stored Vessel found:" + storedVessel.vesselName);
                return null;
            }

            ProtoVessel protoVessel = new ProtoVessel(storedVessel.vesselNode, HighLogic.CurrentGame);
            protoVessel.Load(HighLogic.CurrentGame.flightState);


            List<KCLaunchpadFacility> launchpads = KCLaunchpadFacility.getLaunchPadsInColony(Colony);
            if (launchpads.Count > 0)
            {
                storedVessels.Remove(storedVessel);
                launchpads[0].LaunchVessel(protoVessel);
            }

            return protoVessel;
        }

        public override ConfigNode getConfigNode()
        {
            ConfigNode node = base.getConfigNode();
            node.AddValue("x", x);
            node.AddValue("y", y);
            node.AddValue("z", z);
            node.AddValue("capacity", vesselCapacity);

            foreach (StoredVessel vessel in storedVessels)
            {
                ConfigNode vesselNode = new ConfigNode("vessel");

                vesselNode.SetValue("VesselID", vessel.uuid.ToString(), true);
                vesselNode.SetValue("VesselName", vessel.vesselName, true);
                vesselNode.AddNode(vessel.vesselNode);

                node.AddNode(vesselNode);
            }

            return node;
        }

        public override void OnBuildingClicked()
        {
            hangarWindow.Toggle();
        }

        public KCHangarFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, ConfigNode node) : base(colony, facilityInfo, node)
        {
            x = double.Parse(node.GetValue("x"));
            y = double.Parse(node.GetValue("y"));
            z = double.Parse(node.GetValue("z"));
            vesselCapacity = int.Parse(node.GetValue("capacity"));

            storedVessels = new List<StoredVessel> { };

            foreach (ConfigNode vesselNode in node.GetNodes("vessel"))
            {
                StoredVessel vessel = new StoredVessel();

                vessel.uuid = Guid.Parse(vesselNode.GetValue("VesselID"));
                vessel.vesselName = vesselNode.GetValue("VesselName");
                vessel.vesselNode = vesselNode.GetNode("VESSEL");

                storedVessels.Add(vessel);
            }
            hangarWindow = new KCHangarFacilityWindow(this);
        }

        public KCHangarFacility(colonyClass colony, KCFacilityInfoClass facilityInfo, bool enabled) : base(colony, facilityInfo, enabled)
        {
            hangarWindow = new KCHangarFacilityWindow(this);

            x = 100;
            y = 100;
            z = 100;

            vesselCapacity = 100;
        }
    }
}
