using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class ResourceConversionList
    {
        public static HashSet<ResourceConversionList> AllConversionLists = new HashSet<ResourceConversionList> { }; // all the lists of recipes

        public static ResourceConversionList GetConversionList(string name) => AllConversionLists.FirstOrDefault(rcl => rcl.Name == name);

        public static bool operator ==(ResourceConversionList a, ResourceConversionList b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Name == b.Name;
        }
        public static bool operator !=(ResourceConversionList a, ResourceConversionList b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
            else if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;
            return a.Name != b.Name;
        }
        public override bool Equals(object obj)
        {
            return obj is ResourceConversionList && ((ResourceConversionList)obj).Name == this.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public string Name { get; set; } // the name of the list
        public List<string> ConversionList { get; set; } = new List<string> { }; // the names of other lists
        public List<string> RecipeNames { get; set; } = new List<string> { }; // the names of the recipes

        public HashSet<ResourceConversionRate> GetRecipes()
        {
            HashSet<ResourceConversionRate> resourceConversionRates = new HashSet<ResourceConversionRate>();
            RecipeNames.ForEach(r =>
            {
                ResourceConversionRate rcr = ResourceConversionRate.GetConversionRate(r);
                if (rcr != null) resourceConversionRates.Add(rcr);
            });
            ConversionList.ForEach(l =>
            {
                ResourceConversionList rcl = AllConversionLists.FirstOrDefault(r => r.Name == l);
                if (rcl != null)
                {
                    resourceConversionRates.UnionWith(rcl.GetRecipes());
                }
            });

            return resourceConversionRates;
        }

        public ResourceConversionList(string name, List<string> ConversionList = null, List<string> RecipeNames = null)
        {
            Name = name;
            if (ConversionList != null) this.ConversionList = ConversionList;
            if (RecipeNames != null) this.RecipeNames = RecipeNames;

            if (!AllConversionLists.Contains(this)) AllConversionLists.Add(this);
            else
            {
                ResourceConversionList existingList = AllConversionLists.FirstOrDefault(r => r.Name == name);
                Configuration.writeLog($"Warning: The recipe list {name} already exists in the list of conversion lists.");
                Configuration.writeLog("Adding new recipes to the existing list:");
                RecipeNames.ForEach(recipe =>
                {
                    if (!existingList.RecipeNames.Contains(recipe))
                    {
                        existingList.RecipeNames.Add(recipe);
                        Configuration.writeLog($"- {recipe}");
                    }
                });
                Configuration.writeLog("\nAdding new conversion lists to the existing list:");
                ConversionList.ForEach(conversionList =>
                {
                    if (!existingList.ConversionList.Contains(conversionList))
                    {
                        existingList.ConversionList.Add(conversionList);
                        Configuration.writeLog($"- {conversionList}");
                    }
                });
            }
        }
    }
}
