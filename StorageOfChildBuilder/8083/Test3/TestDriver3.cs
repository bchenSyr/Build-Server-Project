/////////////////////////////////////////////////////////////////////
// TestDriver.cs - demonstration test driver                       
// Author: Beier Chen                                                                
// Source: Jim Fawcett
// Application: CSE681 - Software Modeling and Analysis, Fall 2017 
/////////////////////////////////////////////////////////////////////

using System;

namespace TestDriver3
{
    public class TestDriver3 : ITest
    {
        public bool test()
        {
            bool result;

            Tested1 one = new Tested1();
            string value1 = one.Say();
            Console.WriteLine("\n  one supposed to return \"Tested1\" ");
            Console.Write("  - one returned: \"{0}\"", value1);
            if (value1 != "Tested1")
            {
                result = false;
            }
            else result = true;

            if (result)
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
}
