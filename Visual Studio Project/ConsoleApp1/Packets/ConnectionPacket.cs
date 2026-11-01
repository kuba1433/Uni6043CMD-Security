using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// Client sends: HEADER
    /// Server sends: HEADER SEPERATOR SEPERATOR USERTIMEOUT
    /// The client informs the server of their existence via the message, which indicates they're at the login screen
    /// A user that has ONLY sent a 'ConnectionPacket' are not a part of the set of clients that are logged in, so are exempt from most features, like timeout or receiving most, if not, all other packets
    /// 
    /// The client gets the sentinal string and the timeout timer as a response, so when they're logged in, can ping the server when they are at risk of getting disconnected
    /// 2 seperators are provided because the client is unaware of the seperator and its length and needs the header at the front, so 2 seperators exist to allow the client to distinguish from the timeout timer portion of the packet
    /// (this doesn't need to be done 2 ways because the header is constant between the 2)
    /// 
    /// Ideally, a long timeout timer would disconnect users that just have the login screen open indefinately to save resources, this isn't simulated
    /// </summary>
    class ConnectionPacket : Packet
    {
        public ConnectionPacket()
        {
            sHeader = "Connected";
        }

        protected override string PacketResponse(string sMessage)
        {
            return $"{sHeader}{Server.sSeperator}{Server.sSeperator}{Server.iTimeoutTimer}";  // doesn't use 'SimplePacket' since this packet's format is a special case
        }
    }
}
