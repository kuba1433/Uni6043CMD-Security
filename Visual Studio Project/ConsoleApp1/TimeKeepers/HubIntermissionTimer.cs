using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApp1.Server;

namespace ConsoleApp1
{
    /// <summary>
    /// This sends a message to every logged in client a prompt about not enough players to start a game at even intervals in the hub when there are less than 2 logged in clients
    /// </summary>
    internal class HubIntermissionTimer : TimeKeeper
    {
        public HubIntermissionTimer()
        {
            iTimeToCheck = 20000;
        }

        protected override bool ActivationCondition()
        {
            return GetCurrentGameState() == GameState.Hub && GetAllLoggedInClients().Length < 2;
        }

        protected override bool InterruptTimer()
        {
            return GetAllLoggedInClients().Length >= 2;
        }

        protected override void TimeComplete()
        {
            SendServerChatMessage("Needed at least 2 players to start a game");
        }

        protected override bool ResetCondition()
        {
            return GetCurrentGameState() != GameState.Hub;
        }
    }
}
