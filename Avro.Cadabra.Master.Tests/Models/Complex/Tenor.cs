using System;

namespace Gooseman.Avro.Utility.Tests.Models.Complex
{
    public class Tenor : IBindable
    {
        private int _tenorInMonths;
        private DateTime? _dateTimeValue;

        // default ctor important when deserializing!
        public Tenor()
        {
            
        }

        public Tenor(int tenorInMonths)
        {
            _tenorInMonths = tenorInMonths;
        }

        public DateTime DateTimeValue
        {
            get
            {
                if (_dateTimeValue.HasValue)
                    return _dateTimeValue.Value;

                throw new ApplicationException("ApplyBinding not called.");
            }
        }

        #region Implementation of IBindable

        public void ApplyBinding()
        {
            _dateTimeValue = DateTime.Now.AddMonths(_tenorInMonths);
        }

        #endregion
    }
}
