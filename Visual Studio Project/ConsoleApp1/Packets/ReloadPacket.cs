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
    /// SERVER SENDS AFTER RELOAD : HEADER WEAPONAMMO
    /// Client informs the server it wants to reload and the server starts a timer in the background.
    /// When the timer completes and the player doesn't do anything that makes the reload impossible (swaps weapon in meantime), the server tells the client its new ammo
    /// 
    /// I have identified a bug where the player can fire their weapon while reloading without the timer failing, a fix isn't implemented yet
    /// </summary>
    class ReloadPacket : Packet
    {
        public static Client cliToReload;
        public ReloadPacket()
        {
            sHeader = "Reload";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if (cliSender == null)
            {
                return new Client[1] { cliToReload };
            }
            Server.CreateReloadTimer(cliSender);
            return new Client[0];
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, cliToReload.GetWeaponCurAmmo().ToString());
        }
    }
}
