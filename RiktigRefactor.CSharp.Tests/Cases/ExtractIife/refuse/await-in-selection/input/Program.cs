using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var value = /*[*/await Task.FromResult(42)/*]*/;
        if (value != 42)
        {
            throw new System.Exception("The value should be 42.");
        }
    }
}
