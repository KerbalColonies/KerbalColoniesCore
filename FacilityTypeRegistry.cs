using KerbalColonies.colonyFacilities;
using System;
using System.Collections.Generic;
using System.Linq;

// KC: Kerbal Colonies
// This mod aimes to create a Colony system with Kerbal Konstructs statics
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

namespace KerbalColonies
{
    public static class KCFacilityTypeRegistry
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
            Type t = _registeredTypes.TryGetValue(typeName, out var type) ? type : null;
            if (t == null)
            {
                KeyValuePair<string, Type> kvp = _registeredTypes.FirstOrDefault(x => x.Key.Contains(typeName));
                t = !kvp.Equals(default(KeyValuePair<string, Type>)) ? kvp.Value : null;
            }
            return t;
        }
        public static IEnumerable<string> GetAllRegisteredTypes()
        {
            return _registeredTypes.Keys;
        }

        private static Dictionary<Type, Type> _registeredInfoTypes = new Dictionary<Type, Type>();

        // Register a facility with its info type
        public static void RegisterFacilityInfo<T, U>() where T : KCFacilityBase where U : KCFacilityInfoClass
        {
            Type facilityType = typeof(T);
            Type infoType = typeof(U);
            if (!_registeredInfoTypes.ContainsKey(facilityType))
            {
                _registeredInfoTypes[facilityType] = infoType;
            }
        }

        // Get the info type for a registered facility type, returns the default info type if not found
        public static Type GetInfoType(Type facilityType)
        {
            if (_registeredInfoTypes.TryGetValue(facilityType, out var infoType))
            {
                return infoType;
            }
            return typeof(KCFacilityInfoClass);
        }
    }
}
