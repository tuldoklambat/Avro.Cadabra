// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class TradeRequestValueGetter : ICustomValueGetter
    {
        public object GetValue(object managedObject, string member)
        {
            object result = null;
            var type = managedObject.GetType();

            if (type == typeof(Vanilla))
            {
                switch (member)
                {
                    case "_tenor":
                        result = managedObject.GetFieldValue(member);
                        break;
                    case "ExpiryDate":
                        // bypass ExpiryDate getter to prevent throwing an exception because tenor.Resolve() is not called
                        // and effectively getting the value of ExpiryDate if its actually assigned
                        result = managedObject.GetFieldValue("_expiryDate");
                        break;
                }
            }

            if (type == typeof(Tenor))
            {
                switch (member)
                {
                    case "_tenorInMonths":
                        result = managedObject.GetFieldValue(member);
                        break;
                }
            }

            return result;
        }
    }
}