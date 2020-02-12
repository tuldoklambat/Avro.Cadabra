using Microsoft.Hadoop.Avro;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace avro_helper_master_test.Model
{
    [DataContract]
    public class Sample
    {
        [DataMember]
        public DateTime MyDate { get; set; }

        [DataMember]
        [NullableSchema]
        public List<Sample> Samples { get; set; }
    }
}
