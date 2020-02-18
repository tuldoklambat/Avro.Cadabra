using System;
using System.Reflection;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class TradeRequestFieldProcessor : BaseCustomFieldProcessor
    {
        #region Overrides of BaseCustomFieldProcessor

        public override MemberInfo GetMatchingMemberInfo(Type type, RecordField recordField)
        {
            if (type == typeof(Tenor) && recordField.Name == "_tenorInMonths"
                || type == typeof(Vanilla) && recordField.Name == "_tenor")
            {
                return type.GetField(recordField.Name, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return base.GetMatchingMemberInfo(type, recordField);
        }

        public override object PreFieldSerialization(object obj, string fieldName)
        {
            var result = base.PreFieldSerialization(obj, fieldName);
            var type = obj.GetType();

            if (type == typeof(Vanilla))
            {
                switch (fieldName)
                {
                    case "_tenor":
                        result = obj.GetFieldValue(fieldName);
                        break;
                    case "ExpiryDate":
                        // bypass ExpiryDate getter to prevent throwing an exception because tenor.Resolve() is not called
                        // and effectively getting the value of ExpiryDate if its actually assigned
                        result = obj.GetFieldValue("_expiryDate");
                        break;
                }
            }

            if (type == typeof(Tenor))
            {
                switch (fieldName)
                {
                    case "_tenorInMonths":
                        result = obj.GetFieldValue(fieldName);
                        break;
                }
            }

            return result;
        }

        #endregion

    }
}
