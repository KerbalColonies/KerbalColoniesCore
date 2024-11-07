using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalColonies
{
    abstract internal class KCWindow
    {
        /// <summary>
        /// Basic drawing function. Put excludes or references to other drawing functions in a override here
        /// </summary>
        public virtual void Draw()
        {

        }

        /// <summary>
        /// registers the drawing function to the windowmanager
        /// </summary>
        public virtual void Open()
        {
            KCWindowManager.OpenWindow(this.Draw);
        }

        /// <summary>
        /// unregisters the drawing function to the windowmanager
        /// </summary>
        public virtual void Close()
        {
            KCWindowManager.CloseWindow(this.Draw);
        }

        /// <summary>
        /// Switches the state of the window
        /// </summary>
        public virtual void Toggle()
        {
            if (IsOpen())
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// return if a window is open
        /// </summary>
        /// <returns>true if window is open</returns>
        public virtual bool IsOpen()
        {
            return KCWindowManager.IsOpen(this.Draw);
        }
    }
}
