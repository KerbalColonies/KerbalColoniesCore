using KerbalColonies.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.colonyFacilities.KCMiningFacility
{
    public class KCMiningFacilityWindow : KCFacilityWindowBase
    {
        KCMiningFacility miningFacility;
        public KerbalGUI kerbalGUI;

        private Vector2 resourceScrollPos = new Vector2();
        protected override void CustomWindow()
        {
            miningFacility.Update();

            if (kerbalGUI == null)
            {
                kerbalGUI = new KerbalGUI(miningFacility, true);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(toolRect.width / 2 - 10));
                kerbalGUI.StaffingInterface();
                GUILayout.EndVertical();

                Dictionary<PartResourceDefinition, double> maxPerResource = new Dictionary<PartResourceDefinition, double> { };
                miningFacility.miningFacilityInfo.rates.Where(kvp => kvp.Key <= miningFacility.level).ToList().ForEach(kvp => kvp.Value.ForEach(rate =>
                {
                    if (!maxPerResource.ContainsKey(rate.resource)) maxPerResource.Add(rate.resource, rate.max);
                    else maxPerResource[rate.resource] += rate.max;
                }));

                resourceScrollPos = GUILayout.BeginScrollView(resourceScrollPos, GUILayout.Width(toolRect.width / 2 - 10));
                {
                    KCMiningFacilityInfo miningInfo = miningFacility.miningFacilityInfo;

                    miningFacility.storedResoures.ToList().ForEach(res =>
                    {
                        GUILayout.Label($"<size=20><b>{res.Key.displayName}</b></size>");
                        GUILayout.Label($"Daily rate: {(miningFacility.groupDensities.Sum(kvp => kvp.Value[res.Key]) * miningFacility.getKerbals().Count):f2}/day");
                        GUILayout.Label($"Stored: {res.Value:f2}");
                        GUILayout.Label($"Max: {maxPerResource[res.Key]:f2}");
                        if (GUILayout.Button($"Retrieve {res.Key.displayName}")) miningFacility.RetriveResource(res.Key);

                        GUILayout.Space(10);
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                        GUILayout.Space(10);
                    });
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }

        protected override void OnClose()
        {
            if (kerbalGUI != null && kerbalGUI.ksg != null)
            {
                kerbalGUI.ksg.Close();
                kerbalGUI.transferWindow = false;
            }
        }


        public KCMiningFacilityWindow(KCMiningFacility miningFacility) : base(miningFacility, Configuration.createWindowID())
        {
            this.miningFacility = miningFacility;
            toolRect = new Rect(100, 100, 800, 600);
            this.kerbalGUI = null;
        }
    }
}
