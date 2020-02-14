// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.

namespace Gooseman.Avro.Utility.Tests.Models
{
    public interface IShape
    {
        string Name { get; set; }

        // Since version 2.0
        string Tag { get; set; }
    }
}