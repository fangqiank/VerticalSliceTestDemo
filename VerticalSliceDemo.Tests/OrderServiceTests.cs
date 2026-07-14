using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Features.Orders;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Tests
{
    public class OrderServiceTests(ApiFixture api) : IClassFixture<ApiFixture>
    {
        // Seeds an order directly via the DbContext so tests can set any Status
        // (CreateAsync only ever produces Pending).
        private static Order NewOrder(string customer, OrderStatus status,
            params (string Name, int Qty, decimal Price)[] items)
        {
            var id = Guid.NewGuid();
            return new Order
            {
                Id = id,
                CustomerName = customer,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = items.Sum(i => i.Qty * i.Price),
                Items = items.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = id,
                    ProductName = i.Name,
                    Quantity = i.Qty,
                    UnitPrice = i.Price
                }).ToList()
            };
        }

        [Fact]
        public async Task GetAsync_returns_order_with_its_items()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            var seeded = NewOrder("Alice", OrderStatus.Pending, ("Widget", 2, 10m), ("Gadget", 1, 25m));
            db.Orders.Add(seeded);
            await db.SaveChangesAsync();

            var got = await svc.GetAsync(seeded.Id);

            got.CustomerName.Should().Be("Alice");
            got.Status.Should().Be(OrderStatus.Pending);
            got.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAsync_throws_when_order_missing()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            var act = () => svc.GetAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task ListAsync_filters_by_customer_name()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            db.Orders.Add(NewOrder("Alice Smith", OrderStatus.Pending));
            db.Orders.Add(NewOrder("Bob Jones", OrderStatus.Pending));
            await db.SaveChangesAsync();

            var result = await svc.ListAsync(customerName: "Alice");

            result.Should().ContainSingle();
            result[0].CustomerName.Should().Be("Alice Smith");
        }

        [Fact]
        public async Task ListAsync_filters_by_status()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            db.Orders.Add(NewOrder("A", OrderStatus.Pending));
            db.Orders.Add(NewOrder("B", OrderStatus.Cancelled));
            db.Orders.Add(NewOrder("C", OrderStatus.Confirmed));
            await db.SaveChangesAsync();

            var result = await svc.ListAsync(status: OrderStatus.Cancelled);

            result.Should().ContainSingle();
            result[0].Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        public async Task CancelAsync_cancels_a_pending_order()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            var seeded = NewOrder("Alice", OrderStatus.Pending);
            db.Orders.Add(seeded);
            await db.SaveChangesAsync();

            var cancelled = await svc.CancelAsync(seeded.Id);

            cancelled.Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        public async Task CancelAsync_rejects_a_shipped_order()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            var seeded = NewOrder("Alice", OrderStatus.Shipped);
            db.Orders.Add(seeded);
            await db.SaveChangesAsync();

            var act = () => svc.CancelAsync(seeded.Id);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task CancelAsync_rejects_an_already_cancelled_order()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<OrderService>();

            var seeded = NewOrder("Alice", OrderStatus.Cancelled);
            db.Orders.Add(seeded);
            await db.SaveChangesAsync();

            var act = () => svc.CancelAsync(seeded.Id);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }
}
