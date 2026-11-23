class Limits
{

    public static int Clamp(int value) => value > Settings.Max ? Settings.Max : value;
}

class Settings
{
    public const int Min = 0;

    public const int Max = 10;
}

class Program
{
    static void Main()
    {
        if (Limits.Clamp(50) != 10 || Settings.Max != 10)
        {
            throw new System.Exception("Expected 10.");
        }
    }
}
