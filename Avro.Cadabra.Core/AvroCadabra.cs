// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.

using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gooseman.Avro.Utility
{
    /// <summary>
    /// Helper class to convert class to and from Avro
    /// </summary>
    public static class AvroCadabra
    {
        /// <summary>
        /// Converts an object to an AvroRecord based on the specified schema
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="schema">The schema to follow when converting the object</param>
        /// <returns></returns>
        public static AvroRecord ToAvroRecord(this object obj, string schema)
        {
            return _ToAvroRecord(obj, (RecordSchema)new JsonSchemaBuilder().BuildSchema(schema));
        }

        /// <summary>
        /// Converts an AvroRecord to a specified instance based on a schema or the type information of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="avroRecord"></param>
        /// <param name="schema">The schema to follow when converting from an AvroRecord. If not specified the T type information will be used.</param>
        /// <returns></returns>
        public static T FromAvroRecord<T>(this AvroRecord avroRecord, string schema = null) where T : class, new()
        {
            return schema == null
                ? _FromAvroRecord<T>(avroRecord)
                : _FromAvroRecord<T>(avroRecord, (RecordSchema)new JsonSchemaBuilder().BuildSchema(schema));
        }

        #region Private Methods

        private static AvroRecord _ToAvroRecord(object obj, RecordSchema schema)
        {
            if (schema == null || !obj.GetType().FullName.StartsWith(schema.FullName))
            {
                return null;
            }

            var avroRecord = new AvroRecord(schema);
            foreach (var field in schema.Fields)
            {
                var value = obj.GetValue(field) ?? field.DefaultValue;
                avroRecord[field.Position] = ConvertValueToAvroType(field.TypeSchema, value);
            }

            return avroRecord;
        }

        private static T _FromAvroRecord<T>(AvroRecord avroRecord, RecordSchema schema = null) where T : class, new()
        {
            if (schema == null)
            {
                schema = avroRecord.Schema;
            }

            var instance = new T();
            foreach (var field in schema.Fields)
            {
                var property = instance.GetType().GetProperty(field.Name);

                // check if the field also exists in the previous schema used in the
                // conversion of the T instance to an AvroRecord otherwise ignore
                if (avroRecord.Schema.TryGetField(field.Name, out RecordField matchField))
                {
                    var value = avroRecord[field.Name] ?? field.DefaultValue;
                    value = ConvertValueToDotNetType(field.TypeSchema, typeof(T).Assembly.GetTypes(), property.PropertyType, value);
                    property.SetValue(instance, value);
                }
            }
            return instance;
        }

        private static object ConvertValueToDotNetType(TypeSchema typeSchema, IEnumerable<Type> typeCache, Type type, object fieldValue)
        {
            if (fieldValue == null)
            {
                return null;
            }

            if (typeof(DateTime) == type)
            {
                return new DateTime((long)fieldValue);
            }

            switch (typeSchema)
            {
                case BooleanSchema boolSchema:
                case IntSchema intSchema:
                case LongSchema longSchema:
                case DoubleSchema doubleSchema:
                case FloatSchema floatSchema:
                case StringSchema stringSchema:
                    break;

                case EnumSchema enumSchema:
                    return Enum.Parse(type, ((AvroEnum)fieldValue).Value, true);

                case RecordSchema recordSchema:
                    var targetType = typeCache.Where(t => t.FullName == recordSchema.FullName).FirstOrDefault();
                    return typeof(AvroCadabra)
                        .GetMethod("_FromAvroRecord", BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(targetType)
                        .Invoke(null, new[] { fieldValue, typeSchema });

                case ArraySchema arraySchema:
                    type = type.IsArray ? type.GetElementType() : type.GenericTypeArguments[0];
                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
                    foreach (var value in (IEnumerable)fieldValue)
                    {
                        avroList.GetType().GetMethod("Add").Invoke(avroList, new[] { ConvertValueToDotNetType(arraySchema.ItemSchema, typeCache, type, value) });
                    }
                    return avroList.ToArray();

                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType()
                            .GetMethod("Add")
                            .Invoke(avroMap, new[] { value.Key.ToString(), ConvertValueToDotNetType(mapSchema.ValueSchema, typeCache, type.GenericTypeArguments[1], value.Value) });
                    }
                    return avroMap;

                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(RecordSchema)))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>().Where(s => s.FullName == ((AvroRecord)fieldValue).Schema.FullName).FirstOrDefault();
                        return ConvertValueToDotNetType(recordSchema, typeCache, type, fieldValue);
                    }
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(ArraySchema)))
                    {
                        return ConvertValueToDotNetType(unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault(), typeCache, type, fieldValue);
                    }
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(MapSchema)))
                    {
                        return ConvertValueToDotNetType(unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault(), typeCache, type, fieldValue);
                    }
                    break;

                case NullSchema nullSchema:
                    return null;
            }

            return fieldValue;
        }

        private static object ConvertValueToAvroType(TypeSchema typeSchema, object fieldValue)
        {
            if (fieldValue == null)
                return null;

            if (typeof(DateTime).IsAssignableFrom(fieldValue?.GetType()))
            {
                return Convert.ToDateTime(fieldValue).Ticks;
            }

            switch (typeSchema)
            {
                case BooleanSchema boolSchema:
                case IntSchema intSchema:
                case LongSchema longSchema:
                case DoubleSchema doubleSchema:
                case FloatSchema floatSchema:
                case StringSchema stringSchema:
                    break;

                case EnumSchema enumSchema:
                    return new AvroEnum(enumSchema) { Value = fieldValue.ToString() };

                case RecordSchema recordSchema:
                    return _ToAvroRecord(fieldValue, recordSchema);

                case ArraySchema arraySchema:
                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(arraySchema.ItemSchema.RuntimeType));
                    foreach (var value in (IEnumerable)fieldValue)
                    {
                        avroList.GetType()
                            .GetMethod("Add")
                            .Invoke(avroList, new[] { ConvertValueToAvroType(arraySchema.ItemSchema, value) });
                    }
                    return avroList.ToArray();

                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(mapSchema.KeySchema.RuntimeType, mapSchema.ValueSchema.RuntimeType));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType()
                            .GetMethod("Add")
                            .Invoke(avroMap, new[] { value.Key.ToString(), ConvertValueToAvroType(mapSchema.ValueSchema, value.Value) });
                    }
                    return avroMap;

                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(RecordSchema)))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>().Where(s => s.FullName == fieldValue.GetType().FullName).FirstOrDefault();
                        return ConvertValueToAvroType(recordSchema, fieldValue);
                    }
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(ArraySchema)))
                    {
                        return ConvertValueToAvroType(unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault(), fieldValue);
                    }
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(MapSchema)))
                    {
                        return ConvertValueToAvroType(unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault(), fieldValue);
                    }
                    break;

                case NullSchema nullSchema:
                    return null;
            }

            return fieldValue;
        }

        private static object GetValue(this object obj, RecordField field)
        {
            return obj.GetType().GetProperty(field.Name).GetValue(obj);
        }

        #endregion Private Methods
    }
}