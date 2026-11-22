namespace Forms
{
    public class Circle
    {
        public int Sides => 0;
    }
}

namespace App
{
    using Forms;

    class Program
    {
        static void Main()
        {
            if (new Circle().Sides != 0 || new Forms.Circle().Sides != 0)
            {
                throw new System.Exception("Circle should have no sides.");
            }
        }
    }
}
