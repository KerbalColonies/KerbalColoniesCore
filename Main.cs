using Smooth.Compare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KerbalKonstructs;

namespace KerbalKonstructsKolonization
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalKonstructsKolonization : MonoBehaviour
    {
        const string APP_NAME = "KerbalKonstructsKolonization";

        protected void Start()
        {
            writeDebug("Starting KKK");
        }


        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                KerbalKonstructs.API.PlaceStatic("KK_1700m_C_runway", FlightGlobals.currentMainBody.name, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude, (float) FlightGlobals.ship_altitude - 10f, 0f);
            }
        }

        void writeDebug(string text)
        {
            if (Configuration.enableLogging)
            {
                writeLog(text);
            }
        }

        void writeLog(string text)
        {
            KSPLog.print(APP_NAME + ": " + text);
        }
    }
}
