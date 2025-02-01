using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    internal class KCWindowManager : MonoBehaviour
    {

        public static KCWindowManager instance = null;
        private static bool layoutInitialized = false;

        private Action draw;

        private List<Action> openWindows;

        /// <summary>
        /// First called before start. used settig up internal vaiabled
        /// </summary>
        public void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }
            instance = this;
            //DontDestroyOnLoad(instance);
            draw = delegate { };
            openWindows = new List<Action>();
        }

        #region Monobehavior functions
        /// <summary>
        /// Called after Awake. used for setting up references between objects and initializing windows.
        /// </summary>
        public void Start()
        {

        }

        /// <summary>
        /// Called every scene-switch. remove all external references here.
        /// </summary>
        public void OnDestroy()
        {

        }

        /// <summary>
        /// Monobehaviour function for drawing. 
        /// </summary>
        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (!layoutInitialized)
            {
                UIConfig.SetStyles();
                layoutInitialized = true;
            }
            draw.Invoke();
        }
        #endregion


        #region public Functions

        /// <summary>
        /// Adds a function pointer to the list of drawn windows.
        /// </summary>
        /// <param name="drawfunct"></param>
        public static void OpenWindow(Action drawfunct)
        {
            if (!IsOpen(drawfunct))
            {
                instance.openWindows.Add(drawfunct);
                instance.draw += drawfunct;
            }
        }

        /// <summary>
        /// Removes a function pointer from the list of open windows.
        /// </summary>
        /// <param name="drawfunct"></param>
        public static void CloseWindow(Action drawfunct)
        {
            if (IsOpen(drawfunct))
            {
                instance.openWindows.Remove(drawfunct);
                instance.draw -= drawfunct;
            }
        }

        /// <summary>
        /// Opens a closed window or closes an open one.
        /// </summary>
        /// <param name="drawfunct"></param>
        public static void ToggleWindow(Action drawfunct)
        {
            if (IsOpen(drawfunct))
            {
                CloseWindow(drawfunct);
            }
            else
            {
                OpenWindow(drawfunct);
            }

        }

        /// <summary>
        /// checks if a window is openend
        /// </summary>
        /// <param name="drawfunct"></param>
        /// <returns></returns>
        public static bool IsOpen(Action drawfunct)
        {
            if (instance == null)
            {
                return false;
            }

            return instance.openWindows.Contains(drawfunct);
        }


        #endregion



    }
}
