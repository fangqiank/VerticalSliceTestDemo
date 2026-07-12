using Microsoft.AspNetCore.Mvc;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Features.Orders
{
    public static class CreateOrderEndpoint
    {
        public static void MapCreateOrder(this WebApplication app)
        {
            app.MapPost("/orders", async (
                [FromBody] CreateOrderRequest request,
                AppDbContext db,
                OrderPricingService pricingService) =>
            {
                var items = request.Items.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

                var total = pricingService.CalculateTotalPrice(items, request.IsPremiumCustomer);

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = request.CustomerName,
                    TotalAmount = total,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    Items = items
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                return Results.Created($"/orders/{order.Id}", new CreateOrderResponse(
                    order.Id, order.CustomerName, order.TotalAmount, order.Status));
            });
        }
    }

    public record CreateOrderRequest(
        string CustomerName,
        bool IsPremiumCustomer,
        List<CreateOrderItemRequest> Items);

    public record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

    public record CreateOrderResponse(Guid Id, string CustomerName, decimal TotalAmount, OrderStatus Status);
}
