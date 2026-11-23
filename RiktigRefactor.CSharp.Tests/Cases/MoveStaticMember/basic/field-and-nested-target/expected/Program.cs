namespace Shop
{
    class Order
    {
    }

    static class Outer
    {
        public static class Defaults {
            /// <summary>The default quantity.</summary>
            public static int DefaultQuantity = 2;
        }
    }

    class Program
    {
        static void Main()
        {
            Outer.Defaults.DefaultQuantity++;
            if (Outer.Defaults.DefaultQuantity != 3)
            {
                throw new System.Exception("Expected 3.");
            }
        }
    }
}
