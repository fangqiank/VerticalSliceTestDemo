using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Features.Shipments;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Tests
{
    public class ShipmentServiceTests(ApiFixture api) : IClassFixture<ApiFixture>
    {
        // Seeds a shipment directly. Shipment has no DB-level FK to Order, so no
        // order row is required for these queries/transitions.
        private static Shipment NewShipment(Guid orderId, ShipmentStatus status) => new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Address = "1 Test Street",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task GetAsync_returns_shipment_for_order()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<ShipmentService>();

            var orderId = Guid.NewGuid();
            db.Shipments.Add(NewShipment(orderId, ShipmentStatus.Pending));
            await db.SaveChangesAsync();

            var got = await svc.GetAsync(orderId);

            got.OrderId.Should().Be(orderId);
            got.Status.Should().Be(ShipmentStatus.Pending);
        }

        [Fact]
        public async Task GetAsync_throws_when_no_shipment_for_order()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ShipmentService>();

            var act = () => svc.GetAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task ListAsync_filters_by_status()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<ShipmentService>();

            db.Shipments.Add(NewShipment(Guid.NewGuid(), ShipmentStatus.Pending));
            db.Shipments.Add(NewShipment(Guid.NewGuid(), ShipmentStatus.InTransit));
            await db.SaveChangesAsync();

            var result = await svc.ListAsync(status: ShipmentStatus.Pending);

            result.Should().ContainSingle();
            result[0].Status.Should().Be(ShipmentStatus.Pending);
        }

        [Fact]
        public async Task MarkShippedAsync_moves_pending_to_intransit()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<ShipmentService>();

            var orderId = Guid.NewGuid();
            db.Shipments.Add(NewShipment(orderId, ShipmentStatus.Pending));
            await db.SaveChangesAsync();

            var shipped = await svc.MarkShippedAsync(orderId);

            shipped.Status.Should().Be(ShipmentStatus.InTransit);
        }

        [Fact]
        public async Task MarkShippedAsync_rejects_an_already_intransit_shipment()
        {
            await api.ResetAsync();
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<ShipmentService>();

            var orderId = Guid.NewGuid();
            db.Shipments.Add(NewShipment(orderId, ShipmentStatus.InTransit));
            await db.SaveChangesAsync();

            var act = () => svc.MarkShippedAsync(orderId);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }
}
