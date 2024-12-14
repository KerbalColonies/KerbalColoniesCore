using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalColonies.Serialization
{
    internal static class KCFacilityTypeRegistry
    {
        private static Dictionary<string, Type> _registeredTypes = new Dictionary<string, Type>();

        // Register a new type by a unique string key
        public static void RegisterType<T>() where T : KCFacilityBase
        {
            string key = typeof(T).FullName;
            _registeredTypes[key] = typeof(T);
        }

        // Get a registered type by key
        public static Type GetType(string typeName)
        {
            return _registeredTypes.TryGetValue(typeName, out var type) ? type : null;
        }
        public static IEnumerable<string> GetAllRegisteredTypes()
        {
            return _registeredTypes.Keys;
        }
    }

    internal class KCFacilityClassConverter
    {
        public static string SerializeObject(KCFacilityBase obj)
        {
            obj.EncodeString();
            // Include the type's fully qualified name as metadata outside of the serialized JSON
            string typeName = obj.GetType().FullName;
            string json = JsonUtility.ToJson(obj);
            return $"{typeName}/{json}";
        }

        public static KCFacilityBase DeserializeObject(string serializedData)
        {
            // Extract the type name and JSON content
            string[] parts = serializedData.Split('/');
            if (parts.Length != 2)
            {
                Debug.LogError("Invalid serialized data format.");
                return null;
            }

            string typeName = parts[0];
            string json = parts[1];

            // Look up the type using the type registry
            Type type = KCFacilityTypeRegistry.GetType(typeName);
            if (type == null)
            {
                Debug.LogError($"Unknown type: {typeName}");
                return null;
            }

            // Deserialize using the correct type
            KCFacilityBase obj = (KCFacilityBase)JsonUtility.FromJson(json, type);
            obj.Initialize(obj.facilityName, obj.id, obj.facilityData, obj.enabled);

            return obj;
        }
    }
}
