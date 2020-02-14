﻿// Copyright (c) Joseph De Guzman (tuldoklambat@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.

using Gooseman.Avro.Utility.Tests.Models;
using Gooseman.Avro.Utility.Tests.Properties;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gooseman.Avro.Utility.Tests
{
    public class PrimitiveTypeUnitTests
    {
        private readonly Dictionary<Type, object> _primitiveTypeModels = new Dictionary<Type, object>
        {
            {
                typeof(string),
                new GenericType<string>
                {
                    GenericInstance = "Lorem ipsum dolor sit amet",
                    GenericArray = new[] { "consectetur adipiscing elit", "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua" },
                    GenericList = new List<string>
                    {
                        "Ut enim ad minim veniam",
                        "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat"
                    },
                    GenericMap = new Dictionary<string, string>
                    {
                        { "1", "Duis aute irure dolor"},
                        { "2", "in reprehenderit in voluptate" }
                    }
                }
            },
            {
                typeof(int),
                new GenericType<int>
                {
                    GenericInstance = int.MaxValue,
                    GenericArray = new[] { int.MinValue, int.MaxValue },
                    GenericList = new List<int> { int.MinValue, int.MaxValue },
                    GenericMap = new Dictionary<string, int> { { "1", int.MinValue }, { "2", int.MaxValue } }
                }
            },
            {
                typeof(long),
                new GenericType<long>
                {
                    GenericInstance = long.MaxValue,
                    GenericArray = new[] { long.MinValue, long.MaxValue },
                    GenericList = new List<long> { long.MinValue, long.MaxValue },
                    GenericMap = new Dictionary<string, long> { { "1", long.MinValue }, { "2", long.MaxValue } }
                }
            },
            {
                typeof(float),
                new GenericType<float>
                {
                    GenericInstance = float.MaxValue,
                    GenericArray = new[] { float.MinValue, float.MaxValue },
                    GenericList = new List<float> { float.MinValue, float.MaxValue },
                    GenericMap = new Dictionary<string, float> { { "1", float.MinValue }, { "2", float.MaxValue } }
                }
            },
            {
                typeof(double),
                new GenericType<double>
                {
                    GenericInstance = double.MaxValue,
                    GenericArray = new[] { double.MinValue, double.MaxValue },
                    GenericList = new List<double> { double.MinValue, double.MaxValue },
                    GenericMap = new Dictionary<string, double> { { "1", double.MinValue }, { "2", double.MaxValue } }
                }
            },
            {
                typeof(bool),
                new GenericType<bool>
                {
                    GenericInstance = true,
                    GenericArray = new[] { true, false },
                    GenericList = new List<bool> { true, false },
                    GenericMap = new Dictionary<string, bool> { { "1", true }, { "2", false } }
                }
            },
            {
                typeof(DateTime),
                new GenericType<DateTime>
                {
                    GenericInstance = DateTime.Now,
                    GenericArray = new[] { DateTime.Now.AddMonths(1), DateTime.Now.AddMonths(3) },
                    GenericList = new List<DateTime> { DateTime.Now.AddMonths(1), DateTime.Now.AddMonths(3) },
                    GenericMap = new Dictionary<string, DateTime> { { "1", DateTime.Now.AddMonths(1) }, { "2", DateTime.Now.AddMonths(3) } }
                }
            },
        };

        [TestCase(typeof(string), "string", TestName = "String Conversion Test")]
        [TestCase(typeof(int), "int", TestName = "Integer Conversion Test")]
        [TestCase(typeof(long), "long", TestName = "Long Conversion Test")]
        [TestCase(typeof(float), "float", TestName = "Float Conversion Test")]
        [TestCase(typeof(double), "double", TestName = "Double Conversion Test")]
        [TestCase(typeof(bool), "boolean", TestName = "Boolean Conversion Test")]
        [TestCase(typeof(DateTime), "long", TestName = "DateTime Conversion Test")]
        [Test]
        public void Test_PrimitiveTypes_Conversion(Type type, string avroType)
        {
            dynamic instance = _primitiveTypeModels[type];
            var schema = Encoding.Default.GetString(Resources.GenericSchemaTemplate).Replace("{type}", avroType);

            // convert to AvroRecord
            var convertedInstance = ((object)instance).ToAvroRecord(schema);

            // convert back
            dynamic target = typeof(AvroCadabra).GetMethod("FromAvroRecord")
                .MakeGenericMethod(((object)instance).GetType())
                .Invoke(null, new object[] { convertedInstance, schema });

            // compare
            Assert.AreEqual(instance.GenericInstance, target.GenericInstance);
            Assert.AreEqual(instance.GenericArray[0], target.GenericArray[0]);
            Assert.AreEqual(instance.GenericArray[1], target.GenericArray[1]);
            Assert.AreEqual(instance.GenericList[0], target.GenericList[0]);
            Assert.AreEqual(instance.GenericList[1], target.GenericList[1]);
            Assert.AreEqual(instance.GenericMap["1"], target.GenericMap["1"]);
            Assert.AreEqual(instance.GenericMap["2"], target.GenericMap["2"]);
        }
    }
}