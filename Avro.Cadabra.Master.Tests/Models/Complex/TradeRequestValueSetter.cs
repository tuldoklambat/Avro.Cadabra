// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class TradeRequestValueSetter : ICustomValueSetter
    {
        public bool SetValue(object managedObject, string member, object value)
        {
            var type = managedObject.GetType();

            if (type == typeof(Vanilla))
            {
                switch (member)
                {
                    case "_tenor":
                        managedObject.SetFieldValue(member, value);
                        return true;
                }
            }

            if (type == typeof(Tenor))
            {
                switch (member)
                {
                    case "_tenorInMonths":
                        managedObject.SetFieldValue(member, value);
                        return true;
                }
            }

            return false;
        }
    }
}