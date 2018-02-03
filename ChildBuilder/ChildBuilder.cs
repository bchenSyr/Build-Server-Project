/////////////////////////////////////////////////////////////////////
// ChildBuilder.cs - Child Builder of Build Server
// Author: Beier Chen, bchen22@syr.edu
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Use message-passing communication to access build request messages from the mother Builder process.
 * Attempt to build each library, cited in a retrieved build request, send build log to Repo
 * 
 * Public Interface:
 * --------------------
 * StartThreadProc() - define processing for Child Builder's receive thread
 * 
 * Required Files
 * --------------------
 * ChildBuilder.cs
 * Environment.cs, PrintTool.cs
 * IMPCommService.cs, Receiver.cs, Sender.cs, MessageDispatcher.cs
 * 
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 8 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;

namespace Federation
{
    public class ChildBuilder
    {
        MessageDispatcher dispatcher { get; set; } = new MessageDispatcher(); // message dispatcher of Child Builder
        public static int portNum { get; set; } = 8083; // receive port number of Child Builder itself

        static Receiver receiver { get; set; } // reveicer of Child Builder
        static Sender sender { get; set; } // sender of Child Builder
        private CommMessage fileRequestMsg; // request message for sending
        private CommMessage testRequestMsg; // request message for sending
        private CommMessage readyMsg; // ready message for sending
        private CommMessage buildLogMsg; // ready message for sending
        private string address;
        private string dllName = "default";
        List<string> fileReqArg { get; set; } = new List<string>();

        //----< constructor
        public ChildBuilder()
        {
            SetUpComm();
            InitializeDispatcher();
            sender.postMessage(readyMsg);
        }

        //---< private method: set up message-passing communication service 
        private void SetUpComm()
        {
            receiver = new Receiver();
            receiver.start(BuildServerEnvironment.baseAdd, portNum);
            address = "http://localhost:" + portNum.ToString() + "/IMessagePassingComm";
            sender = new Sender(BuildServerEnvironment.baseAdd, portNum);
            
            readyMsg = new CommMessage(CommMessage.MessageType.notify)
            {
                author = portNum.ToString(),
                from = address,
                to = BuildServerEnvironment.endPoint, // send to main builder
                command = CommMessage.Command.Ready
            };

            buildLogMsg = new CommMessage(CommMessage.MessageType.notify)
            {
                author = portNum.ToString(),
                from = address,
                to = RepoServerEnvironment.endPoint, // send to repo
                command = CommMessage.Command.BuildLog                
            };

            fileRequestMsg = new CommMessage(CommMessage.MessageType.request)
            {
                author = portNum.ToString(),             
                from = address,
                to = RepoServerEnvironment.endPoint, // send to repo
                command = CommMessage.Command.FileRequest
            };
        
            testRequestMsg = new CommMessage(CommMessage.MessageType.request)
            {
                author = portNum.ToString(),
                from = address,
                to = TestHardnessEnvironment.endPoint,   //send to test hardness
                command = CommMessage.Command.TestRequest
            };
        }

        //---< private method: define how each message will be processed 
        private void InitializeDispatcher()
        {
            //----< if received BuildRequest message from Main Builder send message to repo for files
            Func<CommMessage, CommMessage> requestFile = (CommMessage msg) =>
            {
                fileRequestMsg.arguments = msg.arguments; // files list
                fileRequestMsg.message = msg.message; // test name
                MakeBuildLog(msg);
                return fileRequestMsg;  
            };
            dispatcher.AddCommand(CommMessage.Command.BuildRequest, requestFile);

            //----< if received reply FileRequest message from repo, begin to build project
            Func<CommMessage, CommMessage> BuildProject = (CommMessage msg) =>
            {
                BuildCsproj(msg);           
                return msg;  // send to test hardness, 
            };
            dispatcher.AddCommand(CommMessage.Command.FileRequest, BuildProject);
        }

        //----< Method: define processing for Child Builder's receive thread 
        private void StartThreadProc()
        {
            PrintTool.title("Start receive thread of Child Builder", '-');                                 
            while (true)
            {
                CommMessage msg = receiver.getMessage();
                if (msg.type == CommMessage.MessageType.connect)
                {
                    string fromPort = msg.from.Substring(17, 4);
                    string from = "default";
                    if (fromPort == "8080") from = "Client (8080)";
                    else if (fromPort == "8081") from = "Repo (808)";
                    else if (fromPort == "8082") from = "Build Server (8082)";
                    else if (fromPort == "8090") from = "Test Hardness (8090)";
                    else from = string.Format("Child Builder({0})", fromPort);
                    PrintTool.Line(string.Format("- Child Builder received a connect message from {0}", from));
                }
                else
                {
                    if (msg.command == CommMessage.Command.BuildRequest)
                    {
                        PrintTool.Line(string.Format("- Child Builder received BuildRequest message from Main Builder"));
                        msg.show();
                        PrintTool.Line(string.Format("- Child Builder sends FileReqest message to Repo"));
                        dispatcher.DoCommand(msg.command, msg);
                        fileRequestMsg.show();
                        sender.postMessage(fileRequestMsg);
                    }
                    else if (msg.command == CommMessage.Command.FileRequest)
                    {
                        PrintTool.Line(string.Format("- Child Builder received files from repo, begin to build project"));
                        dispatcher.DoCommand(msg.command, msg); // build project
                        SendLogAndReady();
                    }
                }
            }
        }

