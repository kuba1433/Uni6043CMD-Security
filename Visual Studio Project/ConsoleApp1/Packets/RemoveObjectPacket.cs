using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /*
     * CLIENT SENDS : 
     * SERVER SENDS : HEADER OBJECTID
     * Used by the server to tell a client that certain objects no longer exist, the client's 'TransformDataPacket' would otherwise dead reckon them to infinity, which could be troublesome if their stationary and possibly blocking
     */
    internal class RemoveObjectPacket : Packet
    {
        public RemoveObjectPacket()
        {
            sHeader = "RemoveObject";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            return Server.GetAllLoggedInClients();
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, DeterminePacketParameters(sMessage)[0]);
        }
    }
}
