using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace avro_helper_master
{
    public static class AvroHelper
    { 
        private static object GetValue(this object obj, RecordField field)
        {
            return obj.GetType().GetProperty(field.Name).GetValue(obj);
        }

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
                    object avroMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(mapSchema.KeySchema.RuntimeType, mapSchema.ValueSchema.RuntimeType));
                    foreach (DictionaryEntry value in (IDictionary)fieldValue)
                    {
                        avroMap.GetType().GetMethod("Add").Invoke(avroMap, new[] { value.Key.ToString(), ProcessField(mapSchema.ValueSchema, value.Value) });
                    }
                    return avroMap;
                case UnionSchema unionSchema:
                    if (unionSchema.Schemas.Any(s => s.GetType() == typeof(RecordSchema)))
                    {
                        return ProcessField(unionSchema.Schemas.OfType<RecordSchema>().FirstOrDefault(), fieldValue);
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
    }
}
