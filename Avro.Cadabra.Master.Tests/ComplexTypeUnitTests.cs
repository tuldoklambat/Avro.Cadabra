﻿// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using Gooseman.Avro.Utility.Tests.Models;
using Gooseman.Avro.Utility.Tests.Properties;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gooseman.Avro.Utility.Tests.Models.Complex;

namespace Gooseman.Avro.Utility.Tests
{
    public class ComplexTypeUnitTests
    {
        [TestCase(true, Description = "Complex Type Conversion With Schema")]
        [TestCase(false, Description = "Complex Type Conversion Without Schema")]
        [Test]
        public void Test_ComplexType_Conversion(bool withSchema)
        {
            dynamic instance = new ShapeBasket
            {
                Shapes = new List<IShape>
                {
                    new Circle(),
                    new Circle {Name = "Red Dot", Radius = 15, Color = BasicColor.Red},
                    new Square {Name = "Blue Square", Width = 20, Color = BasicColor.Blue},
                    new Triangle
                        {Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo},
                    new StrangeShape
                    {
                        Name = "Blob",
                        ChildShape = new StrangeShape
                            {Name = "Child Blob", ChildShape = new Square {Name = "Child Square", Width = 50}}
                    }
                }
            };

            var schema = Resources.ShapeBasket_v1_0;

            var convertedInstance = withSchema
                ? ((object) instance).ToAvroRecord(schema)
                : ((object) instance).ToAvroRecord();

            dynamic target = withSchema
                ? convertedInstance.FromAvroRecord<ShapeBasket>(schema)
                : convertedInstance.FromAvroRecord<ShapeBasket>();

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
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width,
                target.Shapes[4].ChildShape.ChildShape.Width);
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
                    new Circle {Name = "Red Dot", Radius = 15, Color = BasicColor.Red},
                    new Square {Name = "Blue Square", Width = 20, Color = BasicColor.Blue},
                    new Triangle
                        {Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo},
                    new StrangeShape
                    {
                        Name = "Blob",
                        ChildShape = new StrangeShape
                            {Name = "Child Blob", ChildShape = new Square {Name = "Child Square", Width = 50}}
                    }
                }
            };

            var schemaV1 = Resources.ShapeBasket_v1_0;
            var schemaV2 = Resources.ShapeBasket_v2_0;

            // encode using v1 schema
            var convertedInstance = ((object) instance).ToAvroRecord(schemaV1);

            // restore using v2 schema
            dynamic target = convertedInstance.FromAvroRecord<ShapeBasket>(schemaV2);

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
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width,
                target.Shapes[4].ChildShape.ChildShape.Width);
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
                    new Circle {Name = "Red Dot", Radius = 15, Color = BasicColor.Red, Tag = "RD"},
                    new Square {Name = "Blue Square", Width = 20, Color = BasicColor.Blue, Tag = "BS"},
                    new Triangle
                    {
                        Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo,
                        Tag = "BT"
                    },
                    new StrangeShape
                    {
                        Name = "Blob",
                        ChildShape = new StrangeShape
                        {
                            Name = "Child Blob",
                            ChildShape = new Square {Name = "Child Square", Width = 50, Tag = "CS"}, Tag = "CB"
                        },
                        Tag = "B"
                    }
                }
            };

            var schemaV1 = Resources.ShapeBasket_v1_0;
            var schemaV2 = Resources.ShapeBasket_v2_0;

            // encode using v2 schema
            var convertedInstance = ((object) instance).ToAvroRecord(schemaV2);

            // restore using v1 schema
            dynamic target = convertedInstance.FromAvroRecord<ShapeBasket>(schemaV1);

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
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width,
                target.Shapes[4].ChildShape.ChildShape.Width);
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
                    new Circle {Name = "Red Dot", Radius = 15, Color = BasicColor.Red},
                    new Square {Name = "Blue Square", Width = 20, Color = BasicColor.Blue},
                    new Triangle
                        {Name = "Bermuda Triangle", SideA = 10, SideB = 20, SideC = 30, Color = BasicColor.Indigo},
                    new StrangeShape
                    {
                        Name = "Blob",
                        ChildShape = new StrangeShape
                            {Name = "Child Blob", ChildShape = new Square {Name = "Child Square", Width = 50}}
                    }
                }
            };

            var schema = Resources.ShapeBasket_v1_0;
            var avroFile = Path.GetTempFileName();

            // serialize
            using (var fs = new FileStream(avroFile, FileMode.Create))
            {
                using var writer = AvroContainer.CreateGenericWriter(schema, fs, Codec.Deflate);
                using var sequentialWriter = new SequentialWriter<object>(writer, 1);
                sequentialWriter.Write(((object) instance).ToAvroRecord(schema));
            }

            dynamic target;
            // deserialize
            using (var fs = new FileStream(avroFile, FileMode.Open))
            {
                using var reader = AvroContainer.CreateGenericReader(fs);
                using var sequentialReader = new SequentialReader<object>(reader);
                target = sequentialReader.Objects.Cast<AvroRecord>().Select(r => r.FromAvroRecord<ShapeBasket>())
                    .FirstOrDefault();
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
            Assert.AreEqual(instance.Shapes[4].ChildShape.ChildShape.Width,
                target.Shapes[4].ChildShape.ChildShape.Width);
        }

        [Test]
        public void Test_Custom_Field_Processor()
        {
            /*
              The following is a contrive example of doing Avro serialization to an 
              existing DTO/model with some special requirements and cannot be modified
             */

            /*
             Scenario: We are sending a trade request down the wire with fields that can be set but not retrieve
             and which has impact to non-pure properties e.g. throws an exception if a method is not called before
             retrieving the property value
             */

            var schema = Resources.TradeRequest;

            var trade = new Vanilla();
            var tenor = new Tenor(3);
            trade.SetTenor(tenor);

            var tradeRequest = new TradeRequest
            {
                Trade = trade
            };

            /*
            for some reason tradeRequest.ApplyBinding(); cannot be called by the client sending the request
            so weed need to have a way to maintain the state of the request when sending hence the ICustomFieldProcessor
             */

            var avroRequest = tradeRequest.ToAvroRecord(schema, new TradeRequestValueGetter());

            // assume we send the message on the wire, server receives and restores
            // the message using the same CustomFieldProcessor

            var receivedTradeRequest =
                avroRequest.FromAvroRecord<TradeRequest>(customValueSetter: new TradeRequestValueSetter());
            receivedTradeRequest.ApplyBinding();

            // test if the receive expiry date is 3 months from now
            Assert.AreEqual(((Vanilla) receivedTradeRequest.Trade).ExpiryDate.Date, DateTime.Now.AddMonths(3).Date);
        }

        [Test]
        public void Test_Another_Custom_Field_Processor()
        {
            var schema = Resources.SecretMessage;

            var secretMessage = new SecretMessage {Message = "Hello There!"};
            var avro = secretMessage.ToAvroRecord(schema, new SecretMessageValueGetter());
            var secretMessageReveal =
                avro.FromAvroRecord<SecretMessage>(customValueSetter: new SecretMessageValueSetter());

            // check if the message is encrypted
            Assert.AreNotEqual(Convert.ToString(avro["Message"]), secretMessage.Message);

            // check if restored message is same
            Assert.AreEqual(secretMessage.Message, secretMessageReveal.Message);
        }
    }
}