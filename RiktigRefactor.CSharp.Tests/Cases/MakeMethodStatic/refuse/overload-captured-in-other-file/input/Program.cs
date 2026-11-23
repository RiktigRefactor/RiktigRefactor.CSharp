class Program
{
    static void Main()
    {
        if (Order.Total(new Order(), 3) != -1)
        {
            throw new System.Exception("Wrong overload.");
        }
    }
}
