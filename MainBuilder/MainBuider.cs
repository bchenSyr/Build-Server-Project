/////////////////////////////////////////////////////////////////////
// MainBuilder.cs - Main Builder of Build Server
// Author: Beier Chen, bchen22@syr.edu
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Main Builder can creates a specified number of Child Builders  on command,
 * and be able to close Child Builder if required by GUI Client protoype.
 * Main Builder send and receive messages or files with Child Builder through WCF.
 * 
 * Public Interface:
 * ===================
 * ProcessCreator class:
 * CreateProcess() - creates Child Builder process
 *
 * MainBuilder class
 * StartThreadProc() - define processing for Main Builder's receive thread
 * CreateChildBuilder() - create a specied number of Child Builders
 * 
 * Required Files
 * --------------------
 * MainBuider.cs
 * XMLHandler.cs, Environment.cs, PrintTool.cs, BlockingQueue.cs
 * IMPCommService.cs, Receiver.cs, Sender.cs, MessageDispatcher.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 6 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // ProcessCreator class
    public class ProcessCreator
    {
        //----<creates Child Builder process
        public static bool CreateProcess(int ChildPortNum)
        {
            Process proc = new Process();
            string fileName = "..\\..\\..\\ChildBuilder\\bin\\debug\\ChildBuilder.exe";
            string absFileSpec = Path.GetFullPath(fileName);

            Console.Write("\n  attempting to start {0}", absFileSpec);
            string commandline = ChildPortNum.ToString() + " " + BuildServerEnvironment.port.ToString();
            try
            {
                Process.Start(fileName, commandline);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }
    }

    ///////////////////////////////////////////////////////////////////
    // MainBuilder class
    public class MainBuilder
    {
        static int numChild { get; set; } // number of Child Builder to be created
        static Receiver receiver { get; set; } // reveicer of Main Builder
        static Sender sender { get; set; } // sender of Main Builder
        private CommMessage requestMsg { get; set; }  // request message for sending 
        MessageDispatcher dispatcher { get; set; } = new MessageDispatcher(); // message dispatcher of RepoServer
        BuildRequestParser buildRequestParser { get; set; } = new BuildRequestParser();

        static BlockingQueue<string> testNameQ { get; set; } = new BlockingQueue<string>();
        static BlockingQueue<string> readyQ { get; set; } = new BlockingQueue<string>();
        static BlockingQueue<string> readyPortQ { get; set; } = new BlockingQueue<string>();
        static BlockingQueue<List<string>> BuildReqQ = new BlockingQueue<List<string>>();
        List<string> buildReqParseResult { get; set; } = new List<string>();

        //----< constructor
        public MainBuilder(int NumChild)
        {
            numChild = NumChild;
            SetUpComm();
            InitializeDispatcher();
        }

        //---< set up message-passing communication service 
        private void SetUpComm()
        {
            receiver = new Receiver();
            receiver.start(BuildServerEnvironment.baseAdd, BuildServerEnvironment.port);
            sender = new Sender(BuildServerEnvironment.baseAdd, BuildServerEnvironment.port);

            // send to child builder
            requestMsg = new CommMessage(CommMessage.MessageType.request)
            {
                author = "Main Builder",
                from = BuildServerEnvironment.endPoint,
                command = CommMessage.Command.BuildRequest,
            };
        }

        //----< create a specied number of Child Builders
        public static void CreateChildBuilder()
        {
            for (int i = 1; i <= numChild; ++i)
            {
                if (ProcessCreator.CreateProcess(i))
                {
                    PrintTool.Line(string.Format(" - Creat Child Process {0} Succeeded", i));
                }
                else
                {
                    PrintTool.Line(" - failed");
                }
            }
        }

        //---< define how each message will be processed 
        private void InitializeDispatcher()
        {
            //----< if received BuildRequest request message from repo, parse the build request xml structure string
            Func<CommMessage, CommMessage> parseAndEnqBuildReq = (CommMessage msg) =>
            {
                ParseXML(msg.message);
                PrintTool.Line("Parse Build Request: \n");
                foreach (string str in buildReqParseResult) { Console.WriteLine("   {0}",str); }         
                return msg; // no use
            };
            dispatcher.AddCommand(CommMessage.Command.BuildRequest, parseAndEnqBuildReq);

            //----< if received Ready message from child builder
            Func<CommMessage, CommMessage> enqReadyPort = (CommMessage msg) =>
            {
                readyQ.enQ(msg.from);
                readyPortQ.enQ(msg.author);
                return msg; // no use
            };
            dispatcher.AddCommand(CommMessage.Command.Ready, enqReadyPort);
        }

        //----< define processing for Main Builder's receive thread 
        public void StartThreadProc()
        {
            PrintTool.title("Start receive thread of Main Builder", '-');
            while (true)
            {
                while (readyQ.size() != 0 && testNameQ.size() != 0)
                {
                    CheckReq();
                }

                CommMessage msg = receiver.getMessage();
                if (msg.type != CommMessage.MessageType.connect)
                {
                    PrintTool.Line(string.Format("- Main Builder received a {0} message : {1}", msg.type.ToString(), msg.command.ToString()));
                    msg.show();
                    dispatcher.DoCommand(msg.command, msg);
                }
                else
                {
                    string fromPort = msg.from.Substring(17, 4);
                    string from = "default";
                    if (fromPort == "8080") from = "Client (8080)";
                    else if (fromPort == "8081") from = "Repo (808)";
                    else if (fromPort == "8082") from = "Build Server (8082)";
                    else if (fromPort == "8090") from = "Test Hardness (8090)";
                    else from = string.Format("Child Builder({0})", fromPort);
                    PrintTool.Line(string.Format("- Main Builder received a connect message from {0}", from));
                }

                while (readyQ.size() != 0 && testNameQ.size() != 0)
                {
                    CheckReq();
                }
            }
        }

        //---< send build request to available child builder
        private void CheckReq()
        {
            PrintTool.Line(string.Format("The size of Build Reqest Queue is {0}", BuildReqQ.size()));
            string childPort = readyPortQ.deQ();
            PrintTool.Line(string.Format("The Child Builder {0} is ready", childPort));
            PrintTool.Line(string.Format("Main Builder is sending Build Request " +
                                                                        "to Child Builder {0} ", childPort));

            requestMsg.to = readyQ.deQ();
            requestMsg.arguments = BuildReqQ.deQ();
            requestMsg.message = testNameQ.deQ();
            requestMsg.show();
            sender.postMessage(requestMsg);
        }

        //---< parse build request xml string
        private void ParseXML(string buildRequest)
        {
            List<string> parseResult = buildRequestParser.ParseString(buildRequest);
            List<string> fileReqArg = new List<string>();
            int count = parseResult.Count, i = 0;
            buildReqParseResult.Clear();
            while (i < parseResult.Count) 
            {
                if (parseResult[i] == "ATest")
                {
                    fileReqArg.Clear();
                    buildReqParseResult.Add(string.Format("Test number: {0}", parseResult[i + 1]));
                    buildReqParseResult.Add(string.Format("Test Name: {0}", parseResult[i + 2]));
                    buildReqParseResult.Add(string.Format("Test Driver: {0}", parseResult[i + 3]));
                    testNameQ.enQ(parseResult[i + 2]);
                    fileReqArg.Add(parseResult[i + 3]); // test driver name
                    i = i + 4;
                    while (true)
                    {
                        fileReqArg.Add(parseResult[i]); // tested file name
                        buildReqParseResult.Add(string.Format("Tested files: {0}", parseResult[i]));
                        i++;

                        if (i == parseResult.Count || parseResult[i] == "ATest") // if reach the last line
                        {
                            List<string> temp = new List<string>();
                            for (int j = 0; j < fileReqArg.Count(); j++)
                            {
                                temp.Add(fileReqArg[j]);
                            }
                            BuildReqQ.enQ(temp);
                            break;
                        }
                    }
                }
                else
                {
                    buildReqParseResult.Add(parseResult[i]);
                    i++;
                }
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "Main Builder Console";
            PrintTool.title(string.Format("Main Builder Console, port number: {0}", BuildServerEnvironment.port), '*');
            
            int NumChild = 2;
            MainBuilder mainBuilder = new MainBuilder(NumChild);

            PrintTool.title("Demonstrating Requirement 5: Main Builder creates a specified number of " +
                                      "Child Builders on command");

            MainBuilder.CreateChildBuilder();
   
            // start the receive thread of MainBuilder
            mainBuilder.StartThreadProc();
        }
    }
}
