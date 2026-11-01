using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : PING
    /// SERVER SENDS :
    /// This packet is used by the client to tell the server it exists. 
    /// The server has a timeout for every client, where if a packet isn't received in a predetermined amount of time (see also 'ConnectionPacket') it will ignore the client indefinately.
    /// The server only does this reset if the packet fits a format, so this format is made for when the user simply has no information to express to the server that wouldn't be a waste
    /// </summary>
    class PingPacket : Packet
    {
        public PingPacket()
        {
            sHeader = "Ping";
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            return false;
        }
    }
}
