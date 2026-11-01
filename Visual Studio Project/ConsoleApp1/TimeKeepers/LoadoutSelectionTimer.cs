using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApp1.Server;
using ConsoleApp1.Packets;

namespace ConsoleApp1
{
    /// <summary>
    /// How long players have to select their loadouts before each round
    /// </summary>
    internal class LoadoutSelectionTimer : TimeKeeper
    {
        public LoadoutSelectionTimer()
        {
            iTimeToCheck = StartGamePacket.iLoadoutSelectionTime * 1000;
            if (TESTMODE) iTimeToCheck = 1000;
        }

        protected override bool ActivationCondition()
        {
            return GetCurrentGameState() == GameState.Loadout;
        }

        protected override void TimeComplete()
        {
            SpoofPacket(PacketTypes.SelectWeapon, null);
            NewGameState(GameState.Gameplay);
        }

        protected override bool ResetCondition()
        {
            return GetCurrentGameState() == GameState.Gameplay;
        }
    }
}
