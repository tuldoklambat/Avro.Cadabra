// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gooseman.Avro.Utility
{
    public static partial class AvroCadabra
    {
        private static readonly Dictionary<string, Type> ConcreteTypeCache = new Dictionary<string, Type>();
        private static readonly HashSet<Type> TypeRegistry = new HashSet<Type>();

        private static void RefreshTypeCache(Type type)
        {
            if (TypeRegistry.Contains(type)) 
                return;

            TypeRegistry.Add(type);

            if (!type.IsAbstract)
            {
                ConcreteTypeCache[type.FullName ?? type.Name] = type;
            }

            foreach (var t in type.Assembly.GetTypes().Where(t =>
                !t.IsAbstract && (_typeCacheFilter == null || _typeCacheFilter(t))))
            {
                ConcreteTypeCache[t.FullName ?? t.Name] = t;
            }
        }
        
        public static object GetFieldValue(
            this object obj,
            string fieldName,
            BindingFlags bindingFlags = BindingFlags.NonPublic)
        {
            return obj?.GetType().GetField(fieldName, BindingFlags.Instance | bindingFlags)?.GetValue(obj);
        }

        public static void SetFieldValue(
            this object obj,
            string fieldName,
            object value,
            BindingFlags bindingFlags = BindingFlags.NonPublic)
        {
            obj?.GetType().GetField(fieldName, BindingFlags.Instance | bindingFlags)?.SetValue(obj, value);
        }

        public static object GetPropertyValue(
            this object obj,
            string propertyName)
        {
            return obj?.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        public static void SetPropertyValue(
            this object obj,
            string propertyName,
            object value)
        {
            obj?.GetType().GetProperty(propertyName)?.SetValue(obj, value);
        }

        public static object GetDefault(this Type t)
        {
            return typeof(AvroCadabra)
                .GetMethod("GetDefault", BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(t)
                .Invoke(null, null);
        }

        private static T GetDefault<T>()
        {
            return default;
        }
    }
}
