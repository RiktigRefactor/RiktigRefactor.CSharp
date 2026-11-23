namespace Shop
{
    class Order
    {
        internal const decimal Rate = 0.25m;
        public static int Calls;
    }

    class Pricing
    {
        public const decimal Rate = 100;

        public static int Calls;

        public static decimal Fee(decimal amount)
        {
            Order.Calls++;
            return amount * Order.Rate;
        }
    }

    class Program
    {
        static void Main()
        {
            var fee = Pricing.Fee(8);
            if (fee != 2 || Order.Calls != 1 || Pricing.Calls != 0)
            {
                throw new System.Exception($"{fee} {Order.Calls} {Pricing.Calls}");
            }
        }
    }
}
