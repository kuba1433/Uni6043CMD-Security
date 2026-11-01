using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* 
     * CLIENT SENDS : HEADER POSX POSY POSZ DIRECTIONX DIRECTIONY DIRECTIONZ STOPDIST USERHIT(can have no value indicating no player was hit)
     * SERVER SENDS THIS CLIENT : HEADER POSX POSY POSZ DIRECTIONX DIRECTIONY DIRECTIONZ STOPDIST NEWCURAMMO 
     * SERVER SENDS OTHER CLIENTS : HEADER POSX POSY POSZ DIRECTIONX DIRECTIONY DIRECTIONZ STOPDIST
     * client sends a fire weapon packet, so the server can decrement the ammo and lower the health of others affected (if the player has ammo)
     * server sends the raycast used to allow other clients to see the bullet trail
     * 
     * This packet is one of 2 ways a player can lower the health of another
     */
    class FireWeaponPacket : Packet
    {
        bool bAmmoAvailable = false;
        Client cliPrevPacketSender;
        bool bSendingToSender = false;
        public FireWeaponPacket()
        {
            sHeader = "FireWeapon";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if(iRepetitionIndex == 0)
            {
                cliPrevPacketSender = cliSender;
                string[] asParams = DeterminePacketParameters(sMessage);
                bAmmoAvailable = cliPrevPacketSender.DecrementAmmo();
                Game gameCurrent = Server.GetCurrentGame();
                if (bAmmoAvailable && gameCurrent != null)
                {
                    Server.ChangeStat(cliSender, Server.StatTypes.ShotsMissed, 1);
                    Client[] acliGame = (Client[])gameCurrent;
                    int iDamageDone = 0;
                    for (int i = 7; i < asParams.Length; i++)
                    {
                        int idToCheck;
                        
                        if (int.TryParse(asParams[i], out idToCheck))
                        {
                            for (int j = 0; j < acliGame.Length; j++)
                            {
                                if (acliGame[j].GetPlayerGameObjectID() == idToCheck && acliGame[j].IsAlive())
                                {
                                    iDamageDone = acliGame[j].ShotByWeapon(cliSender.GetCurrentWeapon());
                                    Server.ChangeStat(cliSender, Server.StatTypes.DamageDone, iDamageDone);
                                    if (acliGame[j].GetHealth() <= 0)
                                    {
                                        Server.SpoofPacket(Server.PacketTypes.Death, null, (string)cliSender, j.ToString());
                                        Server.ChangeStat(cliSender, Server.StatTypes.Kills, 1);
                                    }
                                    Server.SpoofPacket(Server.PacketTypes.HealthUpdate, acliGame[j]);
                                }
                            }
                        }
                    }
                    if(iDamageDone > 0)
                    {
                        Server.ChangeStat(cliSender, Server.StatTypes.ShotsHit, 1);
                    }
                    Server.SpoofPacket(Server.PacketTypes.GetStats, null, cliSender.GetPlayerGameObjectID().ToString(), "1");
                    return acliGame;
                }
            }
            return new Client[0];
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            bSendingToSender = cliPrevPacketSender.Equals(cliReceiver);
            return iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            string[] asOut;
            if(bSendingToSender)
            {
                asOut = new string[8];
            }
            else
            {
                asOut = new string[7];
            }
            for (int i = 0; i < asOut.Length; i++)
            {
                if(i == 7)
                {
                    asOut[i] = cliPrevPacketSender.GetWeaponCurAmmo().ToString();
                }
                else
                {
                    asOut[i] = asParams[i];
                }       
            }
            return SimplePacket(sHeader, asOut);
        }
    }
}
