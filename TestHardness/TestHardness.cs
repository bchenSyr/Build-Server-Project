/////////////////////////////////////////////////////////////////////
// TestHardness.cs - Loads and executes tests      
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook   
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * The Test Harness use message-communication service.
 *  It receive test request and files from Child Builder, and load each test library and execute it,
 *  and then submit the results of testing to the Repository.

 * Public Interface:
 * ===================
 * StartThreadProc() - define processing for receive thread
 * 
 *  Required Files
 * --------------------
 * TestHardness.cs
 * Environment.cs, PrintTool.cs, DllLoader.cs
 * IMPCommService.cs, Receiver.cs, Sender.cs, MessageDispatcher.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 8 2017
 * - first release
 * 
 */
using System;
using System.IO;
using System.Xml;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // TestHardness class
    public class TestHardness
    {
        DllLoader dllLoader = new DllLoader();
        MessageDispatcher dispatcher { get; set; } = new MessageDispatcher(); // message dispatcher of test hardndess

        static Receiver receiver { get; set; } // reveicer of test hardndess
        static Sender sender { get; set; } // sender of test hardndess
        private CommMessage testResultMsg;  // notify message for sending 
        string testResult { get; set; } = "default";

        public TestHardness()
        {
            SetUpComm();
            InitializeDispatcher();
            StartThreadProc();
        }

        //---< private method: set up message-passing communication service 
        private void SetUpComm()
        {
            receiver = new Receiver();
            receiver.start(TestHardnessEnvironment.baseAdd, TestHardnessEnvironment.port);

            sender = new Sender(TestHardnessEnvironment.baseAdd, TestHardnessEnvironment.port);

            // send test result to repo
            testResultMsg = new CommMessage(CommMessage.MessageType.notify)
            {
                author = "TestHardness",
                from = TestHardnessEnvironment.endPoint,
                command = CommMessage.Command.TestResult,
                to = RepoServerEnvironment.endPoint
            };
        }

        //---< private method: define how each message will be processed 
        void InitializeDispatcher()
        {
            //----< if received test request from child builder, begin load test library and execute
            Func<CommMessage, CommMessage> loadAndExecute = (CommMessage msg) =>
            {
                string dllName = msg.message; // test library
                LoadTest(dllName);
                MakeTestLog(msg);
                return testResultMsg;
            };
            dispatcher.AddCommand(CommMessage.Command.TestRequest, loadAndExecute);
        }

        //---< define processing for TestHardness's receive thread 
        public void StartThreadProc()
        {
            PrintTool.title("Start receive thread of Test Hardness", '-');
            while (true)
            {
                try
                {
                    CommMessage msg = receiver.getMessage();
                    if (msg.command == CommMessage.Command.TestRequest)
                    {
                        PrintTool.Line(string.Format("- Test Hardness received a TestRequest message"));
                        msg.show();
                        dispatcher.DoCommand(msg.command, msg); // load and execute
                        PrintTool.title("Test Hardness submits the results of testing to the Repository.");
                        testResultMsg.show();
                        sender.postMessage(testResultMsg);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        //---< private method: make a test log to record test result
        private void MakeTestLog(CommMessage msg)
        {
            testResultMsg.arguments.Clear();
            testResultMsg.arguments.Add(string.Format("Test Log"));
            testResultMsg.arguments.Add(msg.arguments[0]); // author
            testResultMsg.arguments.Add(msg.arguments[1]); // test name
            string timeStamp = DateTime.Now.ToString();
            testResultMsg.arguments.Add(string.Format("Test Begin Time: {0}", timeStamp));
            string path = Path.GetFullPath(TestHardnessEnvironment.root);
            testResultMsg.arguments.Add(string.Format("Test Library Path: {0}", path));
            testResultMsg.arguments.Add(string.Format("Test Library Name: {0}", msg.message + ".dll"));
            testResultMsg.arguments.Add(string.Format("Test Result: {0}", testResult));
        }

        //---< private method: load test library
        private void LoadTest(string dllName)
        {
            PrintTool.Line(string.Format("- Test Hardness begins to load test and execute"));
            testResult = dllLoader.LoadingDll(dllName);
        }

        static void Main(string[] args)
        {
            Console.Title = "Test Hardness Console";
            PrintTool.title(string.Format("Test Hardness Console, port number: {0}", TestHardnessEnvironment.port), '*');

            PrintTool.title("Demonstration requirement 9: " +
                                     "The Test Harness loads each test library it receives and execute it. ");
            TestHardness testHardness = new TestHardness();
        }

     }
}
