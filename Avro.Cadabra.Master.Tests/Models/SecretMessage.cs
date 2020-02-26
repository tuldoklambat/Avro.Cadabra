// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;
using System.Text;

namespace Gooseman.Avro.Utility.Tests.Models
{
    public class SecretMessage
    {
        private Guid _id = Guid.NewGuid();
        public Guid Id => _id;
        public string Message { get; set; }
    }

    public class SecretMessageValueGetter : ICustomValueGetter
    {
        public object GetValue(object managedObject, string member)
        {
            switch (member)
            {
                case "_id":
                    return managedObject.GetFieldValue(member);
                case "Message":
                    var sm = ((dynamic) managedObject).Message;
                    return Convert.ToBase64String(Encoding.Default.GetBytes(sm));
                default:
                    return null;
            }
        }
    }

    public class SecretMessageValueSetter : ICustomValueSetter
    {
        public bool SetValue(object managedObject, string member, object value)
        {
            switch (member)
            {
                case "_id":
                    managedObject.SetFieldValue(member, value);
                    return true;
                case "Message":
                    managedObject.SetPropertyValue(member,
                        Encoding.Default.GetString(Convert.FromBase64String(value.ToString())));
                    return true;
            }

            return false;
        }
    }
}