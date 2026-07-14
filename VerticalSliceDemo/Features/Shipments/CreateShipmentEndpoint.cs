using Microsoft.AspNetCore.Mvc;
using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Features.Shipments
{
    public static class CreateShipmentEndpoint
    {
        public static void MapCreateShipment(this WebApplication app)
        {
            app.MapPost("/shipments", async (
                [FromBody] CreateShipmentRequest request,
                ShipmentService service) =>
            {
                try
                {
                    var shipment = await service.CreateAsync(request);
                    return Results.Created($"/shipments/{shipment.Id}", new CreateShipmentResponse(
                        shipment.Id, shipment.OrderId, shipment.Address, shipment.Status));
                }
                catch (Exception ex)
                {
                    return ProblemResults.Map(ex);
                }
            });
        }
    }

    public record CreateShipmentRequest(Guid OrderId, string Address);
    public record CreateShipmentResponse(Guid Id, Guid OrderId, string Address, ShipmentStatus Status);
}
