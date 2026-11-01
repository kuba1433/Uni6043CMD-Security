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
    /// Client sends a username and a hashed password and the server compares its username and password to check if its valid.
    /// The server sends a message attached if there is a problem to relay to the user
    /// </summary>
    class LoginPacket : Packet
    {
        Client cliRelevant;
        public LoginPacket()
        {
            sHeader = "Login";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            cliRelevant = cliSender;
            return new Client[1] { cliSender };
        }

        protected override string PacketResponse(string sMessage)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            Server.LoginResponse lrEThis;
            
            if (Server.TESTMODE)
            {
                lrEThis = Server.LoginResponse.Valid;
            }
            else
            {
                lrEThis = Server.SQLHandling.UserLogin(asParams[0], asParams[1]);
            }
            switch (lrEThis)
            {
                case Server.LoginResponse.Valid:
                    cliRelevant.SetUsername(asParams[0]);
                    Server.SendServerChatMessage($"{asParams[0]} has joined");
                    Server.SpoofPacket(Server.PacketTypes.GetStats, null, cliRelevant.GetPlayerGameObjectID().ToString(), "0");
                    return SimplePacket(sHeader, 1.ToString());
                case Server.LoginResponse.IncorrectPassword:
                    return SimplePacket(sHeader, 0.ToString(), "Incorrect Password");
                case Server.LoginResponse.UserInGame:
                    return SimplePacket(sHeader, 0.ToString(), "User already logged in");
                default:
                    return SimplePacket(sHeader, 0.ToString(), "Username not found");
            }
        }
    }
}
