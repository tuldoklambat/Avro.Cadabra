// Copyright (c) Joseph De Guzman (tuldoklambat@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.

using Gooseman.Avro.Utility.Tests.Models;
using Gooseman.Avro.Utility.Tests.Properties;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gooseman.Avro.Utility.Tests
{
    public class ComplexTypeUnitTests
    {
        [Test]
        public void Test_ComplexType_Conversion()
        {
            dynamic instance = new ShapeBasket
            {
                Shapes = new List<IShape>
                {
                    new Circle(),
                    new Circle { Name = "Red Dot", Radius = 15, Color = BasicColor.Red },
                    new Square { Name = "Blue Square", Width = 20, Color = BasicColor.Blue },
                    new Triangle { Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo },
                    new StrangeShape { Name = "Blob", ChildShape = new StrangeShape { Name = "Child Blob", ChildShape = new Square { Name = "Child Square", Width = 50 } } }
                }
            };

            var schema = Encoding.Default.GetString(Resources.ShapeBasket_v1_0);

            var convertedInstance = ((object)instance).ToAvroRecord(schema);

            dynamic target = convertedInstance.FromAvroRecord<ShapeBasket>(schema);

            Assert.AreEqual(instance.Shapes[0].Name, target.Shapes[0].Name);

            Assert.AreEqual(instance.Shapes[1].Name, target.Shapes[1].Name);
            Assert.AreEqual(instance.Shapes[1].Radius, target.Shapes[1].Radius);
            Assert.AreEqual(instance.Shapes[1].Color, target.Shapes[1].Color);

            Assert.AreEqual(instance.Shapes[2].Name, target.Shapes[2].Name);
            Assert.AreEqual(instance.Shapes[2].Width, target.Shapes[2].Width);
            Assert.AreEqual(instance.Shapes[2].Color, target.Shapes[2].Color);

            Assert.AreEqual(instance.Shapes[3].Name, target.Shapes[3].Name);
            Assert.AreEqual(instance.Shapes[3].SideA, target.Shapes[3].SideA);
            Assert.AreEqual(instance.Shapes[3].SideB, target.Shapes[3].SideB);
            Assert.AreEqual(instance.Shapes[3].SideC, target.Shapes[3].SideC);
            Assert.AreEqual(instance.Shapes[3].Color, target.Shapes[3].Color);

            Assert.AreEqual(instance.Shapes[4].Name, target.Shapes[4].Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.Name, target.Shapes[4].ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Name, target.Shapes[4].ChildShape.ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width, target.Shapes[4].ChildShape.ChildShape.Width);
        }

        [Test]
        public void Test_ComplexType_Schema_Evolution()
        {
            dynamic instance = new ShapeBasket
            {
                Shapes = new List<IShape>
                {
                    // the schema for Circle allows for Names and Radius to be null
                    new Circle(),
                    new Circle { Name = "Red Dot", Radius = 15, Color = BasicColor.Red },
                    new Square { Name = "Blue Square", Width = 20, Color = BasicColor.Blue },
                    new Triangle { Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo },
                    new StrangeShape { Name = "Blob", ChildShape = new StrangeShape { Name = "Child Blob", ChildShape = new Square { Name = "Child Square", Width = 50 } } }
                }
            };

            var schema_v1 = Encoding.Default.GetString(Resources.ShapeBasket_v1_0);
            var schema_v2 = Encoding.Default.GetString(Resources.ShapeBasket_v2_0);

            // encode using v1 schema
            var convertedInstance = ((object)instance).ToAvroRecord(schema_v1);

            // restore using v2 schema
            dynamic target = convertedInstance.FromAvroRecord<ShapeBasket>(schema_v2);

            Assert.AreEqual(instance.Shapes[0].Name, target.Shapes[0].Name);

            Assert.AreEqual(instance.Shapes[1].Name, target.Shapes[1].Name);
            Assert.AreEqual(instance.Shapes[1].Radius, target.Shapes[1].Radius);
            Assert.AreEqual(instance.Shapes[1].Color, target.Shapes[1].Color);

            Assert.AreEqual(instance.Shapes[2].Name, target.Shapes[2].Name);
            Assert.AreEqual(instance.Shapes[2].Width, target.Shapes[2].Width);
            Assert.AreEqual(instance.Shapes[2].Color, target.Shapes[2].Color);

            Assert.AreEqual(instance.Shapes[3].Name, target.Shapes[3].Name);
            Assert.AreEqual(instance.Shapes[3].SideA, target.Shapes[3].SideA);
            Assert.AreEqual(instance.Shapes[3].SideB, target.Shapes[3].SideB);
            Assert.AreEqual(instance.Shapes[3].SideC, target.Shapes[3].SideC);
            Assert.AreEqual(instance.Shapes[3].Color, target.Shapes[3].Color);

            Assert.AreEqual(instance.Shapes[4].Name, target.Shapes[4].Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.Name, target.Shapes[4].ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Name, target.Shapes[4].ChildShape.ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width, target.Shapes[4].ChildShape.ChildShape.Width);
        }

        [Test]
        public void Test_ComplexType_Schema_Evolution_Backward_Compatibility()
        {
            dynamic instance = new ShapeBasket
            {
                Shapes = new List<IShape>
                {
                    // the schema for Circle allows for Names and Radius to be null
                    new Circle(),
                    new Circle { Name = "Red Dot", Radius = 15, Color = BasicColor.Red, Tag = "RD" },
                    new Square { Name = "Blue Square", Width = 20, Color = BasicColor.Blue, Tag = "BS" },
                    new Triangle { Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo, Tag = "BT" },
                    new StrangeShape { Name = "Blob", ChildShape = new StrangeShape { Name = "Child Blob", ChildShape = new Square { Name = "Child Square", Width = 50, Tag = "CS" }, Tag = "CB" }, Tag = "B" }
                }
            };

            var schema_v1 = Encoding.Default.GetString(Resources.ShapeBasket_v1_0);
            var schema_v2 = Encoding.Default.GetString(Resources.ShapeBasket_v2_0);

            // encode using v2 schema
            var convertedInstance = ((object)instance).ToAvroRecord(schema_v2);

            // restore using v1 schema
            dynamic target = convertedInstance.FromAvroRecord<ShapeBasket>(schema_v1);

            Assert.AreEqual(instance.Shapes[0].Name, target.Shapes[0].Name);

            Assert.AreEqual(instance.Shapes[1].Name, target.Shapes[1].Name);
            Assert.AreEqual(instance.Shapes[1].Radius, target.Shapes[1].Radius);
            Assert.AreEqual(instance.Shapes[1].Color, target.Shapes[1].Color);
            Assert.IsTrue(string.IsNullOrEmpty(target.Shapes[1].Tag));

            Assert.AreEqual(instance.Shapes[2].Name, target.Shapes[2].Name);
            Assert.AreEqual(instance.Shapes[2].Width, target.Shapes[2].Width);
            Assert.AreEqual(instance.Shapes[2].Color, target.Shapes[2].Color);
            Assert.IsTrue(string.IsNullOrEmpty(target.Shapes[2].Tag));

            Assert.AreEqual(instance.Shapes[3].Name, target.Shapes[3].Name);
            Assert.AreEqual(instance.Shapes[3].SideA, target.Shapes[3].SideA);
            Assert.AreEqual(instance.Shapes[3].SideB, target.Shapes[3].SideB);
            Assert.AreEqual(instance.Shapes[3].SideC, target.Shapes[3].SideC);
            Assert.AreEqual(instance.Shapes[3].Color, target.Shapes[3].Color);
            Assert.IsTrue(string.IsNullOrEmpty(target.Shapes[3].Tag));

            Assert.AreEqual(instance.Shapes[4].Name, target.Shapes[4].Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.Name, target.Shapes[4].ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Name, target.Shapes[4].ChildShape.ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width, target.Shapes[4].ChildShape.ChildShape.Width);
            Assert.IsTrue(string.IsNullOrEmpty(target.Shapes[4].Tag));
            Assert.IsTrue(string.IsNullOrEmpty(target.Shapes[4].ChildShape.Tag));
            Assert.IsTrue(string.IsNullOrEmpty(target.Shapes[4].ChildShape.ChildShape.Tag));
        }

        [Test]
        public void Test_ComplexType_Serialization_To_Stream()
        {
            dynamic instance = new ShapeBasket
            {
                Shapes = new List<IShape>
                {
                    // the schema for Circle allows for Names and Radius to be null
                    new Circle(),
                    new Circle { Name = "Red Dot", Radius = 15, Color = BasicColor.Red },
                    new Square { Name = "Blue Square", Width = 20, Color = BasicColor.Blue },
                    new Triangle { Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo },
                    new StrangeShape { Name = "Blob", ChildShape = new StrangeShape { Name = "Child Blob", ChildShape = new Square { Name = "Child Square", Width = 50 } } }
                }
            };

            var schema = Encoding.Default.GetString(Resources.ShapeBasket_v1_0);
            var avroFile = Path.GetTempFileName();

            // serialize
            using (var fs = new FileStream(avroFile, FileMode.Create))
            {
                using var writer = AvroContainer.CreateGenericWriter(schema, fs, Codec.Deflate);
                using var sequentialWriter = new SequentialWriter<object>(writer, 1);
                sequentialWriter.Write(((object)instance).ToAvroRecord(schema));
            }

            dynamic target = new ShapeBasket();
            // deserialize
            using (var fs = new FileStream(avroFile, FileMode.Open))
            {
                using var reader = AvroContainer.CreateGenericReader(fs);
                using var sequentialReader = new SequentialReader<object>(reader);
                target = sequentialReader.Objects.Cast<AvroRecord>().Select(r => r.FromAvroRecord<ShapeBasket>()).FirstOrDefault();
            }

            File.Delete(avroFile);

            Assert.AreEqual(instance.Shapes[0].Name, target.Shapes[0].Name);

            Assert.AreEqual(instance.Shapes[1].Name, target.Shapes[1].Name);
            Assert.AreEqual(instance.Shapes[1].Radius, target.Shapes[1].Radius);
            Assert.AreEqual(instance.Shapes[1].Color, target.Shapes[1].Color);

            Assert.AreEqual(instance.Shapes[2].Name, target.Shapes[2].Name);
            Assert.AreEqual(instance.Shapes[2].Width, target.Shapes[2].Width);
            Assert.AreEqual(instance.Shapes[2].Color, target.Shapes[2].Color);

            Assert.AreEqual(instance.Shapes[3].Name, target.Shapes[3].Name);
            Assert.AreEqual(instance.Shapes[3].SideA, target.Shapes[3].SideA);
            Assert.AreEqual(instance.Shapes[3].SideB, target.Shapes[3].SideB);
            Assert.AreEqual(instance.Shapes[3].SideC, target.Shapes[3].SideC);
            Assert.AreEqual(instance.Shapes[3].Color, target.Shapes[3].Color);

            Assert.AreEqual(instance.Shapes[4].Name, target.Shapes[4].Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.Name, target.Shapes[4].ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Name, target.Shapes[4].ChildShape.ChildShape.Name);
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width, target.Shapes[4].ChildShape.ChildShape.Width);

        }
    }
}