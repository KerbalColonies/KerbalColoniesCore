using System.Collections.Generic;

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

namespace KerbalColonies.ResourceManagment
{
    public interface IKCResourceStorage
    {
        SortedDictionary<PartResourceDefinition, double> Resources { get; }

        double Volume { get; }
        double UsedVolume { get; }
        int Priority { get; }

        double MaxStorable(PartResourceDefinition resource);
        SortedDictionary<PartResourceDefinition, double> StoredResources(double lastTime, double deltaTime, double currentTime);
        double ChangeResourceStored(PartResourceDefinition resource, double Amount);
    }
}
