﻿// Copyright (c) Joseph De Guzman (tuldoklambat@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.

using System.Collections.Generic;

namespace Gooseman.Avro.Utility.Tests.Models
{
    public class GenericType<T>
    {
        public T GenericInstance { get; set; }
        public T[] GenericArray { get; set; }
        public IList<T> GenericList { get; set; }
        public IDictionary<string, T> GenericMap { get; set; }
    }
}