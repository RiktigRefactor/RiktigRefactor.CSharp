using static Shop.Order;

namespace Shop
{
    class Program
    {
        static void Main()
        {
            if (Rate() != 3)
            {
                throw new System.Exception("Expected 3.");
            }
        }
    }
}
