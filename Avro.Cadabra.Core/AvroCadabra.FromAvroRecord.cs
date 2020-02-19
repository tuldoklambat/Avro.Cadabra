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
        /// <summary>
        /// Converts an Avro generic record to a managed object based on the record's schema or supplied schema
        /// </summary>
        /// <param name="avroRecord"></param>
        /// <param name="recordSchema"></param>
        /// <param name="typeCacheFilter"></param>
        /// <param name="customFieldProcessor"></param>
        /// <returns></returns>
        public static T FromAvroRecord<T>(
            this AvroRecord avroRecord,
            string recordSchema = null,
            Func<Type, bool> typeCacheFilter = null,
            BaseCustomFieldProcessor customFieldProcessor = null) where T : class
        {
            TypeSchema typeSchema = avroRecord.Schema;
            if (recordSchema != null)
            {
                typeSchema = new JsonSchemaBuilder().BuildSchema(recordSchema);
                if (!(typeSchema is RecordSchema))
                    throw new ApplicationException("Invalid record schema");
            }

            _typeCacheFilter = typeCacheFilter;
            _customFieldProcessor = customFieldProcessor;

            RefreshTypeCache(typeof(T));

            return (T) FromAvroRecord(avroRecord
                , typeof(T)
                , typeSchema);
        }

        private static object FromAvroRecord(
            object avroObj,
            Type managedType,
            TypeSchema typeSchema)
        {
            if (avroObj == null)
            {
                return null;
            }

            switch (typeSchema)
            {
                case RecordSchema recordSchema:
                    if (managedType.IsAbstract)
                    {
                        if (ConcreteTypeCache.ContainsKey(recordSchema.FullName))
                        {
                            managedType = ConcreteTypeCache[recordSchema.FullName];
                            RefreshTypeCache(managedType);
                        }
                        else
                        {
                            throw new ApplicationException($"Type not found '{recordSchema.FullName}'");
                        }
                    }

                    var avroRecord = (AvroRecord) avroObj;
                    var instance = Activator.CreateInstance(managedType);

                    foreach (var field in recordSchema.Fields)
                    {
                        var memberInfo = _customFieldProcessor?.GetMatchingMemberInfo(managedType, field)
                                         ?? instance.GetType().GetProperty(field.Name);

                        // skip if the record does not contain the field in the schema
                        if (!avroRecord.Schema.TryGetField(field.Name, out _))
                            continue;

                        var value = avroRecord[field.Name] ?? field.DefaultValue;

                        switch (memberInfo)
                        {
                            case PropertyInfo pi:
                                var propValue = FromAvroRecord(value, pi.PropertyType, field.TypeSchema);

                                pi.SetValue(instance,
                                    _customFieldProcessor == null
                                        ? propValue
                                        : _customFieldProcessor.PreFieldDeserialization(field.Name, propValue));

                                break;

                            case FieldInfo fi:
                                var fieldValue = FromAvroRecord(value, fi.FieldType, field.TypeSchema);

                                fi.SetValue(instance,
                                    _customFieldProcessor == null
                                        ? fieldValue
                                        : _customFieldProcessor.PreFieldDeserialization(field.Name, fieldValue));

                                break;
                        }
                    }

                    return instance;

                case LongSchema _:
                    if (typeof(DateTime) == managedType)
                    {
                        return new DateTime((long) avroObj);
                    }

                    break;

                case BooleanSchema _:
                case IntSchema _:
                case DoubleSchema _:
                case FloatSchema _:
                case StringSchema _:
                    break;

                case EnumSchema _:
                    return Enum.Parse(managedType, ((AvroEnum) avroObj).Value, true);

                case ArraySchema arraySchema:
                    var itemType = managedType.IsArray
                        ? managedType.GetElementType()
                        : managedType.GenericTypeArguments[0];

                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));

                    var avroListAdd = avroList.GetType().GetMethod("Add");

                    foreach (var value in (IEnumerable) avroObj)
                    {
                        avroListAdd.Invoke(avroList,
                            new[] {FromAvroRecord(value, itemType, arraySchema.ItemSchema)});
                    }

                    return avroList.ToArray();

                case MapSchema mapSchema:
                    var mapItemType = managedType.GenericTypeArguments[1];

                    var avroMap =
                        Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), mapItemType));

                    var avroMapAdd = avroMap.GetType().GetMethod("Add");

                    foreach (DictionaryEntry value in (IDictionary) avroObj)
                    {
                        avroMapAdd?.Invoke(avroMap,
                            new[]
                            {
                                value.Key.ToString(),
                                FromAvroRecord(value.Value, mapItemType, mapSchema.ValueSchema)
                            });
                    }

                    return avroMap;

                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s is RecordSchema))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>()
                            .FirstOrDefault(s => s.FullName == ((AvroRecord) avroObj).Schema.FullName);

                        return FromAvroRecord(avroObj, managedType, recordSchema);
                    }

                    if (unionSchema.Schemas.Any(s => s is ArraySchema))
                    {
                        return FromAvroRecord(avroObj, managedType,
                            unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault());
                    }

                    if (unionSchema.Schemas.Any(s => s is MapSchema))
                    {
                        return FromAvroRecord(avroObj, managedType,
                            unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault());
                    }

                    break;

                case NullSchema _:
                    return null;
            }

            return avroObj;
        }

        private static Func<Type, bool> _typeCacheFilter;
        private static BaseCustomFieldProcessor _customFieldProcessor;

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
    }
}