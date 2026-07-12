namespace VerticalSliceDemo.Features.Orders
{
    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this WebApplication app)
        {
            app.MapCreateOrder();
        }
    }
}
