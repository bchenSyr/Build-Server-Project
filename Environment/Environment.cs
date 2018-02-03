/////////////////////////////////////////////////////////////////////
// Environment.cs - define environment for components of federation
// Author: Beier Chen, bchen22@syr.edu
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 8 2017
 * - first release
 * 
 */

namespace Federation
{
    public struct Environment
    {
        public static string root { get; set; }
        public static string endPoint { get; set; }
        public static string address { get; set; }
        public static int port { get; set; }
        public static bool verbose { get; set; }
    }

    public struct ClientEnvironment
    {
        public static string root { get; set; } = "../../../StorageOfClient";
        public static string endPoint { get; set; } = "http://localhost:8080/IMessagePassingComm";
        public static string baseAdd { get; set; } = "http://localhost";
        public static int port { get; set; } = 8080;
    }

    public struct RepoServerEnvironment
    {
        public static string root { get; set; } = "../../../StorageOfRepo/";
        public static string TestDriverDir { get; set; } = "../../../StorageOfRepo/TestDriver";
        public static string TestDir { get; set; } = "../../../StorageOfRepo/Test";
        public static string endPoint { get; set; } = "http://localhost:8081/IMessagePassingComm";
        public static string baseAdd { get; set; } = "http://localhost";
        public static int port { get; set; } = 8081;
    }

    public struct BuildServerEnvironment
    {
        public static string root { get; set; } = "../../../StorageOfChildBuilder/";
        public static string endPoint { get; set; } = "http://localhost:8082/IMessagePassingComm";
        public static string baseAdd { get; set; } = "http://localhost";
        public static int port { get; set; } = 8082;
    }

    public struct TestHardnessEnvironment
    {
        public static string root { get; set; } = "../../../StorageOfTestHardness";
        public static string endPoint { get; set; } = "http://localhost:8090/IMessagePassingComm";
        public static string baseAdd { get; set; } = "http://localhost";
        public static int port { get; set; } = 8090;
    }
}
