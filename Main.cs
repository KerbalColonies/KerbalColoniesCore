using Smooth.Compare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StaticColonization
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class StaticColonization : MonoBehaviour
    {
        const string APP_NAME = "StaticColonization";

        protected void Start()
        {
            writeDebug("Starting SC");
        }


        public void Update()
        {
            writeDebug("Update!!!");
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
