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
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility
{
    public static partial class AvroCadabra
    {
        private static ICustomValueSetter _customValueSetter;
        private static Func<Type, bool> _typeCacheFilter;

        /// <summary>
        /// Converts an Avro generic record to a managed object based on the record's schema or supplied schema
        /// </summary>
        /// <param name="avroRecord"></param>
        /// <param name="recordSchema"></param>
        /// <param name="typeCacheFilter"></param>
        /// <param name="customValueSetter"></param>
        /// <returns></returns>
        public static T FromAvroRecord<T>(
            this AvroRecord avroRecord,
            string recordSchema = null,
            Func<Type, bool> typeCacheFilter = null,
            ICustomValueSetter customValueSetter = null) where T : class
        {
            TypeSchema typeSchema = avroRecord.Schema;
            if (recordSchema != null)
            {
                typeSchema = new JsonSchemaBuilder().BuildSchema(recordSchema);
                if (!(typeSchema is RecordSchema))
                    throw new ApplicationException("Invalid record schema");
            }

            _typeCacheFilter = typeCacheFilter;
            _customValueSetter = customValueSetter;

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
                    if (managedType.IsAbstract
                        || !managedType.IsGenericType
                        || managedType == typeof(object)
                        || managedType == typeof(AvroRecord))
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

                    // the Where clause here ensures the field in the new schema (if provided) matches a field
                    // in the previous embedded schema
                    foreach (var field in
                        recordSchema.Fields.Where(f => avroRecord.Schema.TryGetField(f.Name, out _)))
                    {
                        var runtimeType = instance.GetType().GetProperty(field.Name)?.PropertyType ??
                                          field.TypeSchema.RuntimeType;

                        var value = FromAvroRecord(avroRecord[field.Name] ?? field.DefaultValue,
                            runtimeType, field.TypeSchema);

                        if (!(_customValueSetter?.SetValue(instance, field.Name, value) ?? false))
                        {
                            instance.SetPropertyValue(field.Name, value);
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
                    managedType = Nullable.GetUnderlyingType(managedType) ?? managedType;
                    return Enum.Parse(managedType, ((AvroEnum) avroObj).Value, true);

                case ArraySchema arraySchema:
                    var itemType = managedType.IsArray
                        ? managedType.GetElementType()
                        : managedType.IsGenericType
                            ? managedType.GenericTypeArguments[0]
                            : typeof(object);

                    var avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));

                    var avroListAdd = avroList.GetType().GetMethod("Add");
                    var avroListToArray = avroList.GetType().GetMethod(("ToArray"));

                    foreach (var value in (IEnumerable)avroObj)
                    {
                        avroListAdd?.Invoke(avroList,
                            new[] { FromAvroRecord(value, itemType, arraySchema.ItemSchema) });
                    }

                    return avroListToArray?.Invoke(avroList, null);

                case MapSchema mapSchema:
                    var mapItemType = managedType.IsGenericType
                        ? managedType.GenericTypeArguments[1]
                        : typeof(object);

                    var avroMap =
                        Activator.CreateInstance(
                            typeof(Dictionary<,>).MakeGenericType(mapSchema.KeySchema.RuntimeType, mapItemType));

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

                    if (unionSchema.Schemas.Any(s => s is EnumSchema))
                    {
                        var enumSchema = unionSchema.Schemas.OfType<EnumSchema>()
                            .FirstOrDefault(s => s.FullName == ((AvroEnum) avroObj).Schema.FullName);

                        return FromAvroRecord(avroObj, managedType, enumSchema);
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
    }
}