using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp1.Packets;

namespace ConsoleApp1
{
    /*
     * Holds information about the game state from the client's perspective (health, weapons, etc)
     * 
     * ideally there would be a weapon class that would do all weapon logic seperately and smoothly
     */
    internal class Client
    {
        static int[] aiWeaponMaxAmmo = new int[8] { 25, 64, 4, 30, 15, 6, 1, 3 };  // The max ammo each weapon in the game has
        static int[] aiWeaponDamage = new int[8] { 5, 4, 40, 2, 10, 20, 0, 0 };  // The damage each weapon has when its raycase hits a target (0 for explosive weapons)
        Vector3 v3LastPos;  // The latest position sent in the most recent 'TransformDataPacket' containing the NID of its player object in the scene (PrefabID == 0)
        public const int iLargestWeaponVal = 4;  // The largest number that can be present in 'aiWeaponsSelected'
        EndPoint endpLoc;  // 'EndPoint' this client is expected to communciate through
        Stopwatch swatch = new Stopwatch();  // a stopwatch with the time of the player's most recently received packet
        string sUsername = "";  // Username this client entered in the login page
        bool bLoggedIn = false;  // Whether the client has made it past the login page
        int[] aiWeaponsSelected = new int[2] { -1, -1 };  // What weapons this client has selected (these are numbers from -1 to 'iLargestWeaponVal' (-1 means no weapon))
        int[] aiWeaponAmmo = new int[2] { -1, -1 };  // How much ammo the weapons the client has selected have each (used for when a client swaps back to a weapon they've partially/fully emptied
        int[] aiGameStats = new int[System.Enum.GetValues(typeof(Server.StatTypes)).Length];  // the local game stats stored for this client (at the end of a game the client pushes this information to the database)
        int iHealth = 100;  // health of the player
        int iHoldingWeapon = 0;  // which weapon is being held in the player's hand
        int iPlayerObjectId;  // the NID for the player object in the client's scene

        public Client(EndPoint endpD)
        {
            endpLoc = endpD;
            swatch.Restart();
        }

        public bool IsAlive()
        {
            return iHealth > 0;
        }

        public void UpdateLastPosition(Vector3 v3New)
        {
            v3LastPos = v3New;
        }

        /// <summary>
        /// Useful at the end of a game only (since it resets local stats)
        /// </summary>
        public void ResetInformation()
        {
            ResetHealth();
            iHoldingWeapon = 0;
            ResetWeaponSelected();
            for (int i = 0; i < aiGameStats.Length; i++)
            {
                aiGameStats[i] = 0;
            }
        }

        public void ResetHealth()
        {
            iHealth = 100;
        }

        public void ResetWeaponSelected()
        {
            for (int i = 0; i < aiWeaponsSelected.Length; i++)
            {
                aiWeaponsSelected[i] = -1;
            }
        }

