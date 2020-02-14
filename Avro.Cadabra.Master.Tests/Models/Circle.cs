﻿// Copyright (c) Joseph De Guzman (tuldoklambat@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.

namespace Gooseman.Avro.Utility.Tests.Models
{
    public class Circle : IShape
    {
        public string Name { get; set; }

        public double Radius { get; set; }

        public BasicColor Color { get; set; }

        public string Tag { get; set; }
    }
}