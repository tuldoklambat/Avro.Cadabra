using System;
using System.Reflection;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class TradeRequestFieldProcessor : BaseCustomFieldProcessor
    {
        #region Overrides of BaseCustomFieldProcessor

        public override MemberInfo GetMatchingMemberInfo<T>(RecordField recordField)
        {
            var type = typeof(T);

            if (type == typeof(Tenor) && recordField.Name == "_tenorInMonths"
                || type == typeof(Vanilla) && recordField.Name == "_tenor")
            {
                return type.GetField(recordField.Name, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return base.GetMatchingMemberInfo<T>(recordField);
        }

        public override object PreFieldSerialization<T>(T obj, string fieldName)
        {
            var result = base.PreFieldSerialization(obj, fieldName);
            var type = typeof(T);

            if (type == typeof(Vanilla))
            {
                switch (fieldName)
                {
                    case "_tenor":
                        result = obj.GetFieldValue<T, Tenor>(fieldName);
                        break;
                    case "ExpiryDate":
                        // bypass ExpiryDate getter to prevent throwing an exception because tenor.Resolve() is not called
                        // and effectively getting the value of ExpiryDate if its actually assigned
                        result = obj.GetFieldValue<T, DateTime>("_expiryDate");
                        break;
                }
            }

            if (type == typeof(Tenor))
            {
                switch (fieldName)
                {
                    case "_tenorInMonths":
                        result = obj.GetFieldValue<T, int>(fieldName);
                        break;
                }
            }

            return result;
        }

        #endregion

    }
}
