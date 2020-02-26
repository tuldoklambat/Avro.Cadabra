// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class TradeRequest : IBindable
    {
        public ITrade Trade { get; set; }

        #region Implementation of IBindable

        public void ApplyBinding()
        {
            if (Trade is IBindable bindable)
            {
                bindable.ApplyBinding();
            }
        }

        #endregion
    }
}