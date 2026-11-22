using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Ready : INotifyCompletion
{
    public Ready GetAwaiter() => this;

    public bool IsCompleted => true;

    public void OnCompleted(Action continuation) => continuation();

    public int /*|*/GetResult() => 3;
}

class Program
{
    static async Task Main()
    {
        if (await new Ready() != 3)
        {
            throw new Exception("await should return GetResult.");
        }
    }
}
