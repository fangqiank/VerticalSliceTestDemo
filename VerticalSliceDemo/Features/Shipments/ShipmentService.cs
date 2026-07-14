using Microsoft.EntityFrameworkCore;
using Npgsql;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Features.Shipments
{
    /// <summary>Creates shipments: validates the address, enforces order eligibility and the
    /// one-shipment-per-order rule (including the concurrent race), and persists.</summary>
    public class ShipmentService(AppDbContext db)
    {
        public async Task<Shipment> CreateAsync(CreateShipmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Address", new[] { "Address is required" } }
                });
            }

            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
            if (order is null)
                throw new NotFoundException($"Order {request.OrderId} not found");

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Shipped or OrderStatus.Delivered)
                throw new ConflictException($"Order {request.OrderId} is not eligible for shipment");

            var existingShipment = await db.Shipments.AnyAsync(s => s.OrderId == request.OrderId);
            if (existingShipment)
                throw new ConflictException($"Shipment already exists for order {request.OrderId}");

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
                throw new ConflictException($"Shipment already exists for order {request.OrderId}");
            }

            return shipment;
        }

        /// <summary>Loads the shipment for a given order (one shipment per order). Throws if none exists.</summary>
        public async Task<Shipment> GetAsync(Guid orderId)
        {
            var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (shipment is null)
                throw new NotFoundException($"Shipment for order {orderId} not found");
            return shipment;
        }

        /// <summary>Lists recent shipments, optionally filtered by status.</summary>
        public async Task<List<Shipment>> ListAsync(ShipmentStatus? status = null, int limit = 50)
        {
            IQueryable<Shipment> query = db.Shipments;
            if (status is not null)
                query = query.Where(s => s.Status == status);

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>Marks a pending shipment as shipped (moves it to InTransit). Only pending shipments can be shipped.</summary>
        public async Task<Shipment> MarkShippedAsync(Guid orderId)
        {
            var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (shipment is null)
                throw new NotFoundException($"Shipment for order {orderId} not found");

            if (shipment.Status is ShipmentStatus.InTransit or ShipmentStatus.Delivered)
                throw new ConflictException($"Shipment for order {orderId} is already {shipment.Status}");

            shipment.Status = ShipmentStatus.InTransit;
            await db.SaveChangesAsync();
            return shipment;
        }
    }
}
