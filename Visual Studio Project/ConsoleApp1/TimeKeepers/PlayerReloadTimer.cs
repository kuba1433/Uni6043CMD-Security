using ConsoleApp1.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// The timer used to reload player weapons, player sends a reload packet starting this timer and if the timer reaches its destination it will send a reload packet back confirming the reload, stopping when the player does unexpected behaviour
    /// </summary>
    internal class PlayerReloadTimer : TimeKeeper
    {
        Client cliToReload;
        int iWeaponToReload;
        public PlayerReloadTimer(Client cliReloader)
        {
            iTimeToCheck = 2000;
            cliToReload = cliReloader;
            iWeaponToReload = cliToReload.GetCurrentWeapon();
        }

        protected override bool InterruptTimer()
        {
            if (cliToReload.GetCurrentWeapon() != iWeaponToReload)
            {
                bRemoveTimer = true;
            }
            return bRemoveTimer;
        }

        protected override void TimeComplete()
        {
            cliToReload.ReloadWeapon();
            ReloadPacket.cliToReload = cliToReload;
            Server.SpoofPacket(Server.PacketTypes.Reload, null);
            bRemoveTimer = true;
        }
    }
}
