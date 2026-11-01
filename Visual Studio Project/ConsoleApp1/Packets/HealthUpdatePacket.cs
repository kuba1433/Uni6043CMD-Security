using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* HealthUpdate
    * CLIENT SENDS (SERVER SPOOFS) : HEADER CLIENTID
    * SERVER SENDS : HEADER HEALTH
    * Tells the client its health to coordinate UI
    */
    class HealthUpdatePacket : Packet
    {
        int iHealthToSend = 0;
        public HealthUpdatePacket()
        {
            sHeader = "HealthUpdate";
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            iHealthToSend = cliReceiver.GetHealth();
            return iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, iHealthToSend.ToString());
        }
    }
}
