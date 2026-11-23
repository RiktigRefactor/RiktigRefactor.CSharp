using System;

namespace Shop
{
    class Order
    {

        public static decimal WithTax(decimal amount) => amount + Pricing.Tax(amount);
    }

    class SpecialOrder : Order
    {
    }

    class Pricing
    {
        public static decimal Discount(decimal amount) => amount / 2;

        public static decimal Tax(decimal amount) => amount / 10;
    }

    class Program
    {
        static void Main()
        {
            Func<decimal, decimal> tax = Pricing.Tax;
            var total = Order.WithTax(100) + Pricing.Tax(10) + tax(20) + Pricing.Tax(30);
            if (total != 116)
            {
                throw new Exception($"Expected 116, got {total}.");
            }
        }
    }
}