        /// <summary>
        /// Decrements the held weapon's ammo and returns whether the player has ammo to be able to shoot
        /// </summary>
        public bool DecrementAmmo()
        {
            if (aiWeaponAmmo[iHoldingWeapon] > 0)
            {
                aiWeaponAmmo[iHoldingWeapon]--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets its internal timer when they have been confirmed to have sent a packet
        /// </summary>
        /// <remarks>Given this timer is also reset when the Server spoofs a packet under this client's name, the 'SpoofPacket' system should attribute to a null client as much as possible</remarks>
        public float NewPacket()
        {
            float fOut = swatch.ElapsedMilliseconds * 1000f;
            swatch.Restart();
            return fOut;
        }

        /// <summary>
        /// Informs the server this client has made it past the login page
        /// </summary>
        public void SetUsername(string sNew)
        {
            sUsername = sNew;
            bLoggedIn = true;
        }

        /// <summary>
        /// Sets weapon slot 'iSlot' to weapon number 'iWeapon'
        /// </summary>
        /// <param name="iSlot"></param>
        /// <param name="iWeapon">This represents an index for the subset of weapons this allows in this slot, i.e. weapon index 4 is 0 if 'iSlot' is 1</param>
        /// <returns>Weapon that was already taking up this slot, useful if dropping weapons could be a thing</returns>
        public int SelectWeapon(int iSlot, int iWeapon)
        {
            int iOut = aiWeaponsSelected[iSlot];
            aiWeaponsSelected[iSlot] = iWeapon;
            return iOut;
        }

        /// <summary>
        /// Retruns the weapon held in the slot specified and if no weapon is in the slot a random one will be generated and outputted
        /// </summary>
        /// <param name="iSlot"></param>
        /// <returns></returns>
        public int ForceGetValidWeapon(int iSlot)
        {
            int iSelected = aiWeaponsSelected[iSlot];
            if (iSelected == -1)
            {
                iSelected = MyRandom.RandomInt(iLargestWeaponVal);
                aiWeaponsSelected[iSlot] = iSelected;
            }
            aiWeaponAmmo[iSlot] = aiWeaponMaxAmmo[iSlot * iLargestWeaponVal + iSelected];
            return iSelected;
        }

        public static int GetWeaponMaxAmmo(int iSlot, int iNum)
        {
            return aiWeaponMaxAmmo[iSlot * iLargestWeaponVal + iNum];
        }

        /// <summary>
        /// Gets the max ammo of the weapon currently held by the player
        /// </summary>
        /// <returns></returns>
        /// <remarks>does no checks for if the player is holding a weapon, so will error if it falls outside of the 'aiWeaponMaxAmmo' array</remarks>
        public int GetWeaponMaxAmmo()
        {
            return aiWeaponMaxAmmo[aiWeaponsSelected[iHoldingWeapon] + (iHoldingWeapon * iLargestWeaponVal)];
        }

        /// <summary>
        /// Returns the weapon index of the currently held weapon
        /// </summary>
        /// <returns></returns>
        /// <remarks>does no checks for if the player is holding a weapon, so will error if it falls outside of the 'aiWeaponMaxAmmo' array</remarks>
        public int GetCurrentWeapon()
        {
            return aiWeaponsSelected[iHoldingWeapon] + iHoldingWeapon * iLargestWeaponVal;
        }

        /// <summary>
        /// Returns the ammo of the weapon currently held
        /// </summary>
        /// <returns></returns>
        /// <remarks>does no checks for if the player is holding a weapon, so will error if it falls outside of the 'aiWeaponMaxAmmo' array</remarks>
        public int GetWeaponCurAmmo()
        {
            return aiWeaponAmmo[iHoldingWeapon];
        }

        public int GetHealth()
        {
            return iHealth;
        }

        /// <summary>
        /// Changes local game stats by 'iStatChange'
        /// </summary>
        /// <param name="iStatChange"></param>
        /// <param name="stEType"></param>
        /// <returns></returns>
        public int AddStatInformation(int iStatChange, Server.StatTypes stEType)
        {
            aiGameStats[(int)stEType] += iStatChange;
            return aiGameStats[(int)stEType];
        }

        public int[] GetAllLocalStats()
        {
            return aiGameStats;
        }

        public int SwapWeapon()
        {
            iHoldingWeapon = (iHoldingWeapon == 0) ? 1 : 0;
            return iHoldingWeapon;
        }

        public void AttachGameObject(int iNew)
        {
            iPlayerObjectId = iNew;
        }

        public int GetPlayerGameObjectID()
        {
            return iPlayerObjectId;
        }

        /// <summary>
        /// Sets the weapon's ammo of the weapon in the player's hands to its maximum
        /// </summary>
        /// <returns></returns>
        public int ReloadWeapon()
        {
            int iWeaponNum = aiWeaponsSelected[iHoldingWeapon] + iHoldingWeapon * iLargestWeaponVal;
            aiWeaponAmmo[iHoldingWeapon] = aiWeaponMaxAmmo[iWeaponNum];
            return aiWeaponAmmo[iHoldingWeapon];
        }

        /// <summary>
        /// Reduces the health of the player assuming they were directly shot by the weapon specified and returns the health reduction
        /// </summary>
        public int ShotByWeapon(int iWeaponNum)
        {
            if(iWeaponNum >= 0)
            {
                iHealth -= aiWeaponDamage[iWeaponNum];
                return aiWeaponDamage[iWeaponNum];
            }
            return 0;
        }

        /// <summary>
        /// Simulates the player picking up the specified pickup type
        /// </summary>
        /// <param name="ptE"></param>
        /// <returns>-1 is returned when the pickup has no effect</returns>
        public int UsePickup(Game.PickupType ptE)
        {
            switch(ptE)
            {
                case Game.PickupType.Health:
                    iHealth += 50;
                    return iHealth;
                case Game.PickupType.Ammo:
                    for(int i = 0; i < aiWeaponAmmo.Length; i++)
                    {
                        aiWeaponAmmo[i] = aiWeaponMaxAmmo[aiWeaponsSelected[i] + i * iLargestWeaponVal];
                    }
                    return aiWeaponAmmo[iHoldingWeapon];
            }
            return -1;
        }

        /// <summary>
        /// Increments all database fields for this client's username by the local stats it accumulated across the game
        /// </summary>
        public void PushStatsToDatabase()
        {
            for(int i = 0; i < aiGameStats.Length; i++)
            {
                Server.SQLHandling.AddToStatField(sUsername, Server.asStatNames[i], aiGameStats[i]);
            }
        }

        /// <summary>
        /// Simulates damage to this client based on the explosion's information
        /// </summary>
        /// <param name="v3ExplosionPos"></param>
        /// <param name="fRadius"></param>
        /// <param name="iMaxDamage"></param>
        /// <param name="iMinDamage"></param>
        /// <returns>Damage dealt</returns>
        public int ExplosionSimulation(Vector3 v3ExplosionPos, float fRadius, int iMaxDamage, int iMinDamage)
        {
            float fDist = Vector3.Distance(v3ExplosionPos, v3LastPos);
            if (fDist > fRadius)
            {
                return 0;
            }
            int iHealthLost = (int)(fDist / fRadius * (iMaxDamage - iMinDamage)) + iMinDamage;
            iHealth -= iHealthLost;
            return iHealthLost;
        }

        public static explicit operator long(Client cliA) => cliA.swatch.ElapsedMilliseconds;
        public static explicit operator string(Client cliA) => cliA.sUsername;
        public static explicit operator bool(Client cliA) => cliA.bLoggedIn;
        public static explicit operator EndPoint(Client cliA) => cliA.endpLoc;
    }
}
