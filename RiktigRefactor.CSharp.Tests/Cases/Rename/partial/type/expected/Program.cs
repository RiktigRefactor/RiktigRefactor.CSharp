class Program
{
    static void Main()
    {
        if (new Gadget().Run() != 42)
        {
            throw new System.Exception("Run should return 42.");
        }
    }
}
