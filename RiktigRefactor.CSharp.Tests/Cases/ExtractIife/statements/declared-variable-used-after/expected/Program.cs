class Program
{
    static void Main()
    {
        int a = 20;
        int answer = ((System.Func<int, int>)(static (a) =>
        {
            var doubled = a * 2;
            var answer = doubled + 2;
            return answer;
        }))(a);
        if (answer != 42)
        {
            throw new System.Exception($"Expected 42 but got {answer}.");
        }
    }
}
