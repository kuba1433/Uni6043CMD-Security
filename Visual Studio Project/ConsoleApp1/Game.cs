using ConsoleApp1.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Game
    {
        const int iMaxPlayers = 10;
        Client[] acliPlayers = null;
        int[] aiPlayerScores;
        int[] aiSpawns;
        PickupType[] aptEPickups;
        int iMap;
        int iRoundCount = 0;
        static int[] aiWinCon = new int[2] { 5, 5 };
        static int[] aiPickupsPerMap = new int[1] { 2 };
        Gamemode gmE;
        public enum Gamemode { FFA, Teams };
        public enum PickupType {None = -1, Health, Ammo}

        public Game(Client[] acliP, int iMapNo, int iModeNo)
        {
            gmE = (Gamemode)iModeNo;
            iMap = iMapNo;
            if (acliP.Length > iMaxPlayers)
            {  // if the input players exceed the maximum, it picks a random portion of the group
                acliPlayers = MyRandom.PickRandomFromArray(acliP, iMaxPlayers);
            }
            else
            {
                acliPlayers = acliP;
            }
            aiPlayerScores = new int[acliPlayers.Length];
            for (int i = 0; i < aiPlayerScores.Length; i++)
            {
                aiPlayerScores[i] = 0;
            }
            aptEPickups = new PickupType[aiPickupsPerMap[iMapNo]];
            ResetPickups();
            for(int i = 0; i < aptEPickups.Length; i++)
            {
                Server.CreatePickupRefreshTimer(i);
            }
        }

        public int IncrementScore(Client cliToIncrement)
        {
            for(int i = 0; i < acliPlayers.Length; i++)
            {
                if (acliPlayers[i].Equals(cliToIncrement))
                {
                    aiPlayerScores[i]++;
                    return aiPlayerScores[i];
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns true if their aren't enough players in the game to warrent the round continueing (only 1 team alive in team game or only 1 player alive)
        /// </summary>
        /// <returns>If there is not enough players to constitute continuation of the game</returns>
        public bool NotEnoughPlayers()
        {
            bool bTeam1Alive = false;
            bool bTeam2Alive = false;
            switch(gmE)
            {
                case Gamemode.FFA:
                    for(int i = 0; i < acliPlayers.Length; i++)
                    {
                        if (acliPlayers[i].IsAlive())
                        {
                            if(bTeam1Alive)
                            {
                                return false;
                            }
                            else
                            {
                                bTeam1Alive = true;
                            }
                        }
                    }
                    break;
                case Gamemode.Teams:
                    for(int i = 0; i < acliPlayers.Length; i++)
                    {
                        if (acliPlayers[i].IsAlive())
                        {
                            if (!bTeam1Alive && aiSpawns[i] < iMaxPlayers / 2)
                            {
                                bTeam1Alive = true;
                            }
                            else if (!bTeam2Alive && aiSpawns[i] >= iMaxPlayers / 2)
                            {
                                bTeam2Alive = true;
                            }
                            if(bTeam1Alive && bTeam2Alive)
                            {
                                return false;
                            }
                        }
                    }
                    break;
            }
            return true;

            //bool bTeam1Alive = false;
            //bool bTeam2Alive = false;
            //int iPlayerCount = 0;
            //for(int i = 0; i < aiSpawns.Length; i++)
            //{
            //    if (!bTeam1Alive && aiSpawns[i] < iMaxPlayers / 2 && acliPlayers[i].IsAlive())
            //    {
            //        bTeam1Alive = true;
            //    }
            //    else if (!bTeam2Alive && aiSpawns[i] >= iMaxPlayers / 2 && acliPlayers[i].IsAlive())
            //    {
            //        bTeam2Alive = true;
            //    }
            //    if (acliPlayers[i].IsAlive())
            //    {
            //        iPlayerCount++;
            //    }
            //}
            //return ((gmE == Gamemode.TeamsbTeam1Alive && !bTeam2Alive) || (!bTeam1Alive && bTeam2Alive) || (gmE == Gamemode.FFA && iPlayerCount > 2);
        }

        public void ResetPickups()
        {
            for(int i = 0; i < aptEPickups.Length; i++)
            {
                aptEPickups[i] = PickupType.None;
            }
        }

        public PickupType GetPickupType(int iIndex)
        {
            return aptEPickups[iIndex];
        }

        /// <summary>
        /// Sets the pickup (presumed none) to a random one and sends a packet to all clients
        /// </summary>
        /// <param name="iIndex"></param>
        /// <returns></returns>
        public PickupType RefreshPickup(int iIndex)
        {
            PickupType ptERand = (PickupType)MyRandom.RandomInt(System.Enum.GetValues(typeof(PickupType)).GetUpperBound(0));
            aptEPickups[iIndex] = ptERand;
            Server.SpoofPacket(Server.PacketTypes.UsePickup, null, iIndex.ToString());
            return ptERand;
        }

        /// <summary>
        /// Sets the pickup to none and returns what it was previously
        /// </summary>
        /// <param name="iIndex"></param>
        /// <returns></returns>
        public PickupType UsePickup(int iIndex)
        {
            PickupType ptEOut = aptEPickups[iIndex];
            aptEPickups[iIndex] = PickupType.None;
            return ptEOut;
        }

        /// <summary>
        /// Returns true if the input client is n this game
        /// </summary>
        /// <param name="cliThis"></param>
        /// <returns></returns>
        public bool InGame(Client cliThis)
        {
            for (int i = 0; i < acliPlayers.Length; i++)
            {
                if (cliThis.Equals(acliPlayers[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the spawnpoint given a specified client
        /// </summary>
        /// <param name="cliThis"></param>
        /// <returns></returns>
        public int GetPlayerSpawn(Client cliThis)
        {
            for (int i = 0; i < acliPlayers.Length; i++)
            {
                if (acliPlayers[i].Equals(cliThis))
                {
                    return aiSpawns[i];
                }
            }
            return -1;
        }

        /// <summary>
        /// Resets 'aiSpawns' and generates spawn locations for each player, for team games it attempts to make the team split as even as possible
        /// </summary>
        /// <returns></returns>
        public int[] GenerateSpawnPoints()
        {
            switch (gmE)
            {
                case Gamemode.FFA:
                    aiSpawns = MyRandom.PickRandomFromArray(ArraysExpanded.PopulateNewIntArray(0, iMaxPlayers), 10);
                    break;
                case Gamemode.Teams:
                    if(aiSpawns == null)
                    {
                        int[] aiTeams = new int[acliPlayers.Length];
                        int[] aiTeamMemberCount = new int[2] { 0, 0 };
                        for(int i = 0;i < aiTeams.Length; i++)
                        {
                            int iForceAssign = -1;
                            for(int j = 0; j < aiTeamMemberCount.Length; j++)
                            {
                                if (aiTeamMemberCount[j] >= acliPlayers.Length / 2)
                                {
                                    if(j == 0)
                                    {
                                        iForceAssign = 1;
                                    }
                                    else
                                    {
                                        iForceAssign = 0;
                                    }
                                }
                            }
                            if(iForceAssign == -1)
                            {
                                iForceAssign = MyRandom.RandomInt(2);
                            }
                            aiTeams[i] = iForceAssign;
                            aiTeamMemberCount[iForceAssign]++;
                        }
                        aiSpawns = new int[aiTeams.Length];
                        for (int i = 0; i < aiTeams.Length; i++)
                        {
                            aiSpawns[i] = aiTeams[i] * (iMaxPlayers / 2);
                        }
                        //aiSpawns = new int[acliPlayers.Length];
                        //int iHalf = (int)(iMaxPlayers * 0.5f);
                        //int[] aiTeam1 = ArraysExpanded.PopulateNewIntArray(0, iHalf);
                        //int[] aiTeam2 = ArraysExpanded.PopulateNewIntArray(iHalf, iHalf * 2);
                        //int iTeam1Assigns = 0;
                        //int iTeam2Assigns = 0;
                        //int iHalfPlayers = (int)(acliPlayers.Length * 0.5f);
                        //for (int i = 0; i < iHalfPlayers * 2; i++)
                        //{
                        //    if ((iTeam1Assigns - iHalfPlayers != iHalfPlayers && MyRandom.RandomInt(2) == 0) || iTeam2Assigns - iHalfPlayers == iHalfPlayers)
                        //    {
                        //        (int, int[]) iaiOutRandom = MyRandom.PickRandomWithReducedArray(aiTeam1);
                        //        iTeam1Assigns++;
                        //        aiSpawns[i] = iaiOutRandom.Item1;
                        //        aiTeam1 = iaiOutRandom.Item2;
                        //    }
                        //    else
                        //    {
                        //        (int, int[]) iaiOutRandom = MyRandom.PickRandomWithReducedArray(aiTeam2);
                        //        iTeam2Assigns++;
                        //        aiSpawns[i] = iaiOutRandom.Item1;
                        //        aiTeam2 = iaiOutRandom.Item2;
                        //    }
                        //}
                        //if ((int)((acliPlayers.Length - 1) * 0.5f) == (int)(acliPlayers.Length * 0.5f))
                        //{
                        //    aiSpawns[aiSpawns.Length - 1] = MyRandom.PickRandom((MyRandom.RandomInt(2) == 0) ? aiTeam1 : aiTeam2);
                        //}
                    }
                    for(int i = 0; i < aiSpawns.Length; i++)
                    {
                        aiSpawns[i] = aiSpawns[i] / (iMaxPlayers / 2) * (iMaxPlayers / 2) + MyRandom.RandomInt(iMaxPlayers / 2);
                    }
                    break;
            }
            return aiSpawns;
        }

        /// <summary>
        /// Increments the round counter, checks if anyone has won and prepares a new round if no-one has won
        /// </summary>
        /// <returns>Whether there is a winner and the next round won't happen</returns>
        public bool NewRound()
        {
            iRoundCount++;
            switch(gmE)
            {
                case Gamemode.FFA:
                    for(int i = 0; i < aiPlayerScores.Length; i++)
                    {
                        if (aiPlayerScores[i] >= aiWinCon[(int)Gamemode.FFA])
                        {
                            return true;
                        }
                    }
                    break;
                case Gamemode.Teams:
                    for(int i = 0; i < 2; i++)
                    {
                        if(GetTeamScore(i) >= aiWinCon[(int)Gamemode.Teams])
                        {
                            return true;
                        }
                    }
                    break;
            }
            for(int i = 0; i < acliPlayers.Length; i++)
            {
                acliPlayers[i].ResetHealth();
                acliPlayers[i].ResetWeaponSelected();
            }
            GenerateSpawnPoints();
            return false;
        }

        /// <summary>
        /// Will attempt to distribute the scores of all the teams, if the gamemode is a team one
        /// </summary>
        /// <returns>Whether the scores have been distributed</returns>
        public bool TryShowTeamScore()
        {
            if(gmE == Gamemode.Teams)
            {
                Server.SpoofPacket(Server.PacketTypes.TeamScoreUpdate, null);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a team score for a specific team, given that the respective 'aiSpawns' tells this what team each client is on
        /// </summary>
        /// <param name="iTeamNum"></param>
        /// <returns></returns>
        public int GetTeamScore(int iTeamNum)
        {
            int iSum = 0;
            for(int i = 0; i < aiPlayerScores.Length; i++)
            {
                if (aiSpawns[i] < iMaxPlayers / 2 && iTeamNum == 0)
                {
                    iSum += aiPlayerScores[i];
                }
                else if(aiSpawns[i] >= iMaxPlayers / 2 && iTeamNum == 1)
                {
                    iSum += aiPlayerScores[i];
                }
            }
            return iSum;
        }

        /// <summary>
        /// Returns an ID of the client in 'acliPlayers'
        /// </summary>
        /// <param name="cliToCheck"></param>
        /// <returns></returns>
        public int GetClientID(Client cliToCheck)
        {
            if (acliPlayers == null)
            {
                return -1;
            }
            for (int i = 0; i < acliPlayers.Length; i++)
            {
                if (acliPlayers[i].Equals(cliToCheck))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Assumes the game has ended and outputs the winner(s) username or team name
        /// </summary>
        /// <returns></returns>
        public string GetWinnerName()
        {
            switch (gmE)
            {
                case Gamemode.FFA:
                    string sOut = "";
                    int iMax = 0;
                    for(int i = 0; i < aiPlayerScores.Length; i++)
                    {
                        if(iMax < aiPlayerScores[i])
                        {
                            iMax = aiPlayerScores[i];
                            sOut = "";
                        }
                        if(iMax == aiPlayerScores[i])
                        {
                            sOut += $"{(string)acliPlayers[i]} ";
                        }
                    }
                    return sOut;
                case Gamemode.Teams:
                    int[] aiTeamScores = new int[2] { GetTeamScore(0), GetTeamScore(1) };
                    if (aiTeamScores[0] > aiTeamScores[1])
                    {
                        return "Team 1";
                    }
                    else if (aiTeamScores[0] < aiTeamScores[1])
                    {
                        return "Team 2";
                    }
                    else
                    {
                        return "Both Teams";
                    }
            }
            return "";
        }

        /// <summary>
        /// What the game needs to do before the game is deleted
        /// </summary>
        public void Shutdown()
        {
            for(int i = 0; i < acliPlayers.Length; i++)
            {
                acliPlayers[i].PushStatsToDatabase();
                acliPlayers[i].ResetInformation();
            }
        }

        //public bool CheckRoundEnd()
        //{
        //    switch(gmE)
        //    {
        //        case Gamemode.FFA:
        //            int iAlivePlayers = 0;
        //            for(int i = 0; i < acliPlayers.Length; i++)
        //            {

        //            }
        //    }
        //}

        public static explicit operator Client[](Game gameA) => gameA.acliPlayers;
    }
}
