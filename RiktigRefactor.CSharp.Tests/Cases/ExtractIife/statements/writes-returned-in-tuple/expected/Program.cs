class Program
{
    static void Main()
    {
        int a = 1;
        int b = 0;
        (a, b) = ((System.Func<int, int, (int, int)>)(static (a, b) =>
        {
            a++;
            b = a * 2;
            return (a, b);
        }))(a, b);
        if (a != 2 || b != 4)
        {
            throw new System.Exception($"Expected a=2, b=4 but got a={a}, b={b}.");
        }
    }
}
