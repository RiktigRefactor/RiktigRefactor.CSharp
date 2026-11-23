using /*|*/Source = Outer.Original;

namespace Outer
{
    class Original
    {
        public static int Value() => 1;
    }
}

namespace Inner
{
    class Local
    {
        public static int Value() => 2;
    }

    class Program
    {
        static void Main()
        {
            if (Source.Value() != 1)
            {
                throw new System.Exception("The alias should name Outer.Original.");
            }
        }
    }
}
