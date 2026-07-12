using FluentAssertions;
using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Tests
{
    public class OrderPricingServiceTests
    {
        private readonly OrderPricingService _service = new();

        [Theory]
        [InlineData(100, 1, 100.00)] // 单件商品
        [InlineData(100, 3, 300.00)] // 多件同价商品
        [InlineData(50, 5, 250.00)] // 大数量
        public void Calculates_subtotal_correctly(int unitPrice, int quantity, decimal expectedSubtotal)
        {
            var items = new List<OrderItem>
            {
                new() { ProductName = "Test", UnitPrice = unitPrice, Quantity = quantity }
            };

            var expected = Math.Round(expectedSubtotal * 0.95m * 1.08m, 2);

            var total = _service.CalculateTotalPrice(items, false);

            total.Should().Be(expected);
        }

        [Fact]
        public void Premium_customers_get_higher_discount_for_large_orders()
        {
            var items = new List<OrderItem>()
            {
                new() { ProductName = "Expensive", UnitPrice = 500, Quantity = 3 }
            };

            var regularTotal = _service.CalculateTotalPrice(items, false);
            var premiumTotal = _service.CalculateTotalPrice(items, true);

            premiumTotal.Should().BeLessThan(regularTotal);

            var expectedPremiumTotal = Math.Round(1500m * 0.90m * 1.08m, 2);
            premiumTotal.Should().Be(expectedPremiumTotal);
        }

        [Fact]
        public void Empty_order_has_zero_total()
        {
            var total = _service.CalculateTotalPrice(new List<OrderItem>(), false);
            total.Should().Be(0);
        }

        [Fact]
        public void Handles_multiple_items_correctly()
        {
            var items = new List<OrderItem>
            {
                new() { ProductName = "Item1", Quantity = 2, UnitPrice = 10 },
                new() { ProductName = "Item2", Quantity = 1, UnitPrice = 30 },
                new() { ProductName = "Item3", Quantity = 3, UnitPrice = 5 }
            };

            var subtotal = (2 * 10) + (1 * 30) + (3 * 5); // 20 + 30 + 15 = 65
            var expected = Math.Round(subtotal * 0.95m * 1.08m, 2);

            var total = _service.CalculateTotalPrice(items, false);

            total.Should().Be(expected);
        }
    }

}
