namespace VerticalSliceDemo.Domains
{
    public class Order
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Shipped,
        Delivered,
        Cancelled
    }
}
