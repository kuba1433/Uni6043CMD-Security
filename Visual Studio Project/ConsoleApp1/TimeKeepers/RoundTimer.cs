using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApp1.Server;

namespace ConsoleApp1
{
    /// <summary>
    /// How long playes have in a round maximally
    /// </summary>
    internal class RoundTimer : TimeKeeper
    {
        public RoundTimer()
        {
            iTimeToCheck = iRoundTimer;
        }

        protected override bool ActivationCondition()
        {
            return GetCurrentGameState() == GameState.Gameplay;
        }

        protected override bool InterruptTimer()
        {
            if(!ActivationCondition())
            {
                bRemoveTimer = true;
            }
            return bRemoveTimer;
        }

        protected override void TimeComplete()
        {
            NewRound();
        }
    }
}
