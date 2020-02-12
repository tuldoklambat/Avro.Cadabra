using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace avro_helper_master_test.Model
{
    [KnownType(typeof(Simple))]
    [KnownType(typeof(AnotherSimple))]
    [KnownType(typeof(List<ISimple>))]
    [KnownType(typeof(List<Simple>))]
    [KnownType(typeof(Dictionary<string, ISimple>))]
    [DataContract]
    public class Complex
    {
        [DataMember]
        public ISimple Simple { get; set; }
        [DataMember]
        public IList<ISimple> Simpletons { get; set; }
        [DataMember]
        public IDictionary<string, ISimple> SimpletonsMap { get; set; }
    }
}
