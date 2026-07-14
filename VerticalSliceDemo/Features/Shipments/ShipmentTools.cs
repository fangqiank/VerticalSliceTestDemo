using System.ComponentModel;
using ModelContextProtocol.Server;
using VerticalSliceDemo.Domains;

namespace VerticalSliceDemo.Features.Shipments
{
    /// <summary>MCP tools exposing shipment capabilities to model clients.</summary>
    [McpServerToolType]
    public class ShipmentTools(ShipmentService shipmentService)
    {
        [McpServerTool]
        [Description("Create a shipment for an existing, eligible order. Only one shipment per order is allowed.")]
        public async Task<string> CreateShipment(
            [Description("Id of the order to ship")] Guid orderId,
            [Description("Shipping address")] string address)
        {
            try
            {
                var shipment = await shipmentService.CreateAsync(new CreateShipmentRequest(orderId, address));
                return $"Created shipment {shipment.Id} for order {shipment.OrderId} to '{shipment.Address}' (status {shipment.Status}).";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [McpServerTool]
        [Description("Get the shipment for a given order (there is at most one shipment per order).")]
        public async Task<string> GetShipment(
            [Description("Id of the order whose shipment to look up")] Guid orderId)
        {
            try
            {
                var s = await shipmentService.GetAsync(orderId);
                return $"Shipment {s.Id}\n  Order: {s.OrderId}\n  Address: {s.Address}\n  Status: {s.Status}\n  Created: {s.CreatedAt:u}";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [McpServerTool]
        [Description("List recent shipments, optionally filtered by status.")]
        public async Task<string> ListShipments(
            [Description("Optional status filter: Pending, InTransit, or Delivered. Omit for any.")] ShipmentStatus? status = null)
        {
            try
            {
                var shipments = await shipmentService.ListAsync(status);
                if (shipments.Count == 0)
                    return "No shipments found.";
                var lines = string.Join("\n", shipments.Select(s => $"- {s.Id} | order {s.OrderId} | {s.Status} | {s.Address} | {s.CreatedAt:u}"));
                return $"{shipments.Count} shipment(s):\n{lines}";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [McpServerTool]
        [Description("Mark a pending shipment as shipped (moves it to InTransit). Only pending shipments can be shipped.")]
        public async Task<string> MarkShipmentShipped(
            [Description("Id of the order whose shipment to mark as shipped")] Guid orderId)
        {
            try
            {
                var s = await shipmentService.MarkShippedAsync(orderId);
                return $"Marked shipment {s.Id} for order {s.OrderId} as shipped (status {s.Status}).";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
    }
}
