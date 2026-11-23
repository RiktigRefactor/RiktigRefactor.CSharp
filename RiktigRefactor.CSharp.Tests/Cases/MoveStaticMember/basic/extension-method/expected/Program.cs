namespace Shop
{
    static class StringExtensions
    {
    }

    static class TextHelpers
    {
        public static string Shout(this string text) => text.ToUpperInvariant() + "!";
    }

    class Program
    {
        static void Main()
        {
            var text = "hi".Shout() + TextHelpers.Shout("yo");
            if (text != "HI!YO!")
            {
                throw new System.Exception(text);
            }
        }
    }
}
