using System;

namespace TestDriver2
{
	interface ITest
	{
		bool test();
	}

    public class TestDriver : ITest
    {
        bool result1;
        bool result2;
        public bool test()
        {
            Tested1 one = new Tested1();
            result1 = one.Test();
            Tested2 two = new Tested2();
            result2 = two.Test();
            if (result1 && result2)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n Test result: succeed!!!");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n Test result: fail!!!");
                Console.ResetColor();
                return false;
            }
           
        }
 
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
			Console.Write("\n TestDriver2 running:");
			TestDriver td = new TestDriver();
			td.test();
			Console.Write("\n");
        }
    }
}
