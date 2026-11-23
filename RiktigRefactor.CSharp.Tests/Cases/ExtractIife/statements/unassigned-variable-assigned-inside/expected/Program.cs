class Program
{
    static void Main()
    {
        int result;
        int input = 6;
        result = ((System.Func<int, int>)(static (input) =>
        {
            int result;
            result = input * 7;
            return result;
        }))(input);
        if (result != 42)
        {
            throw new System.Exception($"Expected 42 but got {result}.");
        }
    }
}
