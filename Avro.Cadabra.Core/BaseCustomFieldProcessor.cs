using System.Reflection;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility
{
    public abstract class BaseCustomFieldProcessor
    {

        public virtual MemberInfo GetMatchingMemberInfo<T>(RecordField recordField)
        {
            return null;
        }

        public virtual object PreFieldSerialization<T>(T obj, string fieldName)
        {
            return null;
        }

        public virtual object PreFieldDeserialization(string fieldName, object value)
        {
            return value;
        }
    }
}
