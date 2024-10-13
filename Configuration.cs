using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalKonstructsKolonization
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class SettingsLoader : MonoBehaviour
    {
        public void Awake()
        {
            // load settings when game start

        }
    }

    /// <summary>
    /// Reads and holds configuration parameters
    /// </summary>
    static class Configuration
    {
        public static bool enableLogging = true; //Enable this only in debug purposes as it floods the logs very much

    }
}
