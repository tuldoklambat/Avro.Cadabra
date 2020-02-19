// // Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// // All rights reserved.
// //
// // THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// // KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// // WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// // MERCHANTABILITY OR NON-INFRINGEMENT.

namespace Gooseman.Avro.Utility
{
    public interface ICustomValueSetter
    {
        /// <summary>
        /// Sets the value of the member name of the managed object
        /// </summary>
        /// <param name="managedObject"></param>
        /// <param name="member"></param>
        /// <param name="value"></param>
        /// <returns>Indicates whether the value was set</returns>
        bool SetValue(object managedObject, string member, object value);
    }
}
