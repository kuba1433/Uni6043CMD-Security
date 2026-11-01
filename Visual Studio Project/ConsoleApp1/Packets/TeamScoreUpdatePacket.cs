using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /*
     * SERVER SEND TO SELF : HEADER
     * SERVER SEND TO CLIENT : HEADER TEAM1SCORE TEAM2SCORE
     */
    internal class TeamScoreUpdatePacket : Packet
    {
        public TeamScoreUpdatePacket()
        {
            sHeader = "TeamScoreUpdate";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            return (cliSender == null) ? (Client[])Server.GetCurrentGame() : new Client[0];
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, Server.GetCurrentGame().GetTeamScore(0).ToString(), Server.GetCurrentGame().GetTeamScore(1).ToString());
        }
    }
}
