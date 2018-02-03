/////////////////////////////////////////////////////////////////////
// DllLoader.cs - Loading Dynamic Link Libraries in specified location                
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #2, CSE 681 Fall2017                                       
// Environment: Windows 10 Education, MacBook                                       
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Loading test dll for TestHardness.
 * 
* Public Interface:
 * ---------------
 * LoadingDll() - load libraries from a specified path, 
 *                          call all methods that have no arguments, using dynamic invocation.
 *                          
 * Required Files
 * --------------------
 * DllLoader.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Oct 8 2017
 * - first release
 * 
 */

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace Federation
{
  public class DllLoader
  {
        //---< load libraries from a specified path
        public string LoadingDll(string dllName)
        {
            dllName = dllName + ".dll";
            string path = TestHardnessEnvironment.root;
            string[] Libraries = Directory.GetFiles(path, dllName);
            bool testReturn = false;
            string result;
            foreach (string library in Libraries)
            {
                Console.WriteLine("\n Lib is {0}", library); 
                Assembly assem = Assembly.LoadFrom(library);
                Type[] types = assem.GetExportedTypes();

                foreach (Type t in types)
                {
                    if (t.IsClass && !t.IsAbstract)
                    {
                        object obj = Activator.CreateInstance(t);
                        MethodInfo[] mis = t.GetMethods();
                        foreach (MethodInfo mi in mis)
                        {
                            if (mi.DeclaringType != typeof(object)) // don't call base members
                            {
                                // don't call if method has arguments
                                try
                                {
                                    if (mi.GetParameters().Length == 0/* && mi.Name != "create"*/)
                                    {
                                        const BindingFlags bf =
                                          BindingFlags.Public |
                                          BindingFlags.InvokeMethod |
                                          BindingFlags.Instance;
                                        testReturn = (bool)t.InvokeMember(mi.Name, bf, null, obj, null);
                                    }
                                }
                                catch { /* continue on error - calling factory function create throws */ }
                            }
                        }
                    }
                }
            }
            if (testReturn)
            {
                result = "pass";
            }
            else result = "fail";      
            return result;
        }
  }

#if(TEST_DLLLOADER)
    class Test_DllLoader
    {
        static void Main()
        {
            DllLoader loader = new DllLoader();
            loader.LoadingDll("CSTestDemo");
        }
    }
#endif
}
