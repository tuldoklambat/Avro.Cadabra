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
        /// <param name="customFieldProcessor"></param>
        /// <returns></returns>
        public static AvroRecord ToAvroRecord<T>(
            this T obj, 
            string schema, 
            BaseCustomFieldProcessor customFieldProcessor = null) 
            where T : class
        {
            return ToAvroRecord(obj, (RecordSchema)new JsonSchemaBuilder().BuildSchema(schema), customFieldProcessor);
        }

        /// <summary>
        /// Converts an object to an AvroRecord based on the specified schema
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="schema"></param>
        /// <param name="customFieldProcessor"></param>
        /// <returns></returns>
        public static AvroRecord ToAvroRecord<T>(
            this T obj,
            RecordSchema schema,
            BaseCustomFieldProcessor customFieldProcessor = null)
            where T : class
        {
            return _ToAvroRecord(obj, schema, customFieldProcessor);
        }

        /// <summary>
        /// Converts an AvroRecord to a specified instance based on a schema or the type information of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="avroRecord"></param>
        /// <param name="schema">The schema to follow when converting from an AvroRecord. If not specified the T type information will be used.</param>
        /// <param name="customFieldProcessor"></param>
        /// <returns></returns>
        public static T FromAvroRecord<T>(
            this AvroRecord avroRecord, 
            string schema = null, 
            BaseCustomFieldProcessor customFieldProcessor = null)
            where T : class, new()
        {
            return schema == null
                ? _FromAvroRecord<T>(avroRecord, customFieldProcessor: customFieldProcessor)
                : _FromAvroRecord<T>(avroRecord, (RecordSchema)new JsonSchemaBuilder().BuildSchema(schema), customFieldProcessor);
        }

        #region Private Methods

        private static AvroRecord _ToAvroRecord<T>(
            T obj, 
            RecordSchema schema, 
            BaseCustomFieldProcessor customFieldProcessor = null)
        {
            if (schema == null || !obj.GetType().FullName.StartsWith(schema.FullName))
            {
                return null;
            }

            object ValueToAvroType(Type type, RecordField field, object value) =>
                typeof(AvroCadabra).GetMethod("ConvertValueToAvroType", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(type)
                    .Invoke(null, new[] {field.TypeSchema, value ?? field.DefaultValue, customFieldProcessor});


            var avroRecord = new AvroRecord(schema);
            foreach (var field in schema.Fields)
            {
                var memberInfo = customFieldProcessor?.GetMatchingMemberInfo<T>(field)
                                 ?? obj.GetType().GetProperty(field.Name);

                var value = customFieldProcessor?.PreFieldSerialization(obj, field.Name);
                switch (memberInfo)
                {
                    case PropertyInfo pi:
                        avroRecord[field.Position] = ValueToAvroType(pi.PropertyType, field, value ?? pi.GetValue(obj));
                        break;
                    case FieldInfo fi:
                        avroRecord[field.Position] = ValueToAvroType(fi.FieldType, field, value ?? fi.GetValue(obj));
                        break;
                }

            }

            return avroRecord;
        }

        private static T _FromAvroRecord<T>(
            AvroRecord avroRecord,
            RecordSchema schema = null,
            BaseCustomFieldProcessor customFieldProcessor = null)
            where T : class, new()
        {
            if (schema == null)
            {
                schema = avroRecord.Schema;
            }

            var instance = new T();
            foreach (var field in schema.Fields)
            {
                var memberInfo = customFieldProcessor?.GetMatchingMemberInfo<T>(field)
                                 ?? instance.GetType().GetProperty(field.Name);

                // check if the field also exists in the previous schema used in the
                // conversion of the T instance to an AvroRecord otherwise ignore
                if (avroRecord.Schema.TryGetField(field.Name, out RecordField matchField))
                {
                    var value = avroRecord[field.Name]
                                ?? field.DefaultValue;

                    object ValueToDotNetType(Type type) => 
                        ConvertValueToDotNetType(field.TypeSchema, 
                            typeof(T).Assembly.GetTypes(), 
                            type, 
                            value, 
                            customFieldProcessor);

                    switch (memberInfo)
                    {
                        case PropertyInfo pi:
                            var propValue = ValueToDotNetType(pi.PropertyType);
                            pi.SetValue(instance,
                                customFieldProcessor == null
                                    ? propValue
                                    : customFieldProcessor.PreFieldDeserialization(field.Name, propValue));
                            break;
                        case FieldInfo fi:
                            var fieldValue = ValueToDotNetType(fi.FieldType);
                            fi.SetValue(instance,
                                customFieldProcessor == null
                                    ? fieldValue
                                    : customFieldProcessor.PreFieldDeserialization(field.Name, fieldValue));
                            break;
                    }
                }
            }

            return instance;
        }


        private static object ConvertValueToDotNetType(
            TypeSchema typeSchema, 
            IEnumerable<Type> typeCache, 
            Type type, 
            object fieldValue, 
            BaseCustomFieldProcessor customFieldProcessor = null)
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
                    var targetType = typeCache.FirstOrDefault(t => t.FullName == recordSchema.FullName);
                    return typeof(AvroCadabra)
                        .GetMethod("_FromAvroRecord", BindingFlags.Static | BindingFlags.NonPublic)?
                        .MakeGenericMethod(targetType)
                        .Invoke(null, new[] { fieldValue, typeSchema, customFieldProcessor });

                case ArraySchema arraySchema:
                    type = type.IsArray ? type.GetElementType() : type.GenericTypeArguments[0];
                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
                    foreach (var value in (IEnumerable)fieldValue)
                    {
                        avroList.GetType()
                            .GetMethod("Add")
                            ?.Invoke(avroList, new[] { ConvertValueToDotNetType(arraySchema.ItemSchema, typeCache, type, value, customFieldProcessor) });
                    }
                    return avroList.ToArray();

                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType()
                            .GetMethod("Add")
                            ?.Invoke(avroMap, new[] { value.Key.ToString(), ConvertValueToDotNetType(mapSchema.ValueSchema, typeCache, type.GenericTypeArguments[1], value.Value, customFieldProcessor) });
                    }
                    return avroMap;

                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s is RecordSchema))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>().FirstOrDefault(s => s.FullName == ((AvroRecord)fieldValue).Schema.FullName);
                        return ConvertValueToDotNetType(recordSchema, typeCache, type, fieldValue, customFieldProcessor);
                    }
                    if (unionSchema.Schemas.Any(s => s is ArraySchema))
                    {
                        return ConvertValueToDotNetType(unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault(), typeCache, type, fieldValue, customFieldProcessor);
                    }
                    if (unionSchema.Schemas.Any(s => s is MapSchema))
                    {
                        return ConvertValueToDotNetType(unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault(), typeCache, type, fieldValue, customFieldProcessor);
                    }
                    break;

                case NullSchema nullSchema:
                    return null;
            }

            return fieldValue;
        }

        private static object ConvertValueToAvroType<T>(
            TypeSchema typeSchema, 
            T fieldValue,
            BaseCustomFieldProcessor customFieldProcessor = null)
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
                    return typeof(AvroCadabra)
                        .GetMethod("_ToAvroRecord", BindingFlags.Static | BindingFlags.NonPublic)?
                        .MakeGenericMethod(fieldValue.GetType())
                        .Invoke(null, new object[] { fieldValue, recordSchema, customFieldProcessor });
                case ArraySchema arraySchema:
                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(arraySchema.ItemSchema.RuntimeType));
                    foreach (var value in (IEnumerable)fieldValue)
                    {
                        avroList.GetType()
                            .GetMethod("Add")
                            ?.Invoke(avroList, new[] { ConvertValueToAvroType(arraySchema.ItemSchema, value, customFieldProcessor) });
                    }
                    return avroList.ToArray();

                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(mapSchema.KeySchema.RuntimeType, mapSchema.ValueSchema.RuntimeType));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType()
                            .GetMethod("Add")
                            ?.Invoke(avroMap, new[] { value.Key.ToString(), ConvertValueToAvroType(mapSchema.ValueSchema, value.Value, customFieldProcessor) });
                    }
                    return avroMap;

                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s is RecordSchema))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>().FirstOrDefault(s => s.FullName == fieldValue.GetType().FullName);
                        return ConvertValueToAvroType(recordSchema, fieldValue, customFieldProcessor);
                    }
                    if (unionSchema.Schemas.Any(s => s is ArraySchema))
                    {
                        return ConvertValueToAvroType(unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault(), fieldValue, customFieldProcessor);
                    }
                    if (unionSchema.Schemas.Any(s => s is MapSchema))
                    {
                        return ConvertValueToAvroType(unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault(), fieldValue, customFieldProcessor);
                    }
                    break;

                case NullSchema nullSchema:
                    return null;
            }

            return fieldValue;
        }

        #endregion Private Methods

        #region Reflection Extension Methods

        public static TResult GetFieldValue<T,TResult>(
            this T obj, 
            string fieldName, 
            BindingFlags bindingFlags = BindingFlags.NonPublic)
        {
            return (TResult) typeof(T).GetField(fieldName, BindingFlags.Instance | bindingFlags)?.GetValue(obj);
        }

        public static TResult GetPropertyValue<T,TResult>(
            this T obj, 
            string propertyName)
        {
            return (TResult) typeof(T).GetProperty(propertyName)?.GetValue(obj);
        }

        public static void SetPropertyValue<T>(
            this T obj, 
            string propertyName, 
            object value)
        {
            typeof(T).GetProperty(propertyName)?.SetValue(obj, value);
        }

        #endregion
    }
}