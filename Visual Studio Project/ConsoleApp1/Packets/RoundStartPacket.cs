using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* RoundStart
    * CLIENT SENDS : 
    * SERVER SENDS : HEADER LOADOUTTIME SPAWNPOINT
    * This packet is sent by the server to tell clients a new round has started (containing the amount of time they have to select a loadout and the spawnpoint they'll spawn from once they do)
    */
    class RoundStartPacket : Packet
    {
        static int iSpawnPointUsed = 0;
        public RoundStartPacket()
        {
            sHeader = "RoundStart";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            return (Client[])Server.GetCurrentGame();
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            if(iRepetitionIndex == 0)
            {
                iSpawnPointUsed = Server.GetCurrentGame().GetPlayerSpawn(cliReceiver);
                cliReceiver.ResetHealth();
                cliReceiver.ResetWeaponSelected();
            }
            
            return iRepetitionIndex == 0 && iSpawnPointUsed != -1;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, StartGamePacket.iLoadoutSelectionTime.ToString(), iSpawnPointUsed.ToString());
        }
    }
}
