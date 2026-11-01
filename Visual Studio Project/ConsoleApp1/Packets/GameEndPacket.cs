using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* GameEnd
    * CLIENT SENDS :
    * SERVER SENDS : HEADER WINNERUSERNAME
    * Sends a 'GameEndPacket' to all users to tell them the winner and the clients to teleport them back to the lobby
    */
    class GameEndPacket : Packet
    {
        public GameEndPacket()
        {
            sHeader = "GameEnd";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if(cliSender == null && iRepetitionIndex == 0)
            {
                return (Client[])Server.GetCurrentGame();
            }
            if(cliSender == null)
            {
                Server.EndGame();
            }
            return new Client[0];
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, Server.GetCurrentGame().GetWinnerName());
        }
    }
}
