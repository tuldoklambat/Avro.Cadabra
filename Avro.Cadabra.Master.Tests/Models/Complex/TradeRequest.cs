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
