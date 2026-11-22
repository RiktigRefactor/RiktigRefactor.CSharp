class Program
{
    static void Main()
    {
        var count = 0;
    Again:
        count++;
        if (count < 3)
        {
            goto Again;
        }

        if (count != 3)
        {
            throw new System.Exception("The loop should run three times.");
        }
    }
}
