using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// KC: Kerbal ColonyBuilding
// This mod aimes to create a colony system with Kerbal Konstructs statics
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

/// This file is a modified version of the KKWindow.cs file from the Kerbal Konstructs mod which is licensed under the MIT License.

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies
{
    public abstract class KCWindow
    {
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }

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
            OnOpen();
            KCWindowManager.OpenWindow(this.Draw);
        }

        /// <summary>
        /// unregisters the drawing function to the windowmanager
        /// </summary>
        public virtual void Close()
        {
            OnClose();
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
