using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : HEADER SLOT(PRIMARY(1)/SECONDARY(2)) WEAPONNUM
    /// SERVER SENDS : HEADER SLOT WEAPONNUM MAXAMMO
    /// Client informs the server of the user's loadout decisions before a game and once loadout selection is over, the server will send this packet causing the actual objects in game to be present.
    /// The player has no concept of ammo until the server sends it the value
    /// </summary>
    class SelectWeaponPacket : Packet
    {
        int iStoredWeapon;
        public SelectWeaponPacket()
        {
            sHeader = "SelectWeapon";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if (cliSender == null)
            {
                return (Client[])Server.GetCurrentGame();
            }
            string[] asParams = DeterminePacketParameters(sMessage);
            cliSender.SelectWeapon(int.Parse(asParams[0]), int.Parse(asParams[1]));
            return new Client[0];
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            if (iRepetitionIndex < 2)
            {
                iStoredWeapon = cliReceiver.ForceGetValidWeapon(iRepetitionIndex);
                return true;
            }
            return false;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, iRepetitionIndex.ToString(), iStoredWeapon.ToString(), Client.GetWeaponMaxAmmo(iRepetitionIndex, iStoredWeapon).ToString());
        }
    }
}
