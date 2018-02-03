/////////////////////////////////////////////////////////////////////
// Receiver.cs - service for MessagePassingComm    
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * provides the service of server side for MessagePassingComm   
 * 
 * Public methods:
 *   -------------------------------------------
 *   - start: creates instance of ServiceHost which services incoming messages
 *   - postMessage: Sender proxies call this message to enqueue for processing
 *   - getMessage: called by Receiver application to retrieve incoming messages
 *   - close: closes ServiceHost
 *   - openFileForWrite : opens a file for storing incoming file blocks
 *   - writeFileBlock: writes an incoming file block to storage
 *   - closeFile: closes newly uploaded file
 * 
 * Required Files
 * --------------------
 * Receiver.cs
 * IMPCommService.cs, Sender.cs, BlockingQueue.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 6 2017
 * - first release
 * 
 */

using System;
using System.ServiceModel;
using System.Threading;
using System.IO;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // Receiver class - receives CommMessages and Files from Senders
    public class Receiver : IMessagePassingComm
    {
        public static Federation.BlockingQueue<CommMessage> rcvQ { get; set; } = null;
        ServiceHost commHost = null;
        FileStream fs = null;
        string lastError = "";
        public bool restartFailed { get; set; } = false;

        /*----< constructor >------------------------------------------*/
        public Receiver()
        {
            if (rcvQ == null)
                rcvQ = new Federation.BlockingQueue<CommMessage>();
        }

        /*----< create ServiceHost listening on specified endpoint >---*/
        public bool start(string baseAddress, int port)
        {
            try
            {
                string address = baseAddress + ":" + port.ToString() + "/IMessagePassingComm";
                //PrintTool.Line(string.Format("starting Receiver on thread {0}", Thread.CurrentThread.ManagedThreadId));
                createCommHost(address);
                restartFailed = false;
                return true;
            }
            catch (Exception ex)
            {
                restartFailed = true;
                Console.Write("\n{0}\n", ex.Message);
                Console.Write("\n  You can't restart a listener on a previously used port");
                Console.Write(" - Windows won't release it until the process shuts down");
                return false;
            }

        }

        /*----< create ServiceHost listening on specified endpoint >---*/
        public void createCommHost(string address)
        {
            WSHttpBinding binding = new WSHttpBinding();
            Uri baseAddress = new Uri(address);
            commHost = new ServiceHost(typeof(Receiver), baseAddress);
            commHost.AddServiceEndpoint(typeof(IMessagePassingComm), binding, baseAddress);
            commHost.Open();
        }

        /*----< enqueue a message for transmission to a Receiver >-----*/
        public void postMessage(CommMessage msg)
        {
            msg.threadId = Thread.CurrentThread.ManagedThreadId;
            rcvQ.enQ(msg);
        }

        /*----< retrieve a message sent by a Sender instance >---------*/
        public CommMessage getMessage()
        {
            CommMessage msg = rcvQ.deQ();
            if (msg.type == CommMessage.MessageType.closeReceiver)
            {
                close();
            }
            return msg;
        }

        /*----< close ServiceHost >------------------------------------*/
        public void close()
        {
            PrintTool.Line("closing receiver - please wait");
            //Console.Out.Flush();
            commHost.Close();
            (commHost as IDisposable).Dispose();
        }

        /*---< called by Sender's proxy to open file on Receiver >-----*/
        public bool openFileForWrite(string name, string serviceStorage)
        {
            try
            {
                string writePath = Path.Combine(serviceStorage, name);
                fs = File.OpenWrite(writePath);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }

        /*----< write a block received from Sender instance >----------*/
        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }

        /*----< close Receiver's uploaded file >-----------------------*/
        public void closeFile()
        {
            fs.Close();
        }
    }

    ///////////////////////////////////////////////////////////////////
    // TestMPCommServer class
    class TestMPCommServer
    {
#if (TEST_SERVER)
        static void Main(string[] args)
        {
            PrintTool.title("Testing MessagePassingComm");

            int commPort = 8000;
            Receiver rcvr = new Receiver();
            rcvr.start("http://localhost", commPort);

            Sender sndr = new Sender("http://localhost", commPort);

            string address = "http://localhost:" + commPort.ToString() + "/IMessagePassingComm";

            //testing service side for MessagePassingComm
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.author = "Beier Chen";
            sndMsg.to = address;
            sndMsg.from = address;
            sndr.postMessage(sndMsg);

            CommMessage rcvMsg = rcvr.getMessage();
            rcvMsg.show();

            PrintTool.title("Testing Closing Receiver");
            sndMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
            sndr.postMessage(sndMsg);

            PrintTool.title("Testing Closing Sender");
            sndMsg = new CommMessage(CommMessage.MessageType.closeSender);
            sndr.postMessage(sndMsg);

            while (true)
            {
                rcvMsg = rcvr.getMessage();
                rcvMsg.show();
                if (rcvMsg.type == CommMessage.MessageType.closeReceiver) break;
            }

            PrintTool.title("End of the demonstration");
        }
#endif
    }
}
