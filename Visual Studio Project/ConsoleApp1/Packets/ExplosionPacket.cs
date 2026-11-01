using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* Explosion
     * CLIENT SENDS : HEADER POSX POSY POSZ EXPLOSIONINDEX NETWORKID
     * SERVER SENDS : (HealthUpdatePacket or DeathPacket)
     * This packet is one of 2 ways a player can lower the health of another
     */
    /// <summary>
    /// This packet is used by the client to inform the server of an explosion at a certain location, which the server would calculate its causal impact based on most recent information (Client's character's positions)
    /// </summary>
    class ExplosionPacket : Packet
    {
        int iDamageDone = 0;
        public ExplosionPacket()
        {
            sHeader = "Explosion";
        }

        protected override void NewMessage(string sMessage)
        {
            iDamageDone = 0;
            string[] asParams = DeterminePacketParameters(sMessage);
            int iExplosiveId = int.Parse(asParams[4]);
            GameObject goExplosive = Server.GetGameObjectByID(iExplosiveId);
            if(goExplosive != null)
            {
                Client cliExplosiveOwner = (Client)goExplosive;
                int iExplosionType = int.Parse(asParams[3]);
                Vector3 v3ExplosionPos = new Vector3(float.Parse(asParams[0]), float.Parse(asParams[1]), float.Parse(asParams[2]));
                Client[] acliPlayers = (Client[])Server.GetCurrentGame();
                for (int i = 0; i < acliPlayers.Length; i++)
                {
                    int iHealthLost = 0;
                    if (acliPlayers[i].IsAlive())
                    {
                        switch (iExplosionType)
                        {
                            case 0:
                                iHealthLost = acliPlayers[i].ExplosionSimulation(v3ExplosionPos, 7.5f, 20, 10);
                                break;
                            case 1:
                                iHealthLost = acliPlayers[i].ExplosionSimulation(v3ExplosionPos, 10f, 25, 10);
                                break;
                        }
                        if (iHealthLost > 0)
                        {
                            if (!(cliExplosiveOwner).Equals(acliPlayers[i]))
                            {
                                iDamageDone += iHealthLost;
                            }
                            if (acliPlayers[i].GetHealth() > 0)
                            {
                                Server.SpoofPacket(Server.PacketTypes.HealthUpdate, acliPlayers[i]);
                            }
                            else
                            {
                                Server.SpoofPacket(Server.PacketTypes.Death, null, (string)acliPlayers[i], i.ToString());
                            }
                        }
                    }
                }
                (cliExplosiveOwner).AddStatInformation(1, Server.StatTypes.ShotsMissed);
                if (iDamageDone > 0)
                {
                    (cliExplosiveOwner).AddStatInformation(iDamageDone, Server.StatTypes.DamageDone);
                    (cliExplosiveOwner).AddStatInformation(1, Server.StatTypes.ShotsHit);
                }
                Server.SpoofPacket(Server.PacketTypes.GetStats, null, cliExplosiveOwner.GetPlayerGameObjectID().ToString(), "1");
                Server.RemoveObjectByNetworkID(int.Parse(asParams[4]));
            }
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            return false;
        }
    }
}
