using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
