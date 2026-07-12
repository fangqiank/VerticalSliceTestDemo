namespace VerticalSliceDemo.Domains
{
    public class Shipment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string Address { get; set; } = string.Empty;
        public ShipmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ShipmentStatus
    {
        Pending,
        InTransit,
        Delivered
    }
}
