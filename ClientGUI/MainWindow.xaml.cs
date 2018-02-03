/////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Client GUI for Build Server
// Author: Beier Chen, bchen22@syr.edu
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Uses message-passing communication. 
 * Get file lists from the Repository, and select files for packaging into a test library and added to a build request structure. 
 * Send build request structures to the repository, request repository to send build request to the Build Server.
 * 
 * Required Files
 * --------------------
 * MainWindow.xaml, MainWindow.xaml.cs, CodePopUp.xaml, CodePopUp.xaml.cs
 * XMLHandler.cs, Environment.cs, PrintTool.cs
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
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using Federation;
using System.IO;
using System.Xml;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 0. Initialize: GUI sends a TestDriverList request message to Repo
    /// 1. Select file from Test Driver List: GUI sends a TestedList request message to Repo
    /// 2. Select file from Tested File List
    /// 3. Push the PackageToTestButton
    /// 4. Push the MakeBuildRequestButton: GUI sends a BuildReqList request message to Repo
    /// 5. Select file from Build Request List
    /// 6. Push the SendBuildRequestButton: GUI sends a BuildRequest message to Repo
    /// </summary>
    public partial class MainWindow : Window
    {
        MessageDispatcher dispatcher { get; set; } = new MessageDispatcher();
        BuildRequestProducer testRequestProducer { get; set; } = new BuildRequestProducer();

        // members related to set up the message-passing communication
        private static Receiver clientReceiver { get; set; }
        private static Sender clientSender { get; set; }
        private CommMessage testDriverReqMsg { get; set; }
        private CommMessage testedFileReqMsg { get; set; }
        private CommMessage buildReqMsg { get; set; }
        private CommMessage sendBuildReqMsg { get; set; }
        Thread receiveThread { get; set; } = null;

        int buildRequestNum { get; set; } = 1;// number to order the build request
        private string testDriverNameSelected { get; set; }  // the test driver being selected
        private string testedNameSelected { get; set; }  // the tested file being selected
        private string buildReqSelected { get; set; }  // the build request being selected
        string remoteStorage { get; set; } = RepoServerEnvironment.root + "BuildRequests"; 

        Queue<String> selectedFileQueue { get; set; } = new Queue<string>();

        int packagedTestNum { get; set; } = 1; // number to order the packaged test
        List<string> selectedTest { get; set; } = new List<string>(); // the final test list being selected   
        private string buildRequest { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            PackageToTestButton.IsEnabled = false;
            MakeBuildRequestButton.IsEnabled = false;
            SendBuildRequestButton.IsEnabled = false;
        }

        //---< Test GUI function
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.Title = "Client Console";
            PrintTool.title(string.Format("Client Console, port number: {0}", ClientEnvironment.port), '*');

            InitializeComponent();
            SetUpComm();
            InitializeDispatcher();

            demonstrate();
        
            receiveThread = new Thread(StartClientThreadProc);
            receiveThread.Start();
         
            getTestDriverList(); // send message to repo for test driver list   
        }

        //---< demonstrate GUI 
        private void demonstrate()
        {
            PrintTool.title("Test 1 will be a succeed build and test pass later in Child Builder and Test Hardness");
            Console.WriteLine("\n - select a test drive: TestDriver4");
            Console.WriteLine("\n - select the tested files of TestDriver4: Tested1.cs, Tested2.cs, Tested3.cs");
            testDriverNameSelected = "TestDriver4.cs"; // Select file from Test Dirver List
            SelectedTestedList.Items.Add("Tested1.cs"); // Select file from Tested File List
            SelectedTestedList.Items.Add("Tested2.cs");
            SelectedTestedList.Items.Add("Tested3.cs");
            package(); // package the selected files into a test and make a build request
            Console.WriteLine("\n, {0}", testRequestProducer.doc.ToString());

            PrintTool.title("Test 2 will be a succeed build and test fail later in Child Builder and Test Hardness");
            Console.WriteLine("\n - select a test drive: TestDriver1");
            Console.WriteLine("\n - select the tested files of TestDriver1: Tested2.cs");
            testDriverNameSelected = "TestDriver1.cs";
            //SelectedTestedList.Items.Add("Tested1.cs");// demonstrate a build fail
            SelectedTestedList.Items.Add("Tested2.cs");
            package();
            Console.WriteLine("\n, {0}", testRequestProducer.doc.ToString());

            PrintTool.title("Test 3 will be a failure build later in Child Builder");
            Console.WriteLine("\n - select a test drive: TestDriver3");
            Console.WriteLine("\n - select the tested files of TestDriver3: Tested1.cs, Tested2.cs");
            testDriverNameSelected = "TestDriver3.cs";
            SelectedTestedList.Items.Add("Tested1.cs"); 
            SelectedTestedList.Items.Add("Tested2.cs");
            package();
            Console.WriteLine("\n, {0}", testRequestProducer.doc.ToString());

            makeBuildRequest(); // make build request XML string and save it to xml file         
            buildReqSelected = "buildRequest1.xml";
            sendBuildRequest(); // ask repo to send build request to build server               
        }
        
        //---< set up communication service for Client
        private void SetUpComm()
        {
            clientReceiver = new Receiver();
            clientReceiver.start(ClientEnvironment.baseAdd, ClientEnvironment.port);
            clientSender = new Sender(ClientEnvironment.baseAdd, ClientEnvironment.port);

            testDriverReqMsg = new CommMessage(CommMessage.MessageType.request)
            {
                command = CommMessage.Command.TestDriverList,
                from = ClientEnvironment.endPoint,
                to = RepoServerEnvironment.endPoint,
                author = "Client"
            };

            testedFileReqMsg = new CommMessage(CommMessage.MessageType.request)
            {
                command = CommMessage.Command.TestedList,
                from = ClientEnvironment.endPoint,
                to = RepoServerEnvironment.endPoint,
                author = "Client"
            };

            buildReqMsg = new CommMessage(CommMessage.MessageType.request)
            {
                command = CommMessage.Command.BuildReqList,
                from = ClientEnvironment.endPoint,
                to = RepoServerEnvironment.endPoint,
                author = "Client"
            };

            sendBuildReqMsg = new CommMessage(CommMessage.MessageType.request)
            {
                command = CommMessage.Command.BuildRequest,
                from = ClientEnvironment.endPoint,
                to = RepoServerEnvironment.endPoint,
                author = "Client"
            };      
        }

        //---< define how each message will be processed >------------*/
        private void InitializeDispatcher()
        {
            Func<CommMessage, CommMessage> UpdateTestDriverList = (CommMessage msg) =>
            {              
                PrintTool.Line(string.Format(" - Client updated Test Driver List to GUI \n"));
                TestDriverList.Items.Clear();
                foreach (string fileName in msg.arguments)
                {
                    TestDriverList.Items.Add(fileName);
                }
                return msg; // no use for now 
            };
            dispatcher.AddCommand(CommMessage.Command.TestDriverList, UpdateTestDriverList);

            Func<CommMessage, CommMessage> UpdateTestedList = (CommMessage msg) =>
            {
                PrintTool.Print(string.Format(" - Client update Tested File List to GUI \n"));
                TestedDir.Text = msg.message;
                TestedList.Items.Clear();
                SelectedTestedList.Items.Clear();
                PackageToTestButton.IsEnabled = false;

                foreach (string fileName in msg.arguments)
                {
                    TestedList.Items.Add(fileName);
                }
                return msg; // no use for now 
            };
            dispatcher.AddCommand(CommMessage.Command.TestedList, UpdateTestedList);

            Func<CommMessage, CommMessage> UpdateBuildReqList = (CommMessage msg) =>
            { 
                PrintTool.Print(string.Format(" - Client update Build Request List to GUI \n"));
                BuildReqList.Items.Clear();
                BuildReqList.Items.Add(msg.arguments);
                foreach (string fileName in msg.arguments)
                {
                    BuildReqList.Items.Add(fileName);
                }
                return msg; // no use for now 
            };
            dispatcher.AddCommand(CommMessage.Command.BuildReqList, UpdateBuildReqList);         
        }

        //---< start Repo's receive thread
        private void StartClientThreadProc()
        {
            PrintTool.title("Start client's receive thread", '-');
            while (true)
            {
                CommMessage msg = clientReceiver.getMessage();

                if (msg.type == CommMessage.MessageType.connect)
                {
                    string fromPort = msg.from.Substring(17, 4);
                    string from = "default";
                    if (fromPort == "8080") from = "Client (8080)";
                    else if (fromPort == "8081") from = "Repo (8081)";
                    else if (fromPort == "8082") from = "Build Server (8082)";
                    else if (fromPort == "8090") from = "Test Hardness (8090)";
                    else from = string.Format("Child Builder({0})", fromPort);
                    PrintTool.Line(string.Format("- Client received a connect message from {0}", from));
                }
                else
                {
                    PrintTool.Line(string.Format("- Client received a {0} message: {1}", msg.type, msg.command));
                    msg.show();
                    Action toMainThrd = () =>
                    {
                        dispatcher.DoCommand(msg.command, msg);
                    };
                    Dispatcher.Invoke(toMainThrd);  // WPF's dispatcher lets child thread use window
                }
            }
        }

        //---< send a message to Repo for Test Driver List
        private void getTestDriverList()
        {
            PrintTool.title(string.Format("Demonstrating requirement 11: GUI gets file lists from the Repository"), '-');
            PrintTool.Line(string.Format("- Client send a TestDriverList request message to Repo"));
            testDriverReqMsg.show();
            clientSender.postMessage(testDriverReqMsg);
        }

        //---< select an item from TestDriverList and show it in the TextBox: SelectedTestDriver
        private void TestDriverList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            testDriverNameSelected = TestDriverList.SelectedValue as string;           
            getTestedFiles();
        }

        //---< send a message to Repo for Tested file List
        private void getTestedFiles()
        {
            SelectedTestDriver.Text = testDriverNameSelected;
            testedFileReqMsg.message = testDriverNameSelected; // ask tested files of the selected test driver

            PrintTool.Line(string.Format("- Client send a TestedList request message to Repo"));
            testedFileReqMsg.show();
            clientSender.postMessage(testedFileReqMsg);
        }

        //---< select an item from TestedList and show it in the ListBox: SelectedTestedList
        private void TestedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            testedNameSelected = TestedList.SelectedValue as string;
            SelectedTestedList.Items.Add(testedNameSelected);
            TestedList.Items.Remove(testedNameSelected);
            PackageToTestButton.IsEnabled = true;
        }

        //---< if PackageToTestButton being clicked     
        private void PackageToTestButton_Click(object sender, RoutedEventArgs e)
        {
            package();
            MakeBuildRequestButton.IsEnabled = true;
            PackageToTestButton.IsEnabled = false; // If a new test driver being selected, package operation will be enable
        }

        //---< package selected test driver and tested files into a test library
        private void package()
        {
            bool isFirstTest = false;
            string testedfileName;
            testRequestProducer.testedFiles.Clear();

            foreach (string arg in SelectedTestedList.Items)
            {
                selectedFileQueue.Enqueue(arg);
                if (arg != null)
                    testRequestProducer.testedFiles.Add(arg); // add TestedFiles to structure
            }
            SelectedTestedList.Items.Clear();

            // update PackagedTestList in the GUI & add build request xml structure
            string testName = "Test" + (packagedTestNum).ToString();
            testRequestProducer.testProject = testName; // add Test Name to structure
            testRequestProducer.testDriver = testDriverNameSelected; // add Test Driver to structure

            testName += " [";
            testName = testName + testDriverNameSelected;
            while (selectedFileQueue.Count > 0)
            {
                testedfileName = selectedFileQueue.Dequeue();
                testName = testName + " " + testedfileName;
            }
            testName += "]";
            PackagedTestList.Items.Add(testName);

            PrintTool.Line(string.Format("- Client packages selected files into a test library: {0} \n", testName));

            // make xml string
            if (packagedTestNum == 1)
            {
                testRequestProducer.author = "Beier Chen";
                isFirstTest = true;
            }

            PrintTool.Line(string.Format("- Adds {0} to the build request structure", testName));
            testRequestProducer.RequestToXML(isFirstTest);

            packagedTestNum++;
            selectedFileQueue.Clear();
        }

        //---< if SendBuildRequestButton being clicked     
        private void MakeBuildRequestButton_Click(object sender, RoutedEventArgs e)
        {
            makeBuildRequest();
            MakeBuildRequestButton.IsEnabled = false;
        }
        
        //---< send build request to repo for storage
        private void makeBuildRequest()
        {
            string localStorage = ClientEnvironment.root;

            string fileName = "buildRequest" + (buildRequestNum).ToString() + ".xml";
            BuildReqList.Items.Add(fileName);

            string savePath = System.IO.Path.Combine(localStorage, fileName);
            savePath = System.IO.Path.GetFullPath(savePath);

            // save the XML document and string
            buildRequest = testRequestProducer.doc.ToString();
            testRequestProducer.SaveXml(savePath);
            buildRequestNum++;

            // send the the XML document to repo
            clientSender.postFile(fileName, localStorage, remoteStorage);

            PrintTool.title(string.Format("Demonstrating requirement 12: GUI sends build request structures to the repository" +
                                                                                        "for storage and transmission to the Build Server."));
            buildReqMsg.message = buildRequest;
            buildReqMsg.show();
            clientSender.postMessage(buildReqMsg);
        }

        //---< select an item from BuildReqList and show it in the TextBox: SelectedBuildReq
        private void BuildReqList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buildReqSelected = BuildReqList.SelectedValue as string;
            SelectedBuildReq.Text = buildReqSelected;
            SendBuildRequestButton.IsEnabled = true;
        }

        //---< if SendBuildRequestButton being clicked
        private void SendBuildRequestButton_Click(object sender, RoutedEventArgs e)
        {
            sendBuildRequest();
        }

        //---< send a message to Repo for sending build request to Build Server
        private void sendBuildRequest()
        {
            PrintTool.title(string.Format("Demonstrating requirement 13: GUI request the repository to send a build request in its storage" +
                                                                                                                    " to the Build Server"), '-');
            PrintTool.Line(string.Format("The selected build request is {0}", buildReqSelected));
            sendBuildReqMsg.message = buildReqSelected;
            sendBuildReqMsg.show();
            clientSender.postMessage(sendBuildReqMsg);
        }

        //---< double click to see the test driver file 
        private void TestDriverList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string fileName = TestDriverList.SelectedValue as string;
            try
            {
                string TestDriverDir = RepoServerEnvironment.TestDriverDir;
                string path = System.IO.Path.Combine(TestDriverDir, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Title = "Code Window: " + fileName;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        //---< double click to see the build request xml file 
        private void BuildReqList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string fileName = BuildReqList.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(remoteStorage, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Title = "Code Window: " + fileName;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
    }
}
