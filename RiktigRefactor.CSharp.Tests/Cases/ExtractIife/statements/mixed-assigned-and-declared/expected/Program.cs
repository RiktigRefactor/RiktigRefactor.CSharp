class Program
{
    static void Main()
    {
        int count = 0;
        (count, string label) = ((System.Func<int, (int, string)>)(static (count) =>
        {
            count += 5;
            string label = "count " + count;
            return (count, label);
        }))(count);
        if (count != 5 || label != "count 5")
        {
            throw new System.Exception($"Expected 5 and \"count 5\" but got {count} and {label}.");
        }
    }
}
