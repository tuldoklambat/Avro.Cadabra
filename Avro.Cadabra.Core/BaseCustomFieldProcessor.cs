// // Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// // All rights reserved.
// //
// // THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// // KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// // WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// // MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using System.Reflection;
using Microsoft.Hadoop.Avro.Schema;

namespace Gooseman.Avro.Utility
{
    public abstract class BaseCustomFieldProcessor
    {

        public virtual MemberInfo GetMatchingMemberInfo(Type type, RecordField recordField)
        {
            return null;
        }

        public virtual object PreFieldSerialization(object obj, string fieldName)
        {
            return null;
        }

        public virtual object PreFieldDeserialization(string fieldName, object value)
        {
            return value;
        }
    }
}
