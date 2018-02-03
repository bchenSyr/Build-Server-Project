/////////////////////////////////////////////////////////////////////
// PrintTool.cs - several pretty printing functions          
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #4, CSE 681, Fall2017                                  
// Environment: Windows 10 Education, MacBook                                       
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * pretty printing functions to help demonstration
 * 
 * Public Interface:
 * ===================
 * title() - printing Cyan color titile line
 * Line() - printing Yellow color line 
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 8 2017
 * - first release
 * 
 */

using System;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // TestUtilities class
    public class PrintTool
    {
        /*----< pretty print titles >----------------------------------*/
        public static void title(string msg, char ch = '-')
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (ch == '*')
            {
                Console.Write("\n  {0} \n  ", msg);
                Console.Write("{0}", new string(ch, msg.Length + 2));
            }
            else
            {
                Console.Write("\n\n  {0}", msg);
                Console.Write("\n  {0}", new string(ch, msg.Length + 2));
            }                        
            Console.ResetColor();
        }

        /*----< write line of text >-------*/
        public static void Print(string msg = "")
        {
            if (msg.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;                
                Console.Write("  {0}", msg);
                Console.ResetColor();
            }
            else
                Console.Write("\n");
        }

        /*----< write line of text >-------*/
        public static void Line(string msg = "")
        {
            if (msg.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\n  {0}", msg);
                Console.ResetColor();
            }
            else
                Console.Write("\n");
        }
    }

    ///////////////////////////////////////////////////////////////////
    // TestTestU class
    class TestTestU
    {
#if(TEST_TESTU)
        static void Main(string[] args)
        {
            PrintTool.title("Testing function: TestUtilities.title");
            PrintTool.Line("Testing function TestUtilities.putLine");

            PrintTool.title("ClientEnvironment.verbose = false, This message should not show");

            PrintTool.title("ClientEnvironment.verbose = true, This message should be printed");
            PrintTool.Line("End of the demonstration");
        }
    }
#endif
}

