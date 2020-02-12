using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace avro_helper_master_test.Model
{
    [DataContract]
    public class AnotherSimple : ISimple
    {
        [DataMember]
        public int Id { get; set; }
    }
}
