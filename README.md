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

## Example:

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
