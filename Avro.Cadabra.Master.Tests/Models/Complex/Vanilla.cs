// Copyright (c) Gooseman Brothers (gooseman.brothers@gmail.com)
// All rights reserved.
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.

using System;

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class Vanilla : ITrade, IBindable
    {
        private DateTime _expiryDate;
        private Tenor _tenor;

        public void SetTenor(Tenor tenor)
        {
            _tenor = tenor;
        }

        public DateTime ExpiryDate
        {
            get
            {
                if (_tenor == null)
                {
                    return _expiryDate;
                }

                _expiryDate = _tenor.DateTimeValue;
                return _expiryDate;
            }
            set
            {
                _expiryDate = value;
                _tenor = null;
            }
        }

        #region Implementation of IProduct

        public string Name => "Vanilla Option";

        #endregion

        #region Implementation of IBindable

        public void ApplyBinding()
        {
            _tenor.ApplyBinding();
        }

        #endregion
    }
}