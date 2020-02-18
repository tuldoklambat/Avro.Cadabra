using System;
using System.Reflection;
using System.Text;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility.Tests.Models
{
    public class SecretMessage
    {
        private Guid _id = Guid.NewGuid();
        public Guid Id => _id;
        public string Message { get; set; }
    }

    public class SecretMessageFieldProcessor : BaseCustomFieldProcessor
    {
        #region Overrides of BaseCustomFieldProcessor

        public override MemberInfo GetMatchingMemberInfo(Type type, RecordField recordField)
        {
            return recordField.Name == "_id" 
                ? type.GetField(recordField.Name, BindingFlags.NonPublic | BindingFlags.Instance) 
                : base.GetMatchingMemberInfo(type, recordField);
        }

        public override object PreFieldSerialization(object obj, string fieldName)
        {
            switch (fieldName)
            {
                case "_id":
                    return obj.GetFieldValue(fieldName);
                case "Message":
                    var sm = ((dynamic) obj).Message;
                    return Convert.ToBase64String(Encoding.Default.GetBytes(sm));
                default:
                    return base.PreFieldSerialization(obj, fieldName);
            }
        }

        public override object PreFieldDeserialization(string fieldName, object value)
        {
            switch (fieldName)
            {
                case "Message":
                    return Encoding.Default.GetString(Convert.FromBase64String(value.ToString()));
                default:
                    return base.PreFieldDeserialization(fieldName, value);
            }
        }

        #endregion

    }
}