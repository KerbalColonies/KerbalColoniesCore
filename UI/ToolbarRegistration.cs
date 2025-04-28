using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolbarControl_NS;
using UnityEngine;

namespace KerbalColonies.UI
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class ToolbarRegistration : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod("KerbalColonies_NS", "Kerbal Colonies");
        }
    }
}
