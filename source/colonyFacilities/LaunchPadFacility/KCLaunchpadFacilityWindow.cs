using KerbalColonies.UI;
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

namespace KerbalColonies.colonyFacilities.LaunchPadFacility
{
    public class KCLaunchpadFacilityWindow : KCFacilityWindowBase
    {
        KCLaunchpadFacility launchpad;

        bool changeLaunchpadName = false;
        KerbalKonstructs.Core.StaticInstance targetInstance;
        int launchSiteNum;
        string newName;
        Vector2 scrollPos = Vector2.zero;
        Vector2 resourceUsageScrollPos = Vector2.zero;
        protected override void CustomWindow()
        {
            facility.Colony.UpdateColony();

            GUILayout.Label($"Launch sites from this facility:");
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            {
                launchpad.launchSiteName.ToList().ForEach(kvp =>
                {
                    if (GUILayout.Button($"{kvp.Key}: {kvp.Value}", UIConfig.ButtonNoBG))
                    {
                        changeLaunchpadName = true;
                        targetInstance = launchpad.instance[kvp.Key];
                        launchSiteNum = kvp.Key;
                        newName = targetInstance.launchSite.LaunchSiteName;
                    }
                });
            }
            GUILayout.EndScrollView();

            if (changeLaunchpadName)
            {
                GUILayout.Label($"Changing name of launchpad {targetInstance.launchSite.LaunchSiteName}:");
                newName = GUILayout.TextField(newName);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("OK", GUILayout.Height(23)))
                    {
                        KerbalKonstructs.Core.LaunchSiteManager.DeleteLaunchSite(targetInstance.launchSite);
                        targetInstance.launchSite.LaunchSiteName = newName;
                        launchpad.launchSiteName[launchSiteNum] = targetInstance.launchSite.LaunchSiteName;
                        KerbalKonstructs.Core.LaunchSiteManager.RegisterLaunchSite(targetInstance.launchSite);
                        changeLaunchpadName = false;
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Height(23)))
                    {
                        changeLaunchpadName = false;
                    }
                }
                GUILayout.EndHorizontal();
            }


            GUILayout.Space(10);
            if (facility.facilityInfo.ResourceUsage[facility.level].Count > 0)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Resource Consumption Priority: {launchpad.ResourceConsumptionPriority}", GUILayout.Height(18));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.RepeatButton("--", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(23))) launchpad.ResourceConsumptionPriority--;
                    if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(23)) | GUILayout.RepeatButton("++", GUILayout.Width(30), GUILayout.Height(23))) launchpad.ResourceConsumptionPriority++;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Resource usage:");
                resourceUsageScrollPos = GUILayout.BeginScrollView(resourceUsageScrollPos, GUILayout.Height(120));
                {
                    launchpad.facilityInfo.ResourceUsage[facility.level].ToList().ForEach(kvp =>
                        GUILayout.Label($"- {kvp.Key.displayName}: {kvp.Value}/s")
                    );
                }
                GUILayout.EndScrollView();
            }
        }
        public KCLaunchpadFacilityWindow(KCLaunchpadFacility launchpad) : base(launchpad, Configuration.createWindowID())
        {
            this.launchpad = launchpad;
            toolRect = new Rect(100, 100, 400, 300);
        }
    }
}
