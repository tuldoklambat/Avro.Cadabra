// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gooseman.Avro.Utility.Tests.Models;
using Gooseman.Avro.Utility.Tests.Properties;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using NUnit.Framework;

namespace Gooseman.Avro.Utility.Tests
{
    public class SerializationTests
    {
        [Test]
        public void Test_Serialization()
        {
            var schema = Encoding.Default.GetString(Resources.SalesRecord);
            var avroFile = Path.ChangeExtension(Path.GetTempFileName(), "avro");

            using (var fs = new FileStream(avroFile, FileMode.Create))
            using (var writer = AvroContainer.CreateGenericWriter(schema, fs, Codec.Deflate))
            using (var sequentialWriter = new SequentialWriter<object>(writer, 1000))
            using (var sr = new StreamReader("1000 Sales Records.csv"))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    var fields = sr.ReadLine()?.Split(',');
                    var salesRecord = new SalesRecord
                    {
                        Region = fields[0],
                        Country = fields[1],
                        ItemType = fields[2],
                        SalesChannel = Enum.Parse<SalesChannelType>(fields[3]),
                        OrderPriority = Enum.Parse<OrderPriorityType>(fields[4]),
                        OrderDate = DateTime.Parse(fields[5]),
                        OrderId = int.Parse(fields[6]),
                        ShipDate = DateTime.Parse(fields[7]),
                        UnitsSold = int.Parse(fields[8]),
                        UnitPrice = double.Parse(fields[9]),
                        UnitCost = double.Parse(fields[10]),
                        TotalRevenue = double.Parse(fields[11]),
                        TotalCost = double.Parse(fields[12]),
                        TotalProfit = double.Parse(fields[13])
                    };
                    sequentialWriter.Write(salesRecord.ToAvroRecord(schema));
                }
            }

            var salesRecords = new List<SalesRecord>();

            using (var fs = new FileStream(avroFile, FileMode.Open))
            using (var reader = AvroContainer.CreateGenericReader(fs))
            using (var sequentialReader = new SequentialReader<object>(reader))
            {
                salesRecords.AddRange(sequentialReader.Objects.Cast<AvroRecord>()
                    .Select(o => o.FromAvroRecord<SalesRecord>(schema)));
            }

            File.Delete(avroFile);
        }
    }
}
