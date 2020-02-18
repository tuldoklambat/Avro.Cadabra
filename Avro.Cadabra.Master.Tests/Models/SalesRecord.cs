using System;

namespace Gooseman.Avro.Utility.Tests.Models
{
    public interface ISalesRecord
    {
        int OrderId { get; set; }
    }

    public class SalesRecord : ISalesRecord
    {
        public string Region { get; set; }
        public string Country { get; set; }
        public string ItemType { get; set; }
        public SalesChannelType SalesChannel { get; set; }
        public OrderPriorityType OrderPriority { get; set; }
        public DateTime OrderDate { get; set; }
        public int OrderId { get; set; }
        public DateTime ShipDate { get; set; }
        public int UnitsSold { get; set; }
        public double UnitPrice { get; set; }
        public double UnitCost { get; set; }
        public double TotalRevenue { get; set; }
        public double TotalCost { get; set; }
        public double TotalProfit { get; set; }
    }

    public enum SalesChannelType
    {
        Online,
        Offline
    }

    public enum OrderPriorityType
    {
        C,
        H,
        M,
        L
    }
}
