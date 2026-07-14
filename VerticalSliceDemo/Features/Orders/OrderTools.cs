using System.ComponentModel;
using ModelContextProtocol.Server;
using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Features.Orders
{
    /// <summary>MCP tools exposing order capabilities to model clients.</summary>
    [McpServerToolType]
    public class OrderTools(OrderService orderService)
    {
        [McpServerTool]
        [Description("Create a sales order with line items. Applies discount and 8% tax. Returns the new order id and total.")]
        public async Task<string> CreateOrder(
            [Description("Customer name")] string customerName,
            [Description("True if the customer gets premium (10%) pricing")] bool isPremiumCustomer,
            [Description("Line items, each with ProductName, Quantity, UnitPrice")] List<CreateOrderItemRequest> items)
        {
            try
            {
                var order = await orderService.CreateAsync(new CreateOrderRequest(customerName, isPremiumCustomer, items));
                return $"Created order {order.Id} for '{order.CustomerName}': total {order.TotalAmount:0.00} (status {order.Status}).";
            }
            catch (ValidationException v)
            {
                return "Validation failed: " + string.Join("; ", v.Errors.SelectMany(p => p.Value));
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [McpServerTool]
        [Description("Get full details of an order by id, including its line items, total, and status.")]
        public async Task<string> GetOrder(
            [Description("Id of the order to look up")] Guid orderId)
        {
            try
            {
                var o = await orderService.GetAsync(orderId);
                var items = o.Items.Count == 0
                    ? "  (no items)"
                    : string.Join("\n", o.Items.Select(i => $"  - {i.ProductName} x{i.Quantity} @ {i.UnitPrice:0.00}"));
                return $"Order {o.Id}\n  Customer: {o.CustomerName}\n  Status: {o.Status}\n  Total: {o.TotalAmount:0.00}\n  Created: {o.CreatedAt:u}\n  Items:\n{items}";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [McpServerTool]
        [Description("List recent orders, optionally filtered by customer name (substring match) and/or status.")]
        public async Task<string> ListOrders(
            [Description("Optional: filter by customer name, substring match (e.g. 'ACME'). Omit to list all.")] string? customerName = null,
            [Description("Optional status filter: Pending, Confirmed, Shipped, Delivered, or Cancelled. Omit for any.")] OrderStatus? status = null)
        {
            try
            {
                var orders = await orderService.ListAsync(customerName, status);
                if (orders.Count == 0)
                    return "No orders found.";
                var lines = string.Join("\n", orders.Select(o => $"- {o.Id} | {o.CustomerName} | {o.Status} | {o.TotalAmount:0.00} | {o.CreatedAt:u}"));
                return $"{orders.Count} order(s):\n{lines}";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [McpServerTool]
        [Description("Cancel an order. Only pending or confirmed orders can be cancelled; shipped or delivered orders cannot.")]
        public async Task<string> CancelOrder(
            [Description("Id of the order to cancel")] Guid orderId)
        {
            try
            {
                var o = await orderService.CancelAsync(orderId);
                return $"Cancelled order {o.Id} for '{o.CustomerName}'.";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
    }
}
