using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.TimeKeepers
{
    /// <summary>
    /// Timer used to spawn pickups, once a player picks up a pickup this timer starts and once it ends a new pickup will take the place
    /// </summary>
    internal class PickupRefreshTimer : TimeKeeper
    {
        int iPickupToRefresh;
        public PickupRefreshTimer(int iPickupId)
        {
            iTimeToCheck = 10000;
            iPickupToRefresh = iPickupId;
        }

        protected override bool ActivationCondition()
        {
            return Server.GetCurrentGameState() == Server.GameState.Gameplay;
        }

        protected override bool InterruptTimer()
        {
            if(Server.GetCurrentGameState() != Server.GameState.Gameplay)
            {
                bRemoveTimer = true;
            }
            return bRemoveTimer;
        }

        protected override void TimeComplete()
        {
            Server.GetCurrentGame().RefreshPickup(iPickupToRefresh);
            bRemoveTimer = true;
        }
    }
}
