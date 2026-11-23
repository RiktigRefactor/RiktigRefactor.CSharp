namespace Shop
{
    static class StringExtensions
    {
        public static string /*|*/Shout(this string text) => text.ToUpperInvariant() + "!";
    }

    static class TextHelpers
    {
    }

    class Program
    {
        static void Main()
        {
            var text = "hi".Shout() + StringExtensions.Shout("yo");
            if (text != "HI!YO!")
            {
                throw new System.Exception(text);
            }
        }
    }
}
