using System;
using System.Collections.Generic;
using System.Linq;

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

namespace KerbalColonies.colonyFacilities.KCResourceConverterFacility
{
    public class ResourceConversionRate
    {
        public static HashSet<ResourceConversionRate> conversionRates = new HashSet<ResourceConversionRate> { };

        public static ResourceConversionRate GetConversionRate(string name) => conversionRates.FirstOrDefault(rcr => rcr.RecipeName == name);

        public static bool operator ==(ResourceConversionRate a, ResourceConversionRate b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.RecipeName == b.RecipeName;
        }
        public static bool operator !=(ResourceConversionRate a, ResourceConversionRate b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;
            return a.RecipeName != b.RecipeName;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceConversionRate && ((ResourceConversionRate)obj).recipeName == this.recipeName;
        }

        public override int GetHashCode()
        {
            return recipeName.GetHashCode();
        }

        private string recipeName;
        public string RecipeName { get { return recipeName; } }

        public string DisplayName;

        private Dictionary<PartResourceDefinition, double> inputResources;
        private Dictionary<PartResourceDefinition, double> outputResources;

        public Dictionary<PartResourceDefinition, double> InputResources { get => inputResources; }
        public Dictionary<PartResourceDefinition, double> OutputResources { get => outputResources; }

        public ResourceConversionRate(string recipeName, string displayName, Dictionary<PartResourceDefinition, double> inputResources, Dictionary<PartResourceDefinition, double> outputResources)
        {
            this.recipeName = recipeName;
            this.DisplayName = displayName;
            this.inputResources = inputResources;
            this.outputResources = outputResources;

            if (!conversionRates.Contains(this)) conversionRates.Add(this);
            else throw new Exception($"The recipe {recipeName} already exists in the list of conversion rates. Please check your config file.");
        }
    }
}
