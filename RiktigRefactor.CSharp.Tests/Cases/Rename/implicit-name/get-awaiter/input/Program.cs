using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Work
{
    public TaskAwaiter<int> /*|*/Other() => Task.FromResult(2).GetAwaiter();
}

static class WorkExtensions
{
    public static TaskAwaiter<int> GetAwaiter(this Work work) => Task.FromResult(1).GetAwaiter();
}

class Program
{
    static async Task Main()
    {
        if (await new Work() != 1)
        {
            throw new System.Exception("await should use the extension GetAwaiter.");
        }
    }
}
