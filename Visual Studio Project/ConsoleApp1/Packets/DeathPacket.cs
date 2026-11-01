using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* Death
     * CLIENT SENDS :
     * SERVER SENDS TO SELF : HEADER KILLERUSERNAME GAMECLIENTID
     * SERVER SENDS : HEADER KILLERUSERNAME
     * A server only packet that informs a client that they should cede control of the player character, since they are dead
     */
    class DeathPacket : Packet
    {
        public DeathPacket()
        {
            sHeader = "Death";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if(cliSender == null)
            {
                return new Client[1] { ((Client[])Server.GetCurrentGame())[int.Parse(DeterminePacketParameters(sMessage)[1])] };
            }
            return new Client[0];
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            if (iRepetitionIndex == 0)
            {
                Server.ChangeStat(cliReceiver, Server.StatTypes.Deaths, 1);  // increments player's death stat and sends this to all other clients to update their leaderboards
                Server.SpoofPacket(Server.PacketTypes.GetStats, null, cliReceiver.GetPlayerGameObjectID().ToString(), "1");
            }
            return iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, DeterminePacketParameters(sMessage)[0]);
        }
    }
}
