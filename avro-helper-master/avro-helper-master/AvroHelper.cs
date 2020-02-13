using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace avro_helper_master
{
    /// <summary>
    /// Helper class to convert class to and from Avro
    /// </summary>
    public static class AvroHelper
    {
        public static AvroRecord ToAvroRecord(this object obj, RecordSchema schema)
        {
            if (schema == null || obj.GetType().FullName != schema.FullName)
                return null;

            var avroRecord = new AvroRecord(schema);

            foreach (var field in schema.Fields)
            {
                var value = obj.GetValue(field) ?? field.DefaultValue;
                avroRecord[field.Position] = ProcessField(field.TypeSchema, value);
            }

            return avroRecord;
        }

        public static T FromAvroRecord<T>(this AvroRecord avroRecord) where T : class, new()
        {
            var instance = new T();
            foreach (var field in avroRecord.Schema.Fields) 
            {
                var property = instance.GetType().GetProperty(field.Name);
                var value = ConvertValueToDotNetType(field.TypeSchema, typeof(T).Assembly.GetTypes(), property.PropertyType, avroRecord[field.Name]);
                property.SetValue(instance, value);
            }
            return instance;
        }

        private static object ConvertValueToDotNetType(TypeSchema typeSchema, IEnumerable<Type> typeCache, Type type, object fieldValue)
        {
            switch (typeSchema)
            {
                case BooleanSchema boolSchema:
                case IntSchema intSchema:
                case LongSchema longSchema:
                case DoubleSchema doubleSchema:
                case FloatSchema floatSchema:
                case StringSchema stringSchema:
                    return fieldValue;
                case EnumSchema enumSchema:
                    return Enum.Parse(type, ((AvroEnum)fieldValue).Value, true);
                case RecordSchema recordSchema:
                    var targetType = typeCache.Where(t => t.FullName == recordSchema.FullName).FirstOrDefault();
                    return typeof(AvroHelper)
                        .GetMethod("FromAvroRecord")
                        .MakeGenericMethod(targetType)
                        .Invoke(null, new[] { fieldValue });
                case ArraySchema arraySchema:
                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.IsArray ? type.GetElementType() : type.GenericTypeArguments[0]));
                    foreach (var value in (IEnumerable)fieldValue)
                    {
                        avroList.GetType().GetMethod("Add").Invoke(avroList, new[] { ConvertValueToDotNetType(arraySchema.ItemSchema, typeCache, type, value) });
                    }
                    return avroList.ToArray();
                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType().GetMethod("Add").Invoke(avroMap, new[] { value.Key.ToString(), ConvertValueToDotNetType(mapSchema.ValueSchema, typeCache, type, value.Value) });
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
                    break;
            }

            return null;
        }

        private static object GetValue(this object obj, RecordField field)
        {
            return obj.GetType().GetProperty(field.Name).GetValue(obj);
        }

        private static object ProcessField(TypeSchema typeSchema, object fieldValue)
        {
            if (fieldValue == null)
                return null;

            if (typeof(DateTime).IsAssignableFrom(fieldValue?.GetType()))
            {
                return Convert.ToDateTime(fieldValue).Ticks;
            }

            switch(typeSchema)
            {
                case BooleanSchema boolSchema:
                case IntSchema intSchema:
                case LongSchema longSchema:
                case DoubleSchema doubleSchema:
                case FloatSchema floatSchema:
                case StringSchema stringSchema:
                    return fieldValue;
                case EnumSchema enumSchema:
                    return new AvroEnum(enumSchema) { Value = fieldValue.ToString() };
                case RecordSchema recordSchema:
                    return fieldValue.ToAvroRecord(recordSchema);
                case ArraySchema arraySchema:
                    dynamic avroList = Activator.CreateInstance(typeof(List<>).MakeGenericType(arraySchema.ItemSchema.RuntimeType));
                    foreach (var value in (IEnumerable)fieldValue)
                    {
                        avroList.GetType().GetMethod("Add").Invoke(avroList, new[] { ProcessField(arraySchema.ItemSchema, value) });
                    }
                    return avroList.ToArray();
                case MapSchema mapSchema:
                    var avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(mapSchema.KeySchema.RuntimeType, mapSchema.ValueSchema.RuntimeType));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType().GetMethod("Add").Invoke(avroMap, new[] { value.Key.ToString(), ProcessField(mapSchema.ValueSchema, value.Value) });
                    }
                    return avroMap;
                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(RecordSchema)))
                    {
                        var recordSchema = unionSchema.Schemas.OfType<RecordSchema>().Where(s => s.FullName == fieldValue.GetType().FullName).FirstOrDefault();
                        return ProcessField(recordSchema, fieldValue);
                    }
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(ArraySchema)))
                    {
                        return ProcessField(unionSchema.Schemas.OfType<ArraySchema>().FirstOrDefault(), fieldValue);
                    }
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(MapSchema)))
                    {
                        return ProcessField(unionSchema.Schemas.OfType<MapSchema>().FirstOrDefault(), fieldValue);
                    }
                    break;
                case NullSchema nullSchema:
                    break;
            }

            return null;
        }

        private static IEnumerable<Type> GetAllKnownTypes(Type type, HashSet<Type> knownTypes = null)
        {
            if (knownTypes == null)
            {
                knownTypes = new HashSet<Type>();
            }


            foreach (var item in type.GetTypeInfo()
                .GetCustomAttributes(true)
                .OfType<KnownTypeAttribute>()
                .Select(a => a.Type))
            {
                if (knownTypes.Add(item))
                {
                    GetAllKnownTypes(item, knownTypes);
                }
            }

            return knownTypes;
        }
    }
}
