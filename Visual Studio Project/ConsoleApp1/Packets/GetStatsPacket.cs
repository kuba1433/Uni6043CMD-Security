using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* Packets to do
      * GetStats
      * CLIENT SENDS : HEADER PLAYEROBJECTID GLOBAL/LOCAL(0/1)(across many games or this game)
      * SERVER SENDS : HEADER USERNAME KILLVAL DEATHVAL DAMAGEVAL HITVAL MISSVAL GLOBAL/LOCAL(0/1)
      * When the client needs stat information for each player, they will pull via this packet.
      * Local - refers to the current game's stats
      * global - refers to the stats stored in the database
      * 
      * when the server spoofs this packet, it sends the information to all relevant clients to update them of a specific change
      */
    class GetStatsPacket : Packet
    {
        string sUsername;
        int[] iVal;
        public GetStatsPacket()
        {
            sHeader = "GetStats";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if(cliSender == null)
            {
                return Server.GetAllLoggedInClients();
            }
            return new Client[1] { cliSender };
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            if (Server.GetCurrentGame() != null)
            {
                if (iRepetitionIndex != 0 || (asParams[1] == "0" && Server.GetCurrentGame().InGame(cliReceiver)) || (asParams[1] == "1" && !Server.GetCurrentGame().InGame(cliReceiver)))
                {
                    return false;
                }
            }
            Client cliStatGiver = Server.GetClientByNID(int.Parse(asParams[0]));
            sUsername = (string)cliStatGiver;
            iVal = (asParams[1] == "0") ? Server.GetStatInformation(sUsername) : cliStatGiver.GetAllLocalStats();
            return iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            string[] asMessage = new string[iVal.Length + 3];
            for(int i = 0; i < asMessage.Length - 1; i++)
            {
                switch(i)
                {
                    case 0:
                        asMessage[0] = sUsername;
                        break;
                    case 1:
                        asMessage[1] = asParams[1];
                        break;
                    default:
                        asMessage[i] = iVal[i - 2].ToString();
                        break;
                }
            }
            asMessage[asMessage.Length - 1] = asParams[2];
            return SimplePacket(sHeader, asMessage);
        }
    }
}
