// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gooseman.Avro.Utility
{
    public static partial class AvroCadabra
    {
        /// <summary>
        /// Converts an object to an Avro generic record based on the supplied schema
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="recordSchema"></param>
        /// <param name="customFieldProcessor">For special field processing</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AvroRecord ToAvroRecord<T>(
            this T obj,
            string recordSchema,
            BaseCustomFieldProcessor customFieldProcessor = null) where T : class
        {
            var typeSchema = new JsonSchemaBuilder().BuildSchema(recordSchema);
            if (typeSchema is RecordSchema)
            {
                return (AvroRecord) ToAvroRecord(obj, typeSchema, customFieldProcessor);
            }

            throw new ApplicationException("Invalid record schema");
        }

        private static object ToAvroRecord(
            object obj,
            TypeSchema typeSchema,
            BaseCustomFieldProcessor customFieldProcessor = null)
        {
            if (obj == null)
            {
                return null;
            }

            switch (typeSchema)
            {
                case RecordSchema recordSchema:
                    var avroRecord = new AvroRecord(recordSchema);
                    foreach (var field in recordSchema.Fields)
                    {
                        var memberInfo = customFieldProcessor?.GetMatchingMemberInfo(obj.GetType(), field)
                                         ?? obj.GetType().GetProperty(field.Name);

                        var value = customFieldProcessor?.PreFieldSerialization(obj, field.Name);
                        switch (memberInfo)
                        {
                            case PropertyInfo pi:
                                avroRecord[field.Position] = ToAvroRecord(
                                    value ?? pi.GetValue(obj) ?? field.DefaultValue, field.TypeSchema,
                                    customFieldProcessor);
                                
                                break;
                            case FieldInfo fi:
                                avroRecord[field.Position] = ToAvroRecord(
                                    value ?? fi.GetValue(obj) ?? field.DefaultValue, field.TypeSchema,
                                    customFieldProcessor);
                                
                                break;
                        }
                    }

                    return avroRecord;

                case LongSchema _:
                    if (obj is DateTime)
                    {
                        return Convert.ToDateTime(obj).Ticks;
                    }

                    break;

                case BooleanSchema _:
                case IntSchema _:
                case DoubleSchema _:
                case FloatSchema _:
                case StringSchema _:
                    break;

                case EnumSchema enumSchema:
                    return new AvroEnum(enumSchema) {Value = obj.ToString()};

                case ArraySchema arraySchema:
                    dynamic avroList =
                        Activator.CreateInstance(typeof(List<>).MakeGenericType(arraySchema.ItemSchema.RuntimeType));

                    var avroListAdd = avroList.GetType().GetMethod("Add");

                    foreach (var value in (IEnumerable) obj)
                    {
                        avroListAdd.Invoke(avroList,
                            new[] {ToAvroRecord(value, arraySchema.ItemSchema, customFieldProcessor)});
                    }

                    return avroList.ToArray();

                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(
                        typeof(Dictionary<,>).MakeGenericType(mapSchema.KeySchema.RuntimeType,
                            mapSchema.ValueSchema.RuntimeType));

                    var avroMapAdd = avroMap.GetType().GetMethod("Add");

                    foreach (DictionaryEntry value in (IDictionary) obj)
                    {
                        avroMapAdd?.Invoke(avroMap,
                            new[]
                            {
                                value.Key.ToString(),
                                ToAvroRecord(value.Value, mapSchema.ValueSchema, customFieldProcessor)
                            });
                    }

                    return avroMap;

                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s is RecordSchema))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>()
                            .FirstOrDefault(s => s.FullName == obj.GetType().FullName);
                        return ToAvroRecord(obj, recordSchema, customFieldProcessor);
                    }

                    if (unionSchema.Schemas.Any(s => s is ArraySchema))
                    {
                        return ToAvroRecord(obj, unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault(),
                            customFieldProcessor);
                    }

                    if (unionSchema.Schemas.Any(s => s is MapSchema))
                    {
                        return ToAvroRecord(obj, unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault(),
                            customFieldProcessor);
                    }

                    break;

                case NullSchema _:
                    return null;
            }

            return obj;
        }
    }
}