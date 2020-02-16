using System.Reflection;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility
{
    public interface ICustomFieldProcessor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recordField"></param>
        /// <returns></returns>
        MemberInfo GetMatchingMemberInfo<T>(RecordField recordField);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instanceObj"></param>
        /// <param name="recordField"></param>
        /// <returns></returns>
        object GetValue<T>(T instanceObj, RecordField recordField);
    }
}
