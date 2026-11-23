using System;

namespace Shop
{
    class Order
    {
        public static decimal /*|*/Tax(decimal amount) => amount / 10;

        public static decimal WithTax(decimal amount) => amount + Tax(amount);
    }

    class SpecialOrder : Order
    {
    }

    class Pricing
    {
        public static decimal Discount(decimal amount) => amount / 2;
    }

    class Program
    {
        static void Main()
        {
            Func<decimal, decimal> tax = Order.Tax;
            var total = Order.WithTax(100) + SpecialOrder.Tax(10) + tax(20) + Shop.Order.Tax(30);
            if (total != 116)
            {
                throw new Exception($"Expected 116, got {total}.");
            }
        }
    }
}
