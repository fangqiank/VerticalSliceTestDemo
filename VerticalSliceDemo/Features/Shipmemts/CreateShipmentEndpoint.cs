using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Features.Shipmemts
{
    public static class CreateShipmentEndpoint
    {
        public static void MapCreateShipment(this WebApplication app)
        {
            app.MapPost("/shipments", async (
                [FromBody] CreateShipmentRequest request,
                AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Address", new[] { "Address is required" } }
                    });
                }

                var orderExists = await db.Orders.AnyAsync(o => o.Id == request.OrderId);
                if (!orderExists)
                    return Results.NotFound($"Order {request.OrderId} not found");

                var existingShipment = await db.Shipments
                    .AnyAsync(s => s.OrderId == request.OrderId);

                if (existingShipment)
                    return Results.Conflict($"Shipment already exists for order {request.OrderId}");

                var shipment = new Shipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    Address = request.Address,
                    Status = ShipmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                db.Shipments.Add(shipment);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
                {
                    return Results.Conflict($"Shipment already exists for order {request.OrderId}");
                }

                return Results.Created($"/shipments/{shipment.Id}", new CreateShipmentResponse(
                    shipment.Id, shipment.OrderId, shipment.Address, shipment.Status));
            });
        }
    }

    public record CreateShipmentRequest(Guid OrderId, string Address);
    public record CreateShipmentResponse(Guid Id, Guid OrderId, string Address, ShipmentStatus Status);
}
