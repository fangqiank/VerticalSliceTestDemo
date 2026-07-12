using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Features.Orders;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Tests
{
    public class CreateOrderTests(ApiFixture api): IClassFixture<ApiFixture>
    {
        [Fact]
        public async Task Creates_order_with_correct_pricing()
        {
            // Arrange
            await api.ResetAsync();
            var client = api.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/orders", new
            {
                CustomerName = "John Doe",
                IsPremiumCustomer = false,
                Items = new[]
                {
                new { ProductName = "Laptop", Quantity = 1, UnitPrice = 999.99m },
                new { ProductName = "Mouse", Quantity = 2, UnitPrice = 29.99m }
            }
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
            order.Should().NotBeNull();
            order!.CustomerName.Should().Be("John Doe");
            order.Status.Should().Be(OrderStatus.Pending);

            // 验证价格计算（通过 OrderPricingService）
            // subtotal = 999.99 + (2 * 29.99) = 1059.97
            // 5% discount = 52.9985
            // discounted = 1006.9715
            // 8% tax = 80.55772
            // total = 1087.52922 ≈ 1087.53
            // subtotal = 999.99 + (2 * 29.99) = 1059.97
            // 5% discount -> 1006.9715, +8% tax -> 1087.52922, rounded = 1087.53
            order.TotalAmount.Should().Be(1087.53m);

            // 验证持久化
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persistedOrder = await db.Orders
                .Include(o => o.Items)
                .FirstAsync(o => o.Id == order.Id);

            persistedOrder.Items.Should().HaveCount(2);
            persistedOrder.CustomerName.Should().Be("John Doe");
        }

        [Fact]
        public async Task Premium_customers_get_better_pricing()
        {
            // Arrange
            await api.ResetAsync();
            var client = api.CreateClient();

            // 普通客户订单
            var regularResponse = await client.PostAsJsonAsync("/orders", new
            {
                CustomerName = "Regular Customer",
                IsPremiumCustomer = false,
                Items = new[]
                {
                new { ProductName = "Premium Widget", Quantity = 5, UnitPrice = 300m }
            }
            });
            var regularOrder = await regularResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

            // 高级客户订单（相同商品）
            var premiumResponse = await client.PostAsJsonAsync("/orders", new
            {
                CustomerName = "Premium Customer",
                IsPremiumCustomer = true,
                Items = new[]
                {
                new { ProductName = "Premium Widget", Quantity = 5, UnitPrice = 300m }
            }
            });
            var premiumOrder = await premiumResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

            // Assert - 高级客户应获得更优惠的价格
            premiumOrder!.TotalAmount.Should().BeLessThan(regularOrder!.TotalAmount);
        }
    }
}
