using System.Runtime.Serialization;

namespace avro_helper_master_test.Model
{
    [DataContract]
    public class Simple : ISimple
    {
        [DataMember]
        public bool Boolean { get; set; }
        [DataMember]
        public int Int { get; set; }
        [DataMember]
        public long Long { get; set; }
        [DataMember]
        public double Double { get; set; }
        [DataMember]
        public float Float { get; set; }
        [DataMember]
        public string String { get; set; }
        [DataMember]
        public Fruit Fruit { get; set; }
        [DataMember]
        public int Id { get; set; }
    }

    public enum Fruit
    {
        Apple,
        Orange
    }
}
