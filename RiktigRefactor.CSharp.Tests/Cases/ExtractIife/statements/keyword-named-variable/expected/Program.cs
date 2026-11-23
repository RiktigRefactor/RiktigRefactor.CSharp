class Program
{
    static void Main()
    {
        int @class = 2;
        @class = ((System.Func<int, int>)(static (@class) =>
        {
            @class *= 21;
            return @class;
        }))(@class);
        if (@class != 42)
        {
            throw new System.Exception($"Expected 42 but got {@class}.");
        }
    }
}
