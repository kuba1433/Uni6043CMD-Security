using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : HEADER USERNAME PASSWORD
    /// SERVER SENDS : HEADER CONFIRMATION(0/1) MESSAGE
    /// This works very similarly to 'LoginPacket', with a few changes: a username match is now invalid and no password comparison needs to be made, just storing
    /// </summary>
    class RegisterPacket : Packet
    {
        Client cliRelevant;
        public RegisterPacket()
        {
            sHeader = "Register";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            cliRelevant = cliSender;
            return new Client[1] { cliSender };
        }

        protected override string PacketResponse(string sMessage)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            if (!Server.SQLHandling.ContainsUsername(asParams[0]))
            {
                cliRelevant.SetUsername(asParams[0]);
                Server.SQLHandling.AddNewCredential(asParams[0], asParams[1]);
                Server.SendServerChatMessage($"{asParams[0]} has joined");
                Server.SpoofPacket(Server.PacketTypes.GetStats, null, cliRelevant.GetPlayerGameObjectID().ToString(), "0");
                return SimplePacket(sHeader, 1.ToString());
            }
            else
            {
                return SimplePacket(sHeader, 0.ToString(), "Username Taken");
            }
        }
    }
}
