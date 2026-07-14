using Microsoft.AspNetCore.Mvc;
using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Features.Orders
{
    public static class CreateOrderEndpoint
    {
        public static void MapCreateOrder(this WebApplication app)
        {
            app.MapPost("/orders", async (
                [FromBody] CreateOrderRequest request,
                OrderService service) =>
            {
                try
                {
                    var order = await service.CreateAsync(request);
                    return Results.Created($"/orders/{order.Id}", new CreateOrderResponse(
                        order.Id, order.CustomerName, order.TotalAmount, order.Status));
                }
                catch (Exception ex)
                {
                    return ProblemResults.Map(ex);
                }
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
