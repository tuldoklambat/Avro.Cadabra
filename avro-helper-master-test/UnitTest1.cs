using avro_helper_master;
using avro_helper_master_test.Model;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using Microsoft.Hadoop.Avro.Schema;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace avro_helper_master_test
{
    public class Tests
    {
        private Simple _simple;
        private string _avroFile;

        [SetUp]
        public void Setup()
        {
            _simple = new Simple
            {
                Id = 1,
                Boolean = true,
                Int = int.MinValue,
                Long = long.MinValue,
                Double = double.MinValue,
                Float = float.MinValue,
                String = "The big brown fox jump over the lazy dog",
                Fruit = Fruit.Apple
            };

            _avroFile = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(_avroFile);
        }

        [Test]
        public void Test_Simple_Model_Serialization()
        {
            var schema = "{\"type\":\"record\",\"name\":\"Simple\",\"fields\":[{\"name\":\"Id\",\"type\":\"int\"},{\"name\":\"Boolean\",\"type\":\"boolean\"},{\"name\":\"Int\",\"type\":\"int\"},{\"name\":\"Long\",\"type\":\"long\"},{\"name\":\"Double\",\"type\":\"double\"},{\"name\":\"Float\",\"type\":\"float\"},{\"name\":\"String\",\"type\":\"string\"},{\"name\":\"Fruit\",\"type\":{\"type\":\"enum\",\"name\":\"Fruit\",\"symbols\":[\"Apple\",\"Orange\"]}}]}\r\n\r\n";
            
            using (var fs = new FileStream(_avroFile, FileMode.Create))
            using (var writer = AvroContainer.CreateGenericWriter(schema, fs, Codec.Deflate))
            using (var sequentialWriter = new SequentialWriter<object>(writer, 1))
            {
                sequentialWriter.Write(_simple.ToAvroRecord((RecordSchema)AvroSerializer.CreateGeneric(schema).WriterSchema));
            }
        }

        [Test]
        public void Test_Complex_Model_Serialization()
        {
            var schema = "{\"type\":\"record\",\"name\":\"avro_helper_master_test.Model.Complex\",\"fields\":[{\"name\":\"Simple\",\"type\":[{\"type\":\"record\",\"name\":\"avro_helper_master_test.Model.Simple\",\"fields\":[{\"name\":\"Boolean\",\"type\":\"boolean\"},{\"name\":\"Int\",\"type\":\"int\"},{\"name\":\"Long\",\"type\":\"long\"},{\"name\":\"Double\",\"type\":\"double\"},{\"name\":\"Float\",\"type\":\"float\"},{\"name\":\"String\",\"type\":\"string\"},{\"name\":\"Fruit\",\"type\":{\"type\":\"enum\",\"name\":\"avro_helper_master_test.Model.Fruit\",\"symbols\":[\"Apple\",\"Orange\"]}},{\"name\":\"Id\",\"type\":\"int\"}]},{\"type\":\"record\",\"name\":\"avro_helper_master_test.Model.AnotherSimple\",\"fields\":[{\"name\":\"Id\",\"type\":\"int\"}]}]},{\"name\":\"Simpletons\",\"type\":[{\"type\":\"array\",\"items\":[\"avro_helper_master_test.Model.Simple\",\"avro_helper_master_test.Model.AnotherSimple\"]},{\"type\":\"array\",\"items\":\"avro_helper_master_test.Model.Simple\"}]},{\"name\":\"SimpletonsMap\",\"type\":[{\"type\":\"map\",\"values\":[\"avro_helper_master_test.Model.Simple\",\"avro_helper_master_test.Model.AnotherSimple\"]}]}]}";
            var complex = new Complex
            {
                Simple = _simple,
                Simpletons = new List<ISimple>
                {
                    new Simple
                    {
                        Id = 2,
                        Boolean = false,
                        Int = int.MaxValue,
                        Long = long.MaxValue,
                        Double = double.MaxValue,
                        Float = float.MaxValue,
                        String = "The big brown fox jump over the lazy dog".ToUpper(),
                        Fruit = Fruit.Orange
                    },
                    new Simple
                    {
                        Id = 4,
                        Boolean = false,
                        Int = int.MaxValue,
                        Long = long.MaxValue,
                        Double = double.MaxValue,
                        Float = float.MaxValue,
                        String = "The big brown fox jump over the lazy dog".ToUpper(),
                        Fruit = Fruit.Orange
                    }
                },
                SimpletonsMap = new Dictionary<string, ISimple>
                {
                    {
                        "First", 
                        new Simple
                        {
                            Id = 3,
                            Boolean = false,
                            Int = int.MaxValue,
                            Long = long.MaxValue,
                            Double = double.MaxValue,
                            Float = float.MaxValue,
                            String = "The big brown fox jump over the lazy dog".ToLower(),
                            Fruit = Fruit.Orange
                        }
                    },
                    {
                        "Second",
                        new Simple
                        {
                            Id = 4,
                            Boolean = false,
                            Int = int.MaxValue,
                            Long = long.MaxValue,
                            Double = double.MaxValue,
                            Float = float.MaxValue,
                            String = "The big brown fox jump over the lazy dog".ToLower(),
                            Fruit = Fruit.Orange
                        }
                    }

                }
            };

            using (var fs = new FileStream(_avroFile, FileMode.Create))
            using (var writer = AvroContainer.CreateGenericWriter(schema, fs, Codec.Deflate))
            using (var sequentialWriter = new SequentialWriter<object>(writer, 1))
            {
                var record = complex.ToAvroRecord((RecordSchema)AvroSerializer.CreateGeneric(schema).WriterSchema);
                sequentialWriter.Write(record);
            }

            using (var fs = new FileStream(_avroFile, FileMode.Open))
            using (var reader = AvroContainer.CreateReader<Complex>(fs))
            using (var sequentialReader = new SequentialReader<Complex>(reader))
            {
                var restored = sequentialReader.Objects.ToList();
            }
        }

        [Test]
        public void Test_Sample_Model_Serialization()
        {
            var schema = "{\"type\":\"record\",\"name\":\"avro_helper_master_test.Model.Sample\",\"fields\":[{\"name\":\"MyDate\",\"type\":\"long\"},{\"name\":\"Samples\",\"type\":[\"null\",{\"type\":\"array\",\"items\":\"avro_helper_master_test.Model.Sample\"}]}]}";
            var sample = new Sample
            {
                MyDate = DateTime.Today,
                Samples = new List<Sample>
                {
                    { new Sample { MyDate = DateTime.Today.AddMonths(1) } },
                    { new Sample { MyDate = DateTime.Today.AddMonths(2) } },
                    { new Sample { MyDate = DateTime.Today.AddMonths(3) } },
                }
            };
            using (var fs = new FileStream(_avroFile, FileMode.Create))
            using (var writer = AvroContainer.CreateGenericWriter(schema, fs, Codec.Deflate))
            using (var sequentialWriter = new SequentialWriter<object>(writer, 1))
            {
                sequentialWriter.Write(sample.ToAvroRecord((RecordSchema)AvroSerializer.CreateGeneric(schema).WriterSchema));
            }

            using (var fs = new FileStream(_avroFile, FileMode.Open))
            using (var reader = AvroContainer.CreateReader<Sample>(fs))
            using (var sequentialReader = new SequentialReader<Sample>(reader))
            {
                var restored = sequentialReader.Objects.ToList();
            }

        }
    }
}