# AvroCadabra

A tiny library to convert a user-defined C# object to an [AvroRecord](<https://docs.microsoft.com/en-us/previous-versions/azure/reference/dn627309(v%3Dazure.100)>) and back.

Useful when you need to use Avro to serialize your DTOs (data transfer objects) but can't afford to decorate it with attributes that may be required for Avro to work.

Another is for instances when you only want to serialize certain properties of your model which is driven by the schema provided.

## Syntax:

```csharp
// To convert an object instance to AvroRecord
myObject.ToAvroRecord(schema);

// To convert an AvroRecord to an object instance
avroRecord.FromAvroRecord<MyObjectClass>();

// or if you a newer schema
avroRecord.FromAvroRecord<MyObjectClass>(newerSchema);
```

### Example:

```csharp
public class ShapeBasket
{
    public IList<IShape> Shapes { get; set; }
}

void main()
{
    var instance = new ShapeBasket
    {
        Shapes = new List<IShape>
        {
            new Circle { Name = "Red Dot", Radius = 15, Color = BasicColor.Red },
            new Square { Name = "Blue Square", Width = 20, Color = BasicColor.Blue }
        }
    };

    var avroFile = Path.GetTempFileName();

    // assuming you have a schema stored as a resource
    var schema_v1 = Encoding.Default.GetString(Resources.ShapeBasket_v1_0);

    // serialize to file
    using (var fs = new FileStream(avroFile, FileMode.Create))
    {
        using var writer = AvroContainer.CreateGenericWriter(schema_v1, fs, Codec.Deflate);
        using var sequentialWriter = new SequentialWriter<object>(writer, 1);

        // convert the instance to an AvroRecord
        sequentialWriter.Write(instance.ToAvroRecord(schema));
    }

    // assuming you want to deserialize using an updated schema
    var schema_v2 = Encoding.Default.GetString(Resources.ShapeBasket_v2_0);

    // deserialize to target type
    var target = new ShapeBasket();
    using (var fs = new FileStream(avroFile, FileMode.Open))
    {
        using var reader = AvroContainer.CreateGenericReader(fs);
        using var sequentialReader = new SequentialReader<object>(reader);

        // convert the AvroRecord to the actual instance
        target = sequentialReader.Objects.Cast<AvroRecord>().Select(r => r.FromAvroRecord<ShapeBasket>()).FirstOrDefault();
    }
}
```

## Custom Field Processor

For instances when you need to serialize the private state of your objects (TF?! IKR?) or do pre field processing before serializing/deserializing.

### Example:

```csharp

using System;
using System.Reflection;
using System.Text;
using Gooseman.Avro.Utility;
using Microsoft.Hadoop.Avro.Schema;

namespace TestAvro
{
    class Program
    {
        static void Main(string[] args)
        {
            var schema = "{\"type\":\"record\",\"name\":\"TestAvro.SecretMessage\",\"fields\":[{\"name\":\"_id\",\"type\":\"string\"},{\"name\":\"Message\",\"type\":\"string\"}]}";
            var secretMessage = new SecretMessage { Message = "Hello There!" };
            var secretMessageFieldProcessor = new SecretMessageFieldProcessor();
            var avro = secretMessage.ToAvroRecord(schema, secretMessageFieldProcessor);
            var secretMessageReveal = avro.FromAvroRecord<SecretMessage>(customFieldProcessor: secretMessageFieldProcessor);

            Console.WriteLine($"Message: {secretMessage.Message}, Encrypted Message: {avro[1]}, Restored Message: {secretMessageReveal.Message}");
            Console.ReadLine();
        }
    }

    public class SecretMessage
    {
        private Guid _id = Guid.NewGuid();
        public Guid Id => _id;
        public string Message { get; set; }
    }

    public class SecretMessageFieldProcessor : BaseCustomFieldProcessor
    {
        public override MemberInfo GetMatchingMemberInfo<T>(RecordField recordField)
        {
            return recordField.Name == "_id"
                ? typeof(T).GetField(recordField.Name, BindingFlags.NonPublic | BindingFlags.Instance)
                : base.GetMatchingMemberInfo<T>(recordField);
        }

        public override object PreFieldSerialization<T>(T obj, string fieldName)
        {
            switch (fieldName)
            {
                case "_id":
                    var id = obj.GetFieldValue<T, Guid>(fieldName);
                    return id;
                case "Message":
                    var sm = ((dynamic)obj).Message;
                    return Convert.ToBase64String(Encoding.Default.GetBytes(sm));
                default:
                    return base.PreFieldSerialization(obj, fieldName);
            }
        }

        public override object PreFieldDeserialization(string fieldName, object value)
        {
            switch (fieldName)
            {
                case "Message":
                    return Encoding.Default.GetString(Convert.FromBase64String(value.ToString()));
                default:
                    return base.PreFieldDeserialization(fieldName, value);
            }
        }
    }
}

```

## Available in Nuget

```
PM> Install-Package Avro.Cadabra.Core
```
