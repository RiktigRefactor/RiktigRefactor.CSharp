namespace Shop
{
    class Order
    {
        /// <summary>The default quantity.</summary>
        public static int /*|*/DefaultQuantity = 2;
    }

    static class Outer
    {
        public static class Defaults { }
    }

    class Program
    {
        static void Main()
        {
            Order.DefaultQuantity++;
            if (Order.DefaultQuantity != 3)
            {
                throw new System.Exception("Expected 3.");
            }
        }
    }
}
