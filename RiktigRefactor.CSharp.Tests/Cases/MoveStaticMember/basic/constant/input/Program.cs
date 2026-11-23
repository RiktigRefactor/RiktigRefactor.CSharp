class Limits
{
    public const int /*|*/Max = 10;

    public static int Clamp(int value) => value > Max ? Max : value;
}

class Settings
{
    public const int Min = 0;
}

class Program
{
    static void Main()
    {
        if (Limits.Clamp(50) != 10 || Limits.Max != 10)
        {
            throw new System.Exception("Expected 10.");
        }
    }
}
