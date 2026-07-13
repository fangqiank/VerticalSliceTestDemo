namespace VerticalSliceDemo.Features.Shipments
{
    public static class ShipmentEndpoints
    {
        public static void MapShipmentEndpoints(this WebApplication app)
        {
            app.MapCreateShipment();
        }
    }
}
