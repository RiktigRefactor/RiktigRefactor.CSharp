class Program
{
    /// <summary>Calls <see cref="Compute"/> and returns its result.</summary>
    static int Run() => Compute();

    /// <summary>Returns the answer.</summary>
    static int Compute() => 42;

    static void Main()
    {
        if (Run() != 42)
        {
            throw new System.Exception("Run should return 42.");
        }
    }
}
