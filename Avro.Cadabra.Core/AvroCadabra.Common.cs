// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using System.Reflection;

namespace Gooseman.Avro.Utility
{
    public static partial class AvroCadabra
    {
        public static object GetFieldValue(
            this object obj,
            string fieldName,
            BindingFlags bindingFlags = BindingFlags.NonPublic)
        {
            return obj?.GetType().GetField(fieldName, BindingFlags.Instance | bindingFlags)?.GetValue(obj);
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
