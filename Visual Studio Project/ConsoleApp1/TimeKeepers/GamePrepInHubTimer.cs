using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApp1.Server;
namespace ConsoleApp1
{
    /// <summary>
    /// How long the time is before a valid number of players exist in the hub (while a game isn't playing out) and the start of the game (1st loadout selection)
    /// </summary>
    internal class GamePrepInHubTimer : TimeKeeper
    {
        public GamePrepInHubTimer()
        {
            if(PLAYGAME)
            {
                iTimeToCheck = Server.iTimeInHub;
                if (Server.TESTMODE) iTimeToCheck = 100;
            }
            else
            {
                iTimeToCheck = 999999999;
            }
        }

        protected override bool ActivationCondition()
        {
            return GetAllLoggedInClients().Length >= 2 && GetCurrentGameState() == GameState.Hub && iRepeatsAfterReset < 1;
        }

        protected override bool InterruptTimer()
        {
            return GetAllLoggedInClients().Length < 2;
        }

        protected override void TimeComplete()
        {
            SpoofPacket(PacketTypes.StartGame, null);
            NewGameState(GameState.Loadout);
        }

        protected override bool ResetCondition()
        {
            return GetCurrentGameState() != GameState.Hub;
        }
    }
}
