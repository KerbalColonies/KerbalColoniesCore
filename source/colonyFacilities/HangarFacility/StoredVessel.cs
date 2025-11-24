using System;

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

/// This file contains parts from the Kerbal Konstructs mod Hangar.cs file which is licensed under the MIT License.
/// The general idea on how to store vessels is also taken from the Kerbal Konstructs mod

// Kerbal Konstructs Plugin (when not states otherwithe in the class-file)
// The MIT License (MIT)

// Copyright(c) 2015-2017 Matt "medsouz" Souza, Ashley "AlphaAsh" Hall, Christian "GER-Space" Bronk, Nikita "whale_2" Makeev, and the KSP-RO team.

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace KerbalColonies.colonyFacilities.HangarFacility
{
    public class StoredVessel
    {
        public string vesselName;
        public Guid uuid;
        public ConfigNode vesselNode;

        public double vesselVolume => x * y * z;
        public double x;
        public double y;
        public double z;
        public double? vesselBuildTime;
        public double? entireVesselBuildTime;
        public double? vesselDryMass;

        public StoredVessel(string vesselName, Guid uuid, double x, double y, double z, ConfigNode vesselNode = null, double? vesselBuildTime = null, double? entireVesselBuildTime = null, double? vesselDryMass = null)
        {
            this.vesselName = vesselName;
            this.uuid = uuid;
            this.vesselNode = vesselNode;
            this.x = x;
            this.y = y;
            this.z = z;
            this.vesselBuildTime = vesselBuildTime;
            this.entireVesselBuildTime = entireVesselBuildTime;
            this.vesselDryMass = vesselDryMass;
        }
    }
}
