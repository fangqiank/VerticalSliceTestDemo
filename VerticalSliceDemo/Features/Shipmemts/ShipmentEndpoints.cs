namespace VerticalSliceDemo.Features.Shipmemts
{
    public static class ShipmentEndpoints
    {
        public static void MapShipmentEndpoints(this WebApplication app)
        {
            app.MapCreateShipment();
        }
    }
}