        //---< private method: build project to test library
        private bool BuildCsproj(CommMessage msg)
        {
            PrintTool.title("Demonstrating requirement 7: Attempt to build each library, cited in a retrieved build request");
            string childBuilderStorage = msg.message;  // eg: childBuilderStorage = .../StorageOfChildBuilder/8083/Test1

            string[] projectFile = Directory.GetFiles(childBuilderStorage, "*.csproj");
            string projectFileName = projectFile[0];
            dllName = Path.GetFileNameWithoutExtension(projectFileName); 
            PrintTool.Line(string.Format("Begin to build {0} \n", projectFileName));
            //eg: projectFileName = .../StorageOfChildBuilder/8083/Test1/CSTestDemo.csproj";

            Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
            BuildRequestData buildRequestData =
                    new BuildRequestData(projectFileName, GlobalProperty, null, new string[] { "Rebuild" }, null);

            ConsoleLogger logger = new ConsoleLogger();
            BuildParameters buildParameters = new BuildParameters
            {
                Loggers = new List<ILogger> { logger },
                OnlyLogCriticalEvents = true
            };
        
            BuildResult buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequestData);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Build Result: {0}", buildResult.OverallResult);
            Console.ResetColor();

            buildLogMsg.arguments.Add(string.Format("Build Result: {0}", buildResult.OverallResult));

            if (buildResult.OverallResult == 0) //OverallResult = 0, build succeed; OverallResult = 1, build failed.
            {
                string sourceDir = childBuilderStorage + "/bin/debug";                
                SendTestLib(dllName, sourceDir); //send dll to test hardness
            }

            if (buildResult.OverallResult == 0)
                return true;
            else return false;
        }

        //---< private method: send  build log and ready message
        private void SendLogAndReady()
        {
            PrintTool.Line(string.Format("- Build finished, send a build log message to Repo \n"));
            buildLogMsg.show();
            sender.postMessage(buildLogMsg);

            PrintTool.Line(string.Format("- Build finished, send a ready message to Main Builder"));
            readyMsg.show();
            sender.postMessage(readyMsg);
        }

        //---< private method: make a build log to record build information
        private void MakeBuildLog(CommMessage msg)
        {
            buildLogMsg.arguments.Clear();
            buildLogMsg.arguments.Add("Build Log");
            buildLogMsg.arguments.Add("Author: Beier Chen");
            string timeStamp = DateTime.Now.ToString();
            buildLogMsg.arguments.Add(string.Format("Build Begin Time: {0}", timeStamp));
            buildLogMsg.arguments.Add(string.Format("Test Name: {0}", msg.message));
            buildLogMsg.arguments.Add(string.Format("Test Driver: {0}", msg.arguments[0]));
            buildLogMsg.arguments.Add(string.Format("Tested Files: "));
            for (int i = 1; i < msg.arguments.Count; i++)
            {
                buildLogMsg.arguments.Add(msg.arguments[i]);
            }
        }

        //---< private method: if build succeed, send files to test hardness
        private void SendTestLib(string dllName, string sourcePath)
        {
            PrintTool.title("Demonstrating requirement 8: " +
                          "If build succeed, send a test request and libraries to the Test Harness for execution," +
                          "and send the build log to the repository."); 

            testRequestMsg.message = dllName;
            testRequestMsg.arguments.Add(buildLogMsg.arguments[1]); // author
            testRequestMsg.arguments.Add(buildLogMsg.arguments[3]); // test name

            string dllNameWith = dllName + ".dll";
            if (sender.postFile(dllNameWith, sourcePath, TestHardnessEnvironment.root))
                PrintTool.Line(string.Format("\n    - Send file \"{0}\" from \"{1}\" to \"{2}\" Succeeded",
                                                                    dllNameWith, sourcePath, TestHardnessEnvironment.root));

            PrintTool.Line(string.Format("- Build Succeed, send test request message and dll to Test Hardness"));
            testRequestMsg.show();
            sender.postMessage(testRequestMsg);
        }

        static void Main(string[] args)
        {
            ChildBuilder.portNum = Int32.Parse(args[0]) + BuildServerEnvironment.port;

            Console.Title = "Child Builder Console";
            PrintTool.title(string.Format("Child Builder {0} Console, port number: {1}", args[0], portNum), '*');

            PrintTool.title("Demonstrating Requirement 3: Pool Processes shall use message-passing communication " +
                "to access messages from the mother Builder process.", '-');

            ChildBuilder childBuilder = new ChildBuilder();

            // send a "ready" message to Main Builder
            PrintTool.Line("Sending a ready message to Main Builder");

            // start the receive thread of Child Builder
            childBuilder.StartThreadProc();
        }
    }
}
