/////////////////////////////////////////////////////////////////////
// MessageDispatcher.cs - define MessageDispatcher class  
// Author: Beier Chen, bchen22@syr.edu
// Source: Jim Fawcett
// Application: Project #4, CSE 681 Fall2017                                  
// Environment: Windows 10 Education, MacBook               
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * provide message dispathching function to componets of Federation
 * 
 * Public methods:
 *   -------------------------------------------
 *   - AddCommand() : add lambda, bound to Func delegate, keyed to msg
 *   - DoCommand() :  invokes delegate bound to cmd key 
 *   
 * Required Files
 * --------------------
 * MessageDispatcher.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : Dec 7 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Federation
{
    using Msg = CommMessage;

    ///////////////////////////////////////////////////////////////////
    // MessageDispatcher class
    public class MessageDispatcher
    {
        Dictionary<Msg.Command, Func<Msg, Msg>> dispather = new Dictionary<Msg.Command, Func<Msg, Msg>>();

        /*----< add lambda, bound to Func delegate, keyed to msg >-----*/
        public void AddCommand(Msg.Command cmd, Func<Msg, Msg> action)
        {
            dispather[cmd] = action;
        }

        /*----< invokes delegate bound to cmd key >--------------------*/
        public Msg DoCommand(Msg.Command cmd, Msg msg)
        {
            if (dispather.Keys.Contains(cmd))
                return dispather[cmd].Invoke(msg);
            else // returns procError message if cmd doesn't have a bound lambda
            {
                Msg reply = new Msg(Msg.MessageType.procError);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = msg.command;
                reply.message = "couldn't process this type of command";
                return reply;
            }
        }
    }

    ///////////////////////////////////////////////////////////////////
    // TestMessageDispatcher class
    class TestMessageDispatcher
    {
#if (TEST_MP)
        static void Main(string[] args)
        {
            PrintTool.title("Testing MessageDispatcher");

            MessageDispatcher myMP = new MessageDispatcher();
  
        }
#endif
    }
}
