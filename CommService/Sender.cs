/////////////////////////////////////////////////////////////////////
// Sender.cs - service for MessagePassingComm    
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * provides the service of client side for MessagePassingComm   
 * 
 * Public methods:
 *   -------------------------------------------
 *   - connect : opens channel and attempts to connect to an endpoint, 
 *                       trying multiple times to send a connect message
 *   - close: closes channel
 *   - postMessage: posts to an internal thread-safe blocking queue, which
 *                               a sendThread then dequeues msg, inspects for destination,
 *                               and calls connect(address, port)
 *   - postFile: attempts to upload a file in blocks
 *   - getLastError: returns exception messages on method failure
 
  * Required Files
 * --------------------
 * Sender.cs
 * IMPCommService.cs, Receiver.cs, BlockingQueue.cs
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 8 2017
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
    // Sender class - sends messages and files to Receiver
    public class Sender
    {
        private IMessagePassingComm channel;
        private ChannelFactory<IMessagePassingComm> factory = null;
        private Federation.BlockingQueue<CommMessage> sndQ = null;
        private int port = 0;
        private string fromAddress = "";
        private string toAddress = "";
        Thread sndThread = null;
        int tryCount = 0, maxCount = 10;
        string lastError = "";
        string lastUrl = "";

        /*----< constructor >------------------------------------------*/
        public Sender(string baseAddress, int listenPort)
        {
            port = listenPort;
            fromAddress = baseAddress + ":" + listenPort.ToString() + "/IMessagePassingComm";
            sndQ = new Federation.BlockingQueue<CommMessage>();
            sndThread = new Thread(threadProc);
            sndThread.Start();
        }

        /*----< processing for receive thread >------------------------*/
        /*
         * - send thread dequeues send message and posts to channel proxy
         * - thread inspects message and routes to appropriate specified endpoint
         */
        void threadProc()
        {
            while (true)
            {
                CommMessage msg = sndQ.deQ();
                if (msg.type == CommMessage.MessageType.closeSender)
                    break;

                if (msg.to == lastUrl)
                    channel.postMessage(msg);
                else
                {
                    close();
                    if (!connect(msg.to))
                        continue;
                    //return;
                    lastUrl = msg.to;
                    channel.postMessage(msg);
                }
            }
        }

        /*----< attempts to connect to Receiver instance >-------------*/
        public bool connect(string baseAddress, int port)
        {
            toAddress = baseAddress + ":" + port.ToString() + "/IMessagePassingComm";
            return connect(toAddress);
        }

        /*----< attempts to connect to Receiver instance >-------------*/
        /*
         * - attempts a finite number of times to connect to a Receiver
         * - first attempt to send will throw exception of no listener at the specified endpoint
         * - to test that we attempt to send a connect message
         */
        public bool connect(string toAddress)
        {
            int timeToSleep = 500;
            createSendChannel(toAddress);
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            connectMsg.command = CommMessage.Command.connect;
            connectMsg.from = fromAddress;
            while (true)
            {
                try
                {
                    channel.postMessage(connectMsg);
                    tryCount = 0;
                    return true;
                }
                catch (Exception ex)
                {
                    if (++tryCount < maxCount)
                    {
                        PrintTool.Line("failed to connect - waiting to try again");
                        Thread.Sleep(timeToSleep);
                    }
                    else
                    {
                        PrintTool.Line("failed to connect - quitting");
                        lastError = ex.Message;
                        return false;
                    }
                }
            }
        }

        /*----< main thread enqueues message for sending >-------------*/
        public void postMessage(CommMessage msg)
        {
            sndQ.enQ(msg);
        }

        /*----< creates proxy with interface of remote instance >------*/
        public void createSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            WSHttpBinding binding = new WSHttpBinding();
            factory = new ChannelFactory<IMessagePassingComm>(binding, address);
            channel = factory.CreateChannel();
        }

        /*----< closes Sender's proxy >--------------------------------*/
        public void close()
        {
            while (sndQ.size() > 0)
            {
                CommMessage msg = sndQ.deQ();
                channel.postMessage(msg);
            }

            try
            {
                if (factory != null)
                    factory.Close();
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        /*----< uploads file to Receiver instance >--------------------*/
        // attempts to upload a file in blocks
        public bool postFile(string fileName, string source, string destination)
        {
            FileStream fs = null;
            long bytesRemaining;

            try
            {
                string path = Path.Combine(source, fileName);

                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;

                channel.openFileForWrite(fileName, destination);

                while (true)
                {
                    //long bytesToRead = Math.Min(ClientEnvironment.blockSize, bytesRemaining);
                    long bytesToRead = Math.Min(1024, bytesRemaining);
                    byte[] blk = new byte[bytesToRead];
                    long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
                    bytesRemaining -= numBytesRead;

                    channel.writeFileBlock(blk);
                    if (bytesRemaining <= 0)
                        break;
                }
                channel.closeFile();
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
            return true;
        }
    }

    ///////////////////////////////////////////////////////////////////
    // TestClient class - sends messages and files to Receiver
    class TestClient
    {
#if (TEST_CLIENT)
        static void Main(string[] args)
        {
            TestUtilities.title("Testing client side for MessagePassingComm");
            ClientEnvironment.verbose = true;

            int commPort = 8000;
            Receiver rcvr = new Receiver();
            rcvr.start("http://localhost", commPort);

            Sender sndr = new Sender("http://localhost", commPort);

            string address = "http://localhost:" + commPort.ToString() + "/Builder";

            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "testing client side for MessagePassingComm";
            sndMsg.author = "Bell";
            sndMsg.to = address;
            sndMsg.from = address;
            sndr.postMessage(sndMsg);

            CommMessage rcvMsg = rcvr.getMessage();
            rcvMsg.show();

            TestUtilities.title("Testing Closing Receiver");
            sndMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
            sndMsg.command = "Close Receiver";
            sndMsg.author = "Bell";
            sndMsg.to = address;
            sndMsg.from = address;
            sndr.postMessage(sndMsg);

            TestUtilities.title("Testing Closing Sender");
            sndMsg = new CommMessage(CommMessage.MessageType.closeSender);
            sndMsg.command = "Close Sender";
            sndMsg.author = "Bell";
            sndMsg.to = address;
            sndMsg.from = address;
            sndr.postMessage(sndMsg);

            while (true)
            {
                rcvMsg = rcvr.getMessage();
                rcvMsg.show();

                if (rcvMsg.type == CommMessage.MessageType.closeReceiver) break;

                if (rcvMsg.command == null) continue;
            }

            TestUtilities.title("End of the demonstration");
        }
#endif
    }
}