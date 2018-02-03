/////////////////////////////////////////////////////////////////////
// TestDriver.cs - demonstration test driver                       
// Author: Beier Chen                                                                
// Source: Jim Fawcett
// Application: CSE681 - Software Modeling and Analysis, Fall 2017 
/////////////////////////////////////////////////////////////////////

using System;

namespace TestDriver1
{
    public class TestDriver1 : ITest
    {
        public bool test()
        {
            bool result1;
            bool result2;

            Tested1 one = new Tested1();
            string value1 = one.Say();
            Console.WriteLine("\n  one supposed to return \"Tested1\" ");
            Console.Write("  - one returned: \"{0}\"", value1);
            if (value1 != "Tested1")
            {
                result1 = false;
            }
            else result1 = true;

            Tested2 two = new Tested2();
            string value2 = two.Say();
            Console.WriteLine("\n  two supposed to return \"Tested2\" ");
            Console.Write("  - two returned: \"{0}\"", value2);
            if (value2 != "Tested0")
            {
                result2 = false;
            }
            else result2 = true;

            if (result1 && result2)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  Test result: succeed");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  Test result: fail");
                Console.ResetColor();
                return false;
            }
        }
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.Write("\n  TestDriver1 running: \n");
            TestDriver1 TestResult = new TestDriver1();
            TestResult.test();
        }
    }
}
