# AvroCadabra

A tiny library to convert a user-defined C# object to an [AvroRecord](<https://docs.microsoft.com/en-us/previous-versions/azure/reference/dn627309(v%3Dazure.100)>) and back.

Useful when you need to use Avro to serialize your DTOs (data transfer objects) but can't afford to or not allowed to redesign just to make it work.

Another is for instances when you only want to serialize certain properties of your model, which can be driven by the schema provided.

## Syntax:

```csharp
// To convert an object instance to AvroRecord
myObject.ToAvroRecord(schema);

// To convert an AvroRecord to an object instance
avroRecord.FromAvroRecord<MyObjectClass>();

// or if you have a newer schema
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

## Custom Field Processing

For instances where you need to extract values from your object beyond the usual way of exposing them via public property getters when serializing, and/or assigning them back to your object via ways other than through public property setters when deserializing e.g. calling a method or assigning them to private fields using .NET reflection.

### Example:

```csharp

using System;
using System.Text;
using Gooseman.Avro.Utility;
using Microsoft.Hadoop.Avro.Schema;

namespace TestAvro
{
    class Program
    {
        static void Main(string[] args)
        {
            var schema = 
                @"{
                    ""type"": ""record"",
                    ""name"": ""TestAvro.SecretMessage"",
                    ""fields"": [
                        {
                            ""name"": ""_id"",
                            ""type"": ""string""
                        },
                        {
                            ""name"": ""Message"",
                            ""type"": ""string""
                        }
                    ]
                }";

            var secretMessage = new SecretMessage { Message = "Hello There!" };
            var avro = secretMessage.ToAvroRecord(schema, new SecretMessageValueGetter());
            var secretMessageReveal = avro.FromAvroRecord<SecretMessage>(customValueSetter: new SecretMessageValueSetter());

            Console.WriteLine($"Original Message: {secretMessage.Message} \r\nSent Message: {avro[1]} \r\nReceived Message: {secretMessageReveal.Message}");
            Console.ReadLine();
        }
    }

    public class SecretMessage
    {
        private Guid _id = Guid.NewGuid();
        public Guid Id => _id;
        public string Message { get; set; }
    }

    public class SecretMessageValueGetter : ICustomValueGetter
    {
        public object GetValue(object managedObject, string member)
        {
            switch (member)
            {
                case "_id":
                    return managedObject.GetFieldValue(member);
                case "Message":
                    var sm = ((dynamic) managedObject).Message;
                    return Convert.ToBase64String(Encoding.Default.GetBytes(sm));
                default:
                    return null;
            }
        }
    }

    public class SecretMessageValueSetter : ICustomValueSetter
    {
        public bool SetValue(object managedObject, string member, object value)
        {
            switch (member)
            {
                case "_id":
                    managedObject.SetFieldValue(member, value);
                    return true;
                case "Message":
                    managedObject.SetPropertyValue(member,
                        Encoding.Default.GetString(Convert.FromBase64String(value.ToString())));
                    return true;
            }

            return false;
        }
    }
}

```
### Result:
```
Original Message: Hello There!
Sent Message: SGVsbG8gVGhlcmUh
Received Message: Hello There!
```

## Available in Nuget

```
PM> Install-Package Avro.Cadabra.Core
```
