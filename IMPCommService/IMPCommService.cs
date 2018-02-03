/////////////////////////////////////////////////////////////////////
// IMPCommService.cs - service interface for MessagePassingComm    
// Author: Beier Chen, bchen22@syr.edu
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
// Source: Jim Fawcett
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * provides the service interface for MessagePassingComm    
 * 
 * 
 * Required Files
 * --------------------
 * IMPCommService.cs
 * 
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
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
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading;

namespace Federation
{
    using Command = String;             // Command is key for message dispatching, e.g., Dictionary<Command, Func<bool>>
    using EndPoint = String;            // string is (ip address or machine name):(port number)
    using Argument = String;
    using ErrorMessage = String;

    public struct ServiceEnvironment
    {
        public static string ServiceStorage { get; set; }
        public static string baseAddress { get; set; }
    }

    [ServiceContract(Namespace = "Federation")]
    public interface IMessagePassingComm
    {
        /*----< support for message passing >--------------------------*/
        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract
        CommMessage getMessage();

        /*----< support for sending file in blocks >-------------------*/
        [OperationContract]
        bool openFileForWrite(string name, string serviceStorage);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract(IsOneWay = true)]
        void closeFile();
    }

    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            connect,           // initial message sent on successfully connecting
            [EnumMember]
            request,           // request for action from receiver
            [EnumMember]
            reply,             // response to a request
            [EnumMember]
            notify,             // notify status or result
            [EnumMember]
            closeSender,       // close down client
            [EnumMember]
            closeReceiver,      // close down server for graceful termination
            [EnumMember]
            procError       // processing error
        }

        public enum Command
        {
            [EnumMember]
            connect,  // sent to destination when connect succeeds
            [EnumMember]
            TestDriverList, // from client to repo, request test driver list
            [EnumMember]
            TestedList, // from client to repo, request tested files list
            [EnumMember]
            BuildReqList, // from client to repo, to store build request
            [EnumMember]
            BuildRequest, // from client to repo,  from repo to build server, from build server to child builder
            [EnumMember]
            Ready, // from child builder to build server, to notify its status
            [EnumMember]
            FileRequest, // from child builder to repo, from testhardness to child builder
            [EnumMember]
            BuildLog, // from child builder to repo
            [EnumMember]
            TestRequest, // from child builder to testhardness
            [EnumMember]
            TestResult // from testhardness to client, to notify test result
        }

        /*----< constructor requires message type >--------------------*/
        public CommMessage(MessageType mt)
        {
            type = mt;
        }

        /*----< data members - all serializable public properties >----*/
        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public string author { get; set; }

        [DataMember]
        public Command command { get; set; }

        [DataMember]
        public string message { get; set; } // need to modify later

        [DataMember]
        public List<Argument> arguments { get; set; } = new List<Argument>();

        [DataMember]
        public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

        [DataMember]
        public ErrorMessage errorMsg { get; set; } = "no error";

        public void show()
        {          
            Console.Write("\n  CommMessage:");
            Console.Write("\n    MessageType :  {0}", type.ToString());
            Console.Write("\n    to          :  {0}", to);
            Console.Write("\n    from        :  {0}", from);
            Console.Write("\n    author      :  {0}", author);
            Console.Write("\n    command     :  {0}", command);
            Console.Write("\n    ThreadId    :  {0}", threadId);

            if (message != null)
            {
                Console.Write("\n    message     :  ");
                Console.Write("{0}", message);
            }
            
            if (arguments.Count > 0 && command != Command.TestRequest)
            {
                Console.Write("\n    arguments:");
                foreach (string arg in arguments)
                    Console.Write("\n                {0} ", arg);
            }
            
            if (errorMsg != "no error")
                Console.Write("\n    errorMsg    : {0}\n", errorMsg);

            Console.WriteLine("\n");
        }
    }
}

