/////////////////////////////////////////////////////////////////////
// TestDriver.cs - demonstration test driver                       
// Author: Beier Chen                                                                
// Source: Jim Fawcett
// Application: CSE681 - Software Modeling and Analysis, Fall 2017 
/////////////////////////////////////////////////////////////////////

using System;

namespace TestDriver4
{
    public class TestDriver : ITest
    {
        public bool test()
        {
            bool result1;
            bool result2;
            bool result3;

            Tested1 one = new Tested1();
            string value1 = one.Say();
            Console.WriteLine("\n  one supposed to return \"Tested1\" ");
            Console.Write("  - one returned {0}", value1);
            if (value1 != "Tested1")
            {
                result1 = false;
            }
            else result1 = true;

            Tested2 two = new Tested2();
            string value2 = two.Say();
            Console.WriteLine("\n  two supposed to return \"Tested2\" ");
            Console.Write("  - two returned {0}", value2);
            if (value2 != "Tested2")
            {
                result2 = false;
            }
            else result2 = true;

            Tested3 three = new Tested3();
            string value3 = three.Say();
            Console.WriteLine("\n  three supposed to return \"Tested3\" ");
            Console.Write("  - three returned {0}", value3);
            if (value3 != "Tested3")
            {
                result3 = false;
            }
            else result3 = true;

            if (result1 && result2 && result3)
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
            Console.Write("\n  TestDriver4 running: \n");
            TestDriver TestResult = new TestDriver();
            TestResult.test();
        }
    }
}
