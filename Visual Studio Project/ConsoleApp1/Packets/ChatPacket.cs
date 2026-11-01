using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : HEADER MESSAGE
    /// SERVER SENDS : HEADER NAME MESSAGE
    /// 
    /// When a client sends a message it goes to the server then to all clients including itself, this allows the server to stop certain kinds of messages
    /// When the server receives a 'ChatPacket' from client 'null', then it'll treat it as a chat message from the server and will use 'S_SERVER_NAME' in place of a username
    /// </summary>
    internal class ChatPacket : Packet
    {
        string sNextMessage = "";
        string sOwner = "";
        public const string S_SERVER_NAME = "SERVER";
        bool bInappropriate = false;
        public ChatPacket()
        {
            sHeader = "Chat";
        }

        protected override void NewMessage(string sMessage)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            sNextMessage = asParams[0];
            bInappropriate = !Server.BannedPhraseFilter(DeterminePacketParameters(sMessage)[0]);
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            return iRepetitionIndex == 0 && !bInappropriate;
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            sOwner = (cliSender == null) ? S_SERVER_NAME : (string)cliSender;
            return Server.GetAllLoggedInClients();
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, sOwner, sNextMessage);
        }
    }
}
