using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
