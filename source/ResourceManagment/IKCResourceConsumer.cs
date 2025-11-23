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
    public interface IKCResourceConsumer
    {
        Dictionary<PartResourceDefinition, double> ExpectedResourceConsumption(double lastTime, double deltaTime, double currentTime);
        void ConsumeResources(double lastTime, double deltaTime, double currentTime);

        /// <summary>
        /// Gets called when there are insufficient resources to meet the expected consumption.
        /// </summary>
        /// <returns>MUST return unused resources, otherwise they are lost</returns>
        Dictionary<PartResourceDefinition, double> InsufficientResources(double lastTime, double deltaTime, double currentTime, Dictionary<PartResourceDefinition, double> sufficientResources, Dictionary<PartResourceDefinition, double> limitingResources);
        Dictionary<PartResourceDefinition, double> ResourceConsumptionPerSecond();
        int ResourceConsumptionPriority { get; }
    }
}
