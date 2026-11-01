using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : HEADER
    /// SERVER SENDS TO SENDER : HEADER PLAYEROBJID WEAPONSLOT WEAPONAMMO WEAPONMAXAMMO
    /// SERVER SENDS TO OTHERS : HEADER PLAYEROBJID WEAPONNUM
    /// </summary>
    class SwapWeaponPacket : Packet
    {
        int iNewSlot = 0;
        Client cliPrevPacketSender;
        bool bSendingToSender = false;
        public SwapWeaponPacket()
        {
            sHeader = "SwapWeapon";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if(iRepetitionIndex == 0)
            {
                iNewSlot = cliSender.SwapWeapon();
            }
            cliPrevPacketSender = cliSender;
            return (Client[])Server.GetCurrentGame();
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            bSendingToSender = cliPrevPacketSender.Equals(cliReceiver);
            return iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            if (bSendingToSender)
            {
                return SimplePacket(sHeader, cliPrevPacketSender.GetPlayerGameObjectID().ToString(), iNewSlot.ToString(), cliPrevPacketSender.GetWeaponCurAmmo().ToString(), cliPrevPacketSender.GetWeaponMaxAmmo().ToString());
            }
            return SimplePacket(sHeader, cliPrevPacketSender.GetPlayerGameObjectID().ToString(), cliPrevPacketSender.GetCurrentWeapon().ToString());
        }
    }
}
