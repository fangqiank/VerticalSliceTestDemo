namespace VerticalSliceDemo.Domains
{
    public class OrderPricingService
    {
        private const decimal PremiumThreshold = 1000m;
        private const decimal PremiumDiscountRate = 0.10m;
        private const decimal RegularDiscountRate = 0.05m;
        private const decimal TaxRate = 0.08m;

        public decimal CalculateTotalPrice(List<OrderItem> items, bool isPremiumCustomer)
        {
            var subtotal = items.Sum(item => item.Quantity * item.UnitPrice);
            
            var discountRate = isPremiumCustomer && subtotal >= PremiumThreshold 
                ? PremiumDiscountRate 
                : RegularDiscountRate;

            var discountedSubtotal = subtotal * (1 - discountRate);
            var tax = discountedSubtotal * TaxRate;

            return Math.Round(discountedSubtotal + tax, 2);
        }
    }
}
