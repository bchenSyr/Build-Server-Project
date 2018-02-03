/////////////////////////////////////////////////////////////////////
// Repo.cs - Repository Server of the Federation
// Author: Beier Chen, bchen22@syr.edu
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
// Source: Jim Fawcett
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * 
 * Public methods:
 * ------------------------------------------
 * SetUpComm() - set up message-passing communication service 
 * StartThreadProc() - define processing for receive thread
 * InitializeDispatcher() - define how each message will be processed
 * 
 * Required Files
 * --------------------
 * Repo.cs
 * Environment.cs, PrintTool.cs
 * IMPCommService.cs, Receiver.cs, Sender.cs, MessageDispatcher.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 8 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // RepoServer class
    public class RepoServer
    {
        IFileMgr FileManager { get; set; } = FileMgrFactory.create(FileMgrType.Local); // file manager of RepoServer
        MessageDispatcher dispatcher { get; set; } = new MessageDispatcher(); // message dispatcher of RepoServer

        static Receiver receiver { get; set; } // receiver of RepoServer
        static Sender sender { get; set; } // sender of RepoServer
        private CommMessage replyClientMsg; // reply message for sending 
        private CommMessage requestMsg; // request message for sending
        private CommMessage fileReturnMsg; // request message for sending

        List<string> buildRequestList { get; set; } = new List<string>();

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
                                                                 new Dictionary<string, Func<CommMessage, CommMessage>>();

        //---< initialize server processing 
        public RepoServer()
        {
            SetUpComm();
            InitializeDispatcher();
        }

        //---< set up message-passing communication service 
        void SetUpComm()
        {
            receiver = new Receiver();
            receiver.start(RepoServerEnvironment.baseAdd, RepoServerEnvironment.port);
            sender = new Sender(RepoServerEnvironment.baseAdd, RepoServerEnvironment.port);

            replyClientMsg = new CommMessage(CommMessage.MessageType.reply)
            {
                author = "Repo",
                from = RepoServerEnvironment.endPoint,
                to = ClientEnvironment.endPoint
            };
            requestMsg = new CommMessage(CommMessage.MessageType.request)
            {
                author = "Repo",
                from = RepoServerEnvironment.endPoint,
                to = BuildServerEnvironment.endPoint,
                command = CommMessage.Command.BuildRequest
            };
            fileReturnMsg = new CommMessage(CommMessage.MessageType.reply)
            {
                author = "Repo",
                from = RepoServerEnvironment.endPoint,
                //to = "http://localhost:" + "8083" + "/IMessagePassingComm",
                command = CommMessage.Command.FileRequest
            };
        }
      
        //---< define how each message will be processed 
        void InitializeDispatcher()
        {
            //----< if received TestDriverList request message from client, return file list
            Func<CommMessage, CommMessage> returnTestDriverList = (CommMessage msg) =>
            {             
                replyClientMsg.command = msg.command;
                replyClientMsg.arguments = FileManager.getFiles(RepoServerEnvironment.TestDriverDir).ToList<string>();
                return replyClientMsg;
            };
            dispatcher.AddCommand(CommMessage.Command.TestDriverList, returnTestDriverList);

            //----< if received returnTestedFileList request message from client, return file list
            Func<CommMessage, CommMessage> returnTestedFileList = (CommMessage msg) =>
            {
                replyClientMsg.command = msg.command;
                string testDriverName = msg.message.Substring(0, msg.message.Length - 3); // get the file name without extionsion
                string testedFileDir = RepoServerEnvironment.root + "Test/" + testDriverName + "/TestedCode";
                replyClientMsg.message = "StorageOfRepo/Test/" + testDriverName + "/TestedCode"; // return the file directory
                replyClientMsg.arguments = FileManager.getFiles(testedFileDir).ToList<string>();
                return replyClientMsg;
            };
            dispatcher.AddCommand(CommMessage.Command.TestedList, returnTestedFileList);

            //----< if received store request message from client, store the XML build request string
            Func<CommMessage, CommMessage> returnBuildReqList = (CommMessage msg) =>
            {
                replyClientMsg.command = msg.command;
                buildRequestList.Add(msg.message);
                string dir = RepoServerEnvironment.root + "BuildRequests";
                replyClientMsg.message = "StorageOfRepo/BuildRequests/"; // return the file directory
                replyClientMsg.arguments = FileManager.getFiles(dir).ToList<string>();
                return replyClientMsg;
            };
            dispatcher.AddCommand(CommMessage.Command.BuildReqList, returnBuildReqList);

            //----< if received sendBuildRequest request message from client, send build request to Build Server
            Func<CommMessage, CommMessage> sendBuildRequest = (CommMessage msg) =>
            {
                PrintTool.title(string.Format("Demonstrating requirement 4: Repository server send build request to Build Server"));
                int num = int.Parse(msg.message.Substring(msg.message.Length - 5, 1)); // build request name
                Console.WriteLine(buildRequestList[num - 1]);
                requestMsg.message = buildRequestList[num - 1];
                return requestMsg;
            };
            dispatcher.AddCommand(CommMessage.Command.BuildRequest, sendBuildRequest);

            //----< if received FileRequest request message from child builder 
            Func<CommMessage, CommMessage> sendFile = (CommMessage msg) =>
            {
                return SendFiles(msg);           
            };
            dispatcher.AddCommand(CommMessage.Command.FileRequest, sendFile);    
        }

        //---< send files to child builder on command 
        private CommMessage SendFiles(CommMessage msg)
        {
            PrintTool.title(string.Format("Demonstrating requirement 4: Repository server send files to Build Server on command"), '-');

            string testName = msg.message;
            string testDriver = msg.arguments[0].Substring(0, msg.arguments[0].Length - 3);
            string childBuilderPort = msg.author;
            List<string> fileList = new List<string>();

            string childBuilderStorage = BuildServerEnvironment.root + childBuilderPort + "/" + testName;
            // eg ...StorageOfChildBuilder/8083/Test1

            if (!System.IO.Directory.Exists(childBuilderStorage))
                System.IO.Directory.CreateDirectory(childBuilderStorage);

            string repoStorage = RepoServerEnvironment.TestDir + "/" + testDriver;
            Console.WriteLine("\n the repo storage is {0}", repoStorage);
            // eg .../StorageOfRepo/Test/TestDriver1

            fileList = FileManager.getFiles(repoStorage).ToList<string>();

            // send all the files related to test driver, except tesed fils
            foreach (string file in fileList)
            {
                sender.postFile(file, repoStorage, childBuilderStorage);
                PrintTool.Line(string.Format("\n    - Send file \"{0}\" from \"{1}\" to \"{2}\" Succeeded",
                                                                                                                                     file, repoStorage, childBuilderStorage));
            }

            // send files in the subdirectory
            return sendSubDir(repoStorage, childBuilderStorage, msg);
        }

        //---< helper function for sendFiles
        private CommMessage sendSubDir(string repoStorage, string childBuilderStorage, CommMessage msg)
        {
            List<string> dirList = new List<string>();
            List<string> fileList = new List<string>();
            dirList = FileManager.getDirs(repoStorage).ToList<string>();
            foreach (string dir in dirList)
            {
                Console.WriteLine("\n the sub directory is {0} ", dir);
                string subDir = repoStorage + "/" + dir;
                if (dir == "Properties")
                {
                    string childPropertiesDir = childBuilderStorage + "/" + dir;

                    if (!System.IO.Directory.Exists(childPropertiesDir))
                        System.IO.Directory.CreateDirectory(childPropertiesDir);

                    fileList = FileManager.getFiles(subDir).ToList<string>();
                    foreach (string file in fileList)
                    {
                        sender.postFile(file, subDir, childPropertiesDir);
                        PrintTool.Line(string.Format("\n    - Send file \"{0}\" from \"{1}\" to \"{2}\" Succeeded",
                                                                                                                                             file, subDir, childPropertiesDir));
                    }
                }
                else if (dir == "TestedCode") // send client selected tested files to child builder
                {
                    for (int i = 1; i < msg.arguments.Count; i++)
                    {
                        sender.postFile(msg.arguments[i], subDir, childBuilderStorage);
                        PrintTool.Line(string.Format("\n    - Send file \"{0}\" from \"{1}\" to \"{2}\" Succeeded",
                                                                                                                    msg.arguments[i], subDir, childBuilderStorage));
                    }
                }
            }
            fileReturnMsg.to = msg.from;
            fileReturnMsg.arguments.Add(msg.message); // test name
            fileReturnMsg.message = childBuilderStorage;
            return fileReturnMsg;
        }

        //---< define processing for Repo's receive thread 
        public void StartThreadProc()
        {
            PrintTool.title("Start receive thread of Repo", '-');
            while (true)
            {
                try
                {
                    CommMessage msg = receiver.getMessage();                  
                    if (msg.type != CommMessage.MessageType.connect)
                    {                 
                        PrintTool.Line(string.Format("- Repo received a {0} message : {1}", 
                                                                            msg.type.ToString(), msg.command.ToString()));
                        
                        if (msg.command == CommMessage.Command.FileRequest)
                        {
                            msg.show();
                            dispatcher.DoCommand(msg.command, msg);
                            sender.postMessage(fileReturnMsg);
                        }
                        else if (msg.type == CommMessage.MessageType.notify)
                        {
                            msg.show();
                        }
                        else
                        {
                            msg.show();
                            CommMessage react = dispatcher.DoCommand(msg.command, msg);
                            PrintTool.Line(string.Format("- Repo reacts on {0} request: send a {1} message", msg.command, react.type.ToString()));
                            sender.postMessage(react);
                        }
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
                        PrintTool.Line(string.Format("- Repo received a connect message from {0}", from));
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        static void Main(string[] args)
        {
            int RepoPort = RepoServerEnvironment.port;
            Console.Title =  "Repository Server Console";

            PrintTool.title(string.Format("Repository Server Console, port number: {0}", RepoPort), '*');

            PrintTool.title(string.Format("Demonstrating requirement 4: Repository server supports client browsing to find files to build"), '-');

            RepoServer myRepo = new RepoServer();         
            myRepo.StartThreadProc();
        }
    }
}
