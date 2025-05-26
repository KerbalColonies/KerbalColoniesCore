using KerbalColonies.colonyFacilities;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
// Copyright (c) 2024-2025 AMPW, Halengar

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

namespace KerbalColonies.UI
{
    /// <summary>
    /// Handles the onTitleChange so the facilities don't need to do that themselves
    /// </summary>
    public abstract class KCFacilityWindowBase : KCWindowBase
    {
        protected KCFacilityBase facility;

        public override void OnTitleChange(string title)
        {
            facility.changeDisplayName(title);
        }

        public KCFacilityWindowBase(KCFacilityBase facility, int windowID) : base(windowID, facility.DisplayName, true)
        {
            this.facility = facility;
        }
    }
}
