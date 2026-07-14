using Microsoft.EntityFrameworkCore;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Features.Orders
{
    /// <summary>Creates orders: validates input, prices via <see cref="OrderPricingService"/>, and persists.</summary>
    public class OrderService(AppDbContext db, OrderPricingService pricingService)
    {
        public async Task<Order> CreateAsync(CreateOrderRequest request)
        {
            if (request.Items.Count == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Items", new[] { "At least one item is required" } }
                });
            }

            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.CustomerName))
                errors["CustomerName"] = new[] { "CustomerName is required" };

            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                if (item.Quantity <= 0)
                    errors[$"Items[{i}].Quantity"] = new[] { "Quantity must be greater than 0" };
                if (string.IsNullOrWhiteSpace(item.ProductName))
                    errors[$"Items[{i}].ProductName"] = new[] { "ProductName is required" };
                if (item.UnitPrice <= 0)
                    errors[$"Items[{i}].UnitPrice"] = new[] { "UnitPrice must be greater than 0" };
            }

            if (errors.Count > 0)
                throw new ValidationException(errors);

            var orderId = Guid.NewGuid();

            var items = request.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            var total = pricingService.CalculateTotalPrice(items, request.IsPremiumCustomer);

            var order = new Order
            {
                Id = orderId,
                CustomerName = request.CustomerName,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = items
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return order;
        }

        /// <summary>Loads a single order together with its line items. Throws if not found.</summary>
        public async Task<Order> GetAsync(Guid id)
        {
            var order = await db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
                throw new NotFoundException($"Order {id} not found");

            return order;
        }

        /// <summary>Lists recent orders, optionally filtered by customer name (substring) and status.</summary>
        public async Task<List<Order>> ListAsync(string? customerName = null, OrderStatus? status = null, int limit = 50)
        {
            IQueryable<Order> query = db.Orders;

            if (customerName is not null)
                query = query.Where(o => o.CustomerName.Contains(customerName));
            if (status is not null)
                query = query.Where(o => o.Status == status);

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>Cancels an order. Only pending or confirmed orders may be cancelled.</summary>
        public async Task<Order> CancelAsync(Guid id)
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order is null)
                throw new NotFoundException($"Order {id} not found");

            if (order.Status is OrderStatus.Cancelled)
                throw new ConflictException($"Order {id} is already cancelled");
            if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
                throw new ConflictException($"Order {id} cannot be cancelled because it is {order.Status}");

            order.Status = OrderStatus.Cancelled;
            await db.SaveChangesAsync();
            return order;
        }
    }
}
