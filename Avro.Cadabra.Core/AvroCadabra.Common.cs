// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility
{
    public static partial class AvroCadabra
    {
        private static readonly Dictionary<string, Type> ConcreteTypeCache = new Dictionary<string, Type>();
        private static readonly HashSet<string> AssemblyRegistry = new HashSet<string>();
        private static readonly Dictionary<Type, TypeSchema> TypeSchemaCache = new Dictionary<Type, TypeSchema>();

        private static void RefreshTypeCache(Type type)
        {
            if (AssemblyRegistry.Contains(type.Assembly.FullName))
                return;

            AssemblyRegistry.Add(type.Assembly.FullName);

            if (!type.IsAbstract)
            {
                ConcreteTypeCache[type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}"] = type;
            }

            foreach (var t in type.Assembly.GetTypes().Where(t =>
                !t.IsAbstract && (_typeCacheFilter == null || _typeCacheFilter(t))))
            {
                ConcreteTypeCache[t.Namespace == null ? t.Name : $"{t.Namespace}.{t.Name}"] = t;
            }
        }

        public static TypeSchema GetAvroSchema(this Type type)
        {
            if (!TypeSchemaCache.ContainsKey(type))
            {
                RefreshTypeCache(type);

                var settings = new AvroSerializerSettings
                {
                    Resolver = new AvroPublicMemberContractResolver(),
                    Surrogate = new CustomSurrogate(),
                    KnownTypes = ConcreteTypeCache.Values
                };
                var methodInfo = typeof(AvroSerializer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public, null,
                    new Type[] {typeof(AvroSerializerSettings)}, null);
                var avroSerializer = methodInfo?.MakeGenericMethod(type).Invoke(null, new object[] {settings});
                TypeSchemaCache[type] = (TypeSchema) avroSerializer.GetPropertyValue("WriterSchema");
            }

            return TypeSchemaCache[type];
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

        private class CustomSurrogate : IAvroSurrogate
        {
            public Type GetSurrogateType(Type type)
            {
                if (((TypeInfo) type).ImplementedInterfaces.Contains(typeof(IEnumerable)))
                {
                    switch (type.GenericTypeArguments.Length)
                    {
                        case 2:
                            return typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments);
                        case 1:
                            return typeof(List<>).MakeGenericType(type.GenericTypeArguments);
                    }
                }

                return type;
            }

            public object GetDeserializedObject(object obj, Type targetType)
            {
                return obj;
            }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                return obj;
            }
        }

    }
}