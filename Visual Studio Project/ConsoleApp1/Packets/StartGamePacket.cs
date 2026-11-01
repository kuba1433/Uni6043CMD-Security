using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : (NEVER SENDS)
    /// SERVER SENDS : HEADER MAP GAMEMODE TIME SPAWNPOINT ROUNDTIME
    /// Once the client receives a 'StartGamePacket' it will be thrusted into a game, the server internally determines whether a player will be in the next game
    /// 
    /// </summary>
    class StartGamePacket : Packet
    {
        static int iMapPicked = 0;
        static int iGamemodePicked = 0;
        static int iSpawnPointUsed = -1;
        public const int iLoadoutSelectionTime = 15;
        public StartGamePacket()
        {
            sHeader = "StartGame";
        }

        protected override void NewMessage(string sMessage)
        {
            iMapPicked = MyRandom.RandomInt(Server.iMapCount);
            iGamemodePicked = MyRandom.RandomInt(Server.iGamemodeCount);
            Server.StartNewGame(iMapPicked, iGamemodePicked);
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if (cliSender == null)
            {
                return (Client[])Server.GetCurrentGame();
            }
            return new Client[0];
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            iSpawnPointUsed = Server.GetCurrentGame().GetPlayerSpawn(cliReceiver);
            return iRepetitionIndex == 0 && iSpawnPointUsed != -1;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, iMapPicked.ToString(), iGamemodePicked.ToString(), iLoadoutSelectionTime.ToString(), iSpawnPointUsed.ToString(), (Server.iRoundTimer / 1000).ToString());
        }
    }
}
