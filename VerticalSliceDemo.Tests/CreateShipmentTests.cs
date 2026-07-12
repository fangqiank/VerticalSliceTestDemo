using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Features.Orders;
using VerticalSliceDemo.Features.Shipmemts;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Tests
{
    public class CreateShipmentTests(ApiFixture api) : IClassFixture<ApiFixture>
    {
        [Fact]
        public async Task Creates_shipment_and_persists_it()
        {
            await api.ResetAsync();
            var client = api.CreateClient();

            var createOrderResponse = await client.PostAsJsonAsync("/orders", new
            {
                CustomerName = "Test Customer",
                IsPremiumCustomer = false,
                Items = new[]
                {
                    new { ProductName = "Widget", Quantity = 2, UnitPrice = 10.0m }
                }
            });
            var order = await createOrderResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

            var response = await client.PostAsJsonAsync("/shipments", new
            {
                OrderId = order!.Id,
                Address = "123 Test St, Test City, TX"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var shipmentResponse = await response.Content.ReadFromJsonAsync<CreateShipmentResponse>();
            shipmentResponse.Should().NotBeNull();
            shipmentResponse!.OrderId.Should().Be(order.Id);
            shipmentResponse.Address.Should().Be("123 Test St, Test City, TX");
            shipmentResponse.Status.Should().Be(ShipmentStatus.Pending);

            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var shipmentInDb = await db.Shipments.SingleAsync(s => s.OrderId == order.Id);
            shipmentInDb.Status.Should().Be(ShipmentStatus.Pending);
            shipmentInDb.Address.Should().Be("123 Test St, Test City, TX");
        }

        [Fact]
        public async Task Rejects_a_shipment_without_an_address()
        {
            await api.ResetAsync();
            var client = api.CreateClient();

            var createOrderResponse = await client.PostAsJsonAsync("/orders", new
            {
                CustomerName = "Test Customer",
                IsPremiumCustomer = false,
                Items = new[]
                {
                    new { ProductName = "Widget", Quantity = 2, UnitPrice = 10.0m }
                }
            });
            var order = await createOrderResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

            var response = await client.PostAsJsonAsync("/shipments", new
            {
                OrderId = order!.Id,
                Address = ""
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Shipments.AnyAsync(s => s.OrderId == order.Id)).Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_duplicate_shipment_for_same_order()
        {
            await api.ResetAsync();
            var client = api.CreateClient();

            var createOrderResponse = await client.PostAsJsonAsync("/orders", new
            {
                CustomerName = "Test Customer",
                IsPremiumCustomer = false,
                Items = new[]
                {
                    new { ProductName = "Widget", Quantity = 2, UnitPrice = 10.0m }
                }
            });
            var order = await createOrderResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

            await client.PostAsJsonAsync("/shipments", new
            {
                OrderId = order!.Id,
                Address = "123 Test St, Test City, TX"
            });

            var response = await client.PostAsJsonAsync("/shipments", new
            {
                OrderId = order.Id,
                Address = "456 Test St, Test City, TX"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var shipmentCount = await db.Shipments.CountAsync(s => s.OrderId == order.Id);
            shipmentCount.Should().Be(1);
        }
    }
}
