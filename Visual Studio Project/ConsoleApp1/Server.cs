using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using ConsoleApp1.Packets;
using ConsoleApp1.TimeKeepers;
using Microsoft.Data.Sqlite;
namespace ConsoleApp1
{
    /*
     * The server is infrastructure used to allow for client(s) to communicate with the server to allow information transfer between clients without direct connection between each and every client
     */
    internal static class Server
    {
        const int iPort = 9143;  // The port used to communciate over
        const int iMaxDataAlloc = 1024;  // The amount of bytes (and therefore the max length (in bytes)) a packet can have (maximum)
        public const int iTimeoutTimer = 2500;  // Time in milliseconds after the most recent packet that the server will drop the user
        public const int iRoundTimer = 60000;  // Time in milliseconds that each round will last
        public const int iTimeInHub = 15000;  // Time in milliseconds that there is between a game ending and a new game starting
        public const bool TESTMODE = true;  // Makes certain things possible and impossible to help development testing
        /*
         * What TESTMODE does:
         * Makes it impossible for a client to disconnect (including server timeouts)
         * immediately tries to start a game when there are enough players
         * Loadout selection time is 1 second
         * Server automatically allows all login and register requests
         */
        public const bool PLAYGAME = false;  // Whether the server will try to start a game
        public const bool bHideTransformPackets = false;  // Whether the server will output to the console 'TransformDataPacket's

        public const string sSeperator = ";";  // string used to seperate features of packets
        public const int iMapCount = 1;  // Amount of maps the server can pick from
        public const int iGamemodeCount = 2;  // Amount of gamemodes the server can pick from
        static bool bRunning = true;  // Whether the server is running and can receive packets
        static bool bExitEvent = false;
        static IPEndPoint ipendpServerLocation = new IPEndPoint(new IPAddress(new byte[4] { 127, 0, 0, 1 }), iPort);  // Endpoint used for server
        static Socket sockConnection = new Socket(SocketType.Dgram, ProtocolType.Udp);  // Socket used for the server
        static bool bBlockingConnection = false;  // Whether the connection is blocking, so if the server halts until it receives a packet
        static byte[] abyteAllocation = new byte[iMaxDataAlloc];  // Raw data storage used for communicating via packets
        static List<Client> lcliClients = new List<Client>();  // All clients (including clients on the login page)
        static int iIdPointer = 100;  // Network ID that the next object will be assigned to
        static List<GameObject> lgoAll = new List<GameObject>();  // All game objects that have received an NID
        static string[] asBannedTerms = new string[1] { "testbadword" };
        public static string[] asStatNames = { "kills", "deaths", "damage", "shotHit", "shotMiss" };  // These are the strings used inside of databases to store statistics
        public enum StatTypes { Kills, Deaths, DamageDone, ShotsHit, ShotsMissed };  // Statistics storable, in places it uses the size of this enum to dictate how much should be stored
        static Packet[] packTypes = new Packet[23]  // All packet types that can be received and sent
        {
            new ConnectionPacket(),
            new NIDRequestPacket(),
            new TransformPacket(),
            new PollGameObjectPacket(),
            new GetPrefabIDPacket(),
            new ChatPacket(),
            new LoginPacket(),
            new RegisterPacket(),
            new PingPacket(),
            new StartGamePacket(),
            new SelectWeaponPacket(),
            new GetStatsPacket(),
            new FireWeaponPacket(),
            new HealthUpdatePacket(),
            new RoundStartPacket(),
            new GameEndPacket(),
            new UsePickupPacket(),
            new ExplosionPacket(),
            new DeathPacket(),
            new SwapWeaponPacket(),
            new ReloadPacket(),
            new RemoveObjectPacket(),
            new TeamScoreUpdatePacket()
        };
        public enum PacketTypes  // An enum denoting where in 'packTypes' a specific packet would be present
        {
            None = -1,
            Connnection,
            NIDRequest,
            TransformData,
            PollGameObjects,
            GetPrefabID,
            Chat,
            Login,
            Register,
            Ping,
            StartGame,
            SelectWeapon,
            GetStats,
            FireWeapon,
            HealthUpdate,
            RoundStart,
            GameEnd,
            UsePickup,
            Explosion,
            Death,
            SwapWeapon,
            Reload,
            RemoveObject,
            TeamScoreUpdate
        }
        public enum GameState { Hub, Gameplay, Loadout };  // The 3 main game states that allow timers to function within the server
        static GameState gsECur = GameState.Hub;
        static Game gameCur;  // The current game being ran, when this is null no game is being played and the 'GameState' is 'Hub'

        static List<TimeKeeper> atikeepAll = new List<TimeKeeper>();  // The list of all 'TimeKeeper' objects that are currently being used and therefore could be counting down

        public enum TimeKeeperTypes  // The 'TimeKeeper' objects used for determining which one is desired
        {
            HubIntermission,
            GamePrepInHub,
            LoadoutSelection,
            Round,
            PlayerReload
        }

        /// <summary>
        /// Adds a 'PlayerReloadTimer' 'TimeKeeper' to the list of 'TimeKeepers' to be counted down for a player's reload
        /// </summary>
        /// <param name="cliToReload">Client that this counter should be attached to</param>
        /// <returns>The TimeKeeper itself</returns>
        public static PlayerReloadTimer CreateReloadTimer(Client cliToReload)
        {
            PlayerReloadTimer tkeepOut = new PlayerReloadTimer(cliToReload);
            atikeepAll.Add(tkeepOut);
            return tkeepOut;
        }

        /// <summary>
        /// Adds a 'PickupRefreshTimer' 'TimeKeeper' to the list of 'TimeKeepers' to be counted down for a pickup's respawn
        /// </summary>
        /// <param name="iIndex">Pickup index on the map being timed</param>
        /// <returns>The TimeKeeper itself</returns>
        public static PickupRefreshTimer CreatePickupRefreshTimer(int iIndex)
        {
            PickupRefreshTimer tkeepOut = new PickupRefreshTimer(iIndex);
            atikeepAll.Add(tkeepOut);
            return tkeepOut;
        }

        /// <summary>
        /// What occurs before the server runs
        /// Opening the database,
        /// Initialising 'TimeKeeper's that are present perpetually
        /// Defines socket connection
        /// Informs of the server now being in the loop
        /// </summary>
        public static void Startup()
        {
            SQLHandling.InitialiseSQL();
            atikeepAll.Add(new HubIntermissionTimer());
            atikeepAll.Add(new GamePrepInHubTimer());
            atikeepAll.Add(new LoadoutSelectionTimer());
            atikeepAll.Add(new RoundTimer());
            //SQLHandling.ClearSQL();
            sockConnection.Blocking = bBlockingConnection;
            sockConnection.Bind(ipendpServerLocation);
            Console.WriteLine("Socket Open");
        }

        /// <summary>
        /// Used to coordinate for when the server is shut down
        /// </summary>
        public static void Shutdown()
        {
            SQLHandling.Close();
        }

        public enum LoginResponse { Valid, UserInGame, NoUsername, IncorrectPassword }  // The responses the server can have to someone's login attempt

        /// <summary>
        /// Organises SQLite based tasks and seperates all database things out
        /// </summary>
        public static class SQLHandling
        {
            static SqliteConnection sqlconUsed;

            /// <summary>
            /// Opens the database and stores it in 'sqlconUsed', to be used by other functions in 'SQLHandling
            /// </summary>
            public static SqliteConnection Open()
            {
                SqliteConnection sqlcon = new SqliteConnection("Data Source=database.db");
                sqlcon.Open();
                sqlconUsed = sqlcon;
                return sqlcon;
            }

            /// <summary>
            /// Creates the tables used for the project if they don't exist
            /// </summary>
            public static void CreateTables()
            {
                SqliteCommand sqlcom = new SqliteCommand("CREATE TABLE IF NOT EXISTS loginCredentials(username TEXT PRIMARY KEY NOT NULL, password TEXT NOT NULL);", sqlconUsed);
                sqlcom.ExecuteNonQuery();
                SqliteCommand sqlcomStats = new SqliteCommand("CREATE TABLE IF NOT EXISTS statistics(username TEXT PRIMARY KEY NOT NULL, kills INTEGER NOT NULL, deaths INTEGER NOT NULL, damage INTEGER NOT NULL, shotHit INTEGER NOT NULL, shotMiss INTEGER NOT NULL);", sqlconUsed);
                sqlcomStats.ExecuteNonQuery();
            }

            /// <summary>
            /// This is ran on the Server's startup (helpful for bulk function calls in SQLHandling)
            /// </summary>
            public static void InitialiseSQL()
            {
                
                Open();
                CreateTables();
                //ReadAllCredentials();
            }

            /// <summary>
            /// Creats a new account that can be logged in, with stats set to 0.
            /// This is only used when a user registers, so it auto logs them in, if information is valid
            /// </summary>
            /// <param name="sUsername">Username the player would like their account to be under</param>
            /// <param name="sPassword">Hashed password the player typed in</param>
            public static void AddNewCredential(string sUsername, string sPassword)
            {
                if (!ContainsUsername(sUsername))
                {
                    SqliteCommand sqlcom = new SqliteCommand(@"INSERT INTO loginCredentials(username, password) VALUES (@user, @pass);", sqlconUsed);
                    sqlcom.Parameters.AddWithValue("@user", sUsername);
                    sqlcom.Parameters.AddWithValue("@pass", GenerateHash(sUsername, sPassword));
                    sqlcom.ExecuteNonQuery();
                    sqlcom = new SqliteCommand($@"INSERT INTO statistics(username, {asStatNames[(int)StatTypes.Kills]}, {asStatNames[(int)StatTypes.Deaths]}, {asStatNames[(int)StatTypes.DamageDone]}, {asStatNames[(int)StatTypes.ShotsHit]}, {asStatNames[(int)StatTypes.ShotsMissed]}) VALUES (@user, @kill, @death, @damage, @hit, @miss);", sqlconUsed);
                    sqlcom.Parameters.AddWithValue("@user", sUsername);
                    sqlcom.Parameters.AddWithValue("@kill", 0);
                    sqlcom.Parameters.AddWithValue("@death", 0);
                    sqlcom.Parameters.AddWithValue("@damage", 0);
                    sqlcom.Parameters.AddWithValue("@hit", 0);
                    sqlcom.Parameters.AddWithValue("@miss", 0);
                    sqlcom.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// Returns true if the inputted username is in the 'loginCredentials' table
            /// </summary>
            public static bool ContainsUsername(string sUsername)
            {
                SqliteCommand sqlcom = new SqliteCommand(@"SELECT * FROM loginCredentials WHERE username = @user;", sqlconUsed);
                sqlcom.Parameters.AddWithValue("@user", sUsername);
                SqliteDataReader sqlreader = sqlcom.ExecuteReader();
                return sqlreader.HasRows;
            }

            /// <summary>
            /// Gets the double-hashed password for the respective username
            /// </summary>
            static string GetPassword(string sUsername)
            {
                SqliteCommand sqlcom = new SqliteCommand(@"SELECT password FROM loginCredentials WHERE username = @user;", sqlconUsed);
                sqlcom.Parameters.AddWithValue("@user", sUsername);
                SqliteDataReader sqlreader = sqlcom.ExecuteReader();
                //Console.WriteLine(sqlreader.HasRows.ToString());
                sqlreader.Read();
                return sqlreader.GetString(0);
            }

            /// <summary>
            /// Used for Debug: Writes all usernames and double-hashed passwords to the console
            /// </summary>
            public static void ReadAllCredentials()
            {
                SqliteCommand sqlcom = new SqliteCommand("SELECT * FROM loginCredentials;", sqlconUsed);
                SqliteDataReader sqlreader = sqlcom.ExecuteReader();
                while (sqlreader.Read())
                {
                    Console.WriteLine(sqlreader.GetString(0) + sqlreader.GetString(1));
                }
            }

            /// <summary>
            /// Gets the value of a specific statistic attached to the player
            /// </summary>
            /// <param name="sUsername"></param>
            /// <param name="sField">Name of the field, to avoid errors use the server's array of stat names</param>
            /// <returns></returns>
            /// <remarks>This function has gone unused for a long period of time and isn't used in the final submission, so it is not necessarilly functional</remarks>
            public static int GetStatField(string sUsername, string sField)
            {
                SqliteCommand sqlcom = new SqliteCommand(@"SELECT @field FROM statistics WHERE username = @user;", sqlconUsed);
                sqlcom.Parameters.AddWithValue("@user", sUsername);
                sqlcom.Parameters.AddWithValue("@field", sField);
                SqliteDataReader sqlreader = sqlcom.ExecuteReader();
                sqlreader.Read();
                return sqlreader.GetInt32(0);
            }

            /// <summary>
            /// Returns all the player's stats (specified by 'sUsername' in an array of ints)
            /// </summary>
            /// <param name="sUsername"></param>
            /// <returns>The ints are ordered in the same way as in the enum within the 'Server'</returns>
            public static int[] GetStats(string sUsername)
            {
                string sAllAttribs = "";
                for(int i = 0; i < asStatNames.Length; i++)
                {
                    sAllAttribs += $" {asStatNames[i]}";
                    if(i != asStatNames.Length - 1)
                    {
                        sAllAttribs += ",";
                    }
                }   
                SqliteCommand sqlcom = new SqliteCommand($"SELECT{sAllAttribs} FROM statistics WHERE username = @user;", sqlconUsed);
                sqlcom.Parameters.AddWithValue("@user", sUsername);
                SqliteDataReader sqlreader = sqlcom.ExecuteReader();
                int[] aiOut = new int[asStatNames.Length];
                while(sqlreader.Read())
                {
                    for(int i = 0; i < asStatNames.Length; i++)
                    {
                        aiOut[i] = sqlreader.GetInt32(i);
                    }
                }
                return aiOut;
            }

            /// <summary>
            /// Increases 'sUsername''s stat specified by 'sField' by 'iChange'
            /// </summary>
            /// <param name="sUsername"></param>
            /// <param name="sField">to avoid errors use the server's array of stat names</param>
            /// <param name="iChange">Can be negative</param>
            public static void AddToStatField(string sUsername, string sField, int iChange)
            {
                if(iChange > 0)
                {
                    SqliteCommand sqlcom = new SqliteCommand($"UPDATE statistics SET [{sField}] = [{sField}] + @added WHERE username = @user;", sqlconUsed);
                    sqlcom.Parameters.AddWithValue("@added", iChange);
                    sqlcom.Parameters.AddWithValue("@user", sUsername);
                    sqlcom.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// Clears 'loginCredentials', used for testing
            /// </summary>
            /// <remarks>Given this was made before the 'statistics' table, this can cause problems when you remove a username and then try to remake an account using the same username that was deleted, since it tries to auto-repopulate the statistics table with the new username</remarks>
            public static void ClearSQL()
            {
                SqliteCommand sqlcom = new SqliteCommand("DELETE FROM loginCredentials;", sqlconUsed);
                sqlcom.ExecuteNonQuery();
            }

            /// <summary>
            /// Closes the connection to the database
            /// </summary>
            public static void Close()
            {
                sqlconUsed.Close();
            }

            /// <summary>
            /// Used to determine if the password the user inputted matches what they inputted when they registered
            /// </summary>
            /// <param name="sUsername">username is required since the password in the database is hashed using it</param>
            /// <param name="sPassword">the hashed input the player types</param>
            /// <returns>If the inputted password matches</returns>
            static bool ComparePassword(string sUsername, string sPassword)
            {
                string sPasswordToCompare = GetPassword(sUsername);
                return GenerateHash(sUsername, sPassword) == sPasswordToCompare;
            }

            /// <summary>
            /// What response should occur in response to the inputted username and password by the player
            /// </summary>
            /// <param name="sUsername"></param>
            /// <param name="sPassword"></param>
            /// <returns></returns>
            public static LoginResponse UserLogin(string sUsername, string sPassword)
            {
                if (!ContainsUsername(sUsername))
                {
                    return LoginResponse.NoUsername;
                }
                else if (GetClientByUsername(sUsername) != null)
                {
                    return LoginResponse.UserInGame;
                }
                else if (ComparePassword(sUsername, sPassword))
                {
                    return LoginResponse.Valid;
                }
                return LoginResponse.IncorrectPassword;
            }
        }

        /// <summary>
        /// I started an XML version of the database, but didn't finish before moving onto SQL, this class should be treated as deprecated
        /// </summary>
        public static class XMLHandling
        {
            const string sXMLFilepath = "PlayerInformation.xml";

            public static void InitialiseXML()
            {
                CreateXML();
                if (!File.Exists(sXMLFilepath))
                {
                    CreateXML();
                }
            }

            static void CreateXML()
            {
                XmlWriterSettings xmlwset = new XmlWriterSettings();
                xmlwset.Indent = true;
                xmlwset.NewLineOnAttributes = true;
                using (XmlWriter xmlw = XmlWriter.Create(sXMLFilepath))
                {
                    xmlw.WriteStartDocument();
                    xmlw.WriteStartElement("Users");
                    xmlw.WriteEndElement();
                    xmlw.WriteEndDocument();
                    xmlw.Close();
                }
            }

            public static void NewUserToXML(string sUsername, string sPassword)
            {
                sPassword = GenerateHash(sUsername, sPassword);
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(sXMLFilepath);
                AddXMLElement(xmldoc.DocumentElement, AddXMLElement(StringToXMLElementNode(xmldoc, "User"), AddXMLElements(StringToXMLElementNode(xmldoc, "Credentials"), StringToXMLElement(xmldoc, "Username", sUsername), StringToXMLElement(xmldoc, "Password", sPassword))));
                xmldoc.Save(sXMLFilepath);
            }

            static XmlElement AddXMLElement(XmlElement xmlelemBase, XmlElement xmlelemNew)
            {
                xmlelemBase.AppendChild(xmlelemNew);
                return xmlelemBase;
            }

            static XmlElement AddXMLElements(XmlElement xmlelemBase, params XmlElement[] axmlelemToAdd)
            {
                for (int i = 0; i < axmlelemToAdd.Length; i++)
                {
                    AddXMLElement(xmlelemBase, axmlelemToAdd[i]);
                }
                return xmlelemBase;
            }

            static XmlElement StringToXMLElementNode(XmlDocument xmldoc, string sIn)
            {
                return xmldoc.CreateElement(sIn);
            }

            static XmlElement StringToXMLElement(XmlDocument xmldoc, string sElem, string sVal)
            {
                XmlElement xmlelemOut = xmldoc.CreateElement(sElem);
                xmlelemOut.InnerText = sVal;
                return xmlelemOut;
            }

            public static string[] GetAttributeOfAllUsers(params string[] asPath)
            {
                XmlNodeList xmlnl = GetAllUserInformation();
                string[] asOut = new string[xmlnl.Count];
                for (int i = 0; i < xmlnl.Count; i++)
                {
                    XmlNode xmlnSelected = xmlnl[i];
                    for (int j = 0; j < asPath.Length; j++)
                    {
                        xmlnSelected = xmlnSelected[asPath[j]];
                    }
                    asOut[i] = xmlnSelected.InnerText;
                }
                return asOut;
            }

            static string GetAttribute(int iUserIndex, params string[] asPath)
            {
                XmlNode xmlnSelected = GetAllUserInformation()[iUserIndex];
                for (int i = 0; i < asPath.Length; i++)
                {
                    xmlnSelected = xmlnSelected[asPath[i]];
                }
                return xmlnSelected.InnerText;
            }

            static XmlNodeList GetAllUserInformation()
            {
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(sXMLFilepath);
                return xmldoc.DocumentElement.ChildNodes;
            }

            public static string[] GetAllUsernames()
            {
                return GetAttributeOfAllUsers("Credentials", "Username");
            }

            public static bool ComparePasswords(int iUserIndex, string sPassword)
            {
                string sUsername = GetAttribute(iUserIndex, "Credentials", "Username");
                string sPasswordToCompare = GetAttribute(iUserIndex, "Credentials", "Password");
                return GenerateHash(sUsername, sPassword) == sPasswordToCompare;
            }

            public static int UsernameInDatabase(string sUsername)
            {
                string[] asUsernames = GetAllUsernames();
                for (int i = 0; i < asUsernames.Length; i++)
                {
                    if (asUsernames[i] == sUsername)
                    {
                        return i;
                    }
                }
                return -1;
            }


            public static LoginResponse UserLogin(string sUsername, string sPassword)
            {
                int iUserIndex = UsernameInDatabase(sUsername);
                if (iUserIndex < 0)
                {
                    return LoginResponse.NoUsername;
                }
                else if (GetClientByUsername(sUsername) != null)
                {
                    return LoginResponse.UserInGame;
                }
                else if (ComparePasswords(iUserIndex, sPassword))
                {
                    return LoginResponse.Valid;
                }
                return LoginResponse.IncorrectPassword;
            }

            public static void AddNewLogin(string sUsername, string sPassword)
            {
                NewUserToXML(sUsername, sPassword);
            }
        }

        public static int GetStatInformation(string sUsername, StatTypes stEThis)
        {
            return SQLHandling.GetStatField(sUsername, asStatNames[(int)stEThis]);
        }

        public static int[] GetStatInformation(string sUsername)
        {
            return SQLHandling.GetStats(sUsername);
        }

        public static void ChangeStat(Client cliThis, StatTypes stE, int iChange)
        {
            cliThis.AddStatInformation(iChange, stE);
            if(stE == StatTypes.Kills && iChange > 0)
            {
                gameCur.IncrementScore(cliThis);
                gameCur.TryShowTeamScore();
            }
        }

        /// <summary>
        /// What occurs at every server tick
        /// 
        /// 1 : UpdateGameState() - Checks if the game state needs to be updated due to some special conditions (outside of 'TimeKeeper' objects)
        /// 2 : CheckForTimerRemoval() - Whether a 'TimeKeeper' object has specified it needs to be removed, since it has outlived its usefullness
        /// 3 : atikeepAll[i].Update() - The Update loop for each 'TimeKeeper' object
        /// 4 : ListenForMessage() - Checking for any messages sent by client
        /// 5 : CheckForClientTimeout() - Check if the server needs to disconnect clients refusing to send packets
        /// </summary>
        public static void Loop()
        {
            while (bRunning)
            {
                UpdateGameState();
                CheckForTimerRemoval();
                for (int i = 0; i < atikeepAll.Count; i++)
                {
                    atikeepAll[i].Update();
                }
                ListenForMessage();
                if(!Server.TESTMODE) CheckForClientTimeout();
            }
        }

        /// <summary>
        /// This updates the game state within the main server loop, due to some very specific conditions, only 1 is currently used:
        /// This starts a new round if the game state is 'Gameplay' and there are not enough players to constitute the continuation of the round
        /// </summary>
        static void UpdateGameState()
        {
            if(gameCur != null)
            {
                if(gameCur.NotEnoughPlayers() && GetCurrentGameState() == GameState.Gameplay)
                {
                    NewRound();
                }
            }
        }

        /// <summary>
        /// Function coordinating the end of the game, shutting down the 'Game' object and removing it
        /// </summary>
        public static void EndGame()
        {
            NewGameState(GameState.Hub);
            gameCur.Shutdown();
            gameCur = null;
        }

        /// <summary>
        /// This checks if any 'TimeKeeper' object has signalled that it will not be useful to the server and therefore needs removing
        /// </summary>
        /// <returns>Number of 'TimeKeeper' objects deleted from the list</returns>
        static int CheckForTimerRemoval()
        {
            int iDeleted = 0;
            for (int i = atikeepAll.Count - 1; i >= 0; i--)
            {
                if (atikeepAll[i].bRemoveTimer)
                {
                    atikeepAll.RemoveAt(i);
                    iDeleted++;
                }
            }
            return iDeleted;
        }

        /// <summary>
        /// Searches for the specific 'GameObject' and removes it
        /// </summary>
        public static void RemoveObjectByNetworkID(int iNetId)
        {
            for(int i = 0; i < lgoAll.Count; i++)
            {
                if (lgoAll[i].GetID() == iNetId)
                {
                    DeleteObjectByIndex(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Removes a client deemed to have not sent a packet recently and cleanly disconnects them from other client's perspectives
        /// </summary>
        static void TimeoutClient(Client cliToKill)
        {
            lcliClients.Remove(cliToKill);
            for (int i = 0; i < lgoAll.Count; i++)
            {
                if (cliToKill.Equals((Client)lgoAll[i]))
                {
                    if (lgoAll[i].GetPrefabID() == 0)
                    {
                        SendServerChatMessage($"{(string)cliToKill} has been disconnected");
                    }
                    DeleteObjectByIndex(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Removes a 'GameObject' by their index inside of 'lgoAll' and Sends a 'RemoveObjectPacket' to all other clients with an attached id of the object
        /// </summary>
        /// <param name="iIndex">index of the object in 'lgoAll'</param>
        static void DeleteObjectByIndex(int iIndex)
        {
            SpoofPacket(PacketTypes.RemoveObject, null, lgoAll[iIndex].GetID().ToString());
            lgoAll.RemoveAt(iIndex);
        }

        /// <summary>
        /// Checks if a client's most recent packet sent has exceeded the time before it considers the client to not be useful, either because they aren't sending packets often enough due to very poor connection or malicious reasons
        /// </summary>
        /// <returns>The amount of users timed out</returns>
        static int CheckForClientTimeout()
        {
            Client[] acliAll = GetAllLoggedInClients();
            int iOut = 0;
            for (int i = 0; i < acliAll.Length; i++)
            {
                if ((long)acliAll[i] > iTimeoutTimer)
                {
                    TimeoutClient(acliAll[i]);
                    iOut++;
                }
            }
            return iOut;
        }

        /// <summary>
        /// Sends a chat message to all players under the pseudoname of 'SERVER'
        /// </summary>
        /// <param name="sMessage">text every player will see after 'SERVER :'</param>
        /// <returns>If this message was sent to anyone</returns>
        public static bool SendServerChatMessage(string sMessage)
        {
            bool bOut = false;
            if (lcliClients.Count > 0)
            {
                SpoofPacket(PacketTypes.Chat, null, sMessage);
                bOut = true;
            }
            return bOut;
        }

        /// <summary>
        /// Updates 'abyteAllocation' to avoid issues of it being filled with past information
        /// </summary>
        static void ReallocateBytes()
        {
            abyteAllocation = new byte[iMaxDataAlloc];
        }

        /// <summary>
        /// Pairs a specific message with a client in its list, if a client can't be found then null is returned to prompt the server to create a client for this endpoint
        /// </summary>
        /// <param name="endpSender">'EndPoint' of the message</param>
        /// <returns>'Client' object this message is attached to</returns>
        static Client DetermineSender(EndPoint endpSender)
        {
            for (int i = 0; i < lcliClients.Count; i++)
            {
                if (((EndPoint)lcliClients[i]).Equals(endpSender))
                {
                    return lcliClients[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new 'Client' object for this 'EndPoint'
        /// </summary>
        /// <returns>'Client' created</returns>
        /// <remarks>make sure to always run 'DetermineSender' before this if you don't want duplicate 'Client's</remarks>
        static Client NewClient(EndPoint endp)
        {
            Client cli = new Client(endp);
            lcliClients.Add(cli);
            return cli;
        }

        /// <summary>
        /// The logic used in the server's main loop to determine what is being sent to it
        /// </summary>
        static void ListenForMessage()
        {
            try
            {
                ReallocateBytes();
                EndPoint endpSender = new IPEndPoint(IPAddress.Any, 0);
                int iByteCount = sockConnection.ReceiveFrom(abyteAllocation, ref endpSender);  // this errors if there is no message being transmitted in this moment
                string sMessage = Encoding.ASCII.GetString(abyteAllocation, 0, iByteCount);
                Client cliSender = DetermineSender(endpSender);
                if (cliSender == null)
                {  // if the sender has not sent a message to the server in the past
                    PacketTypes ptE = HeaderCheck(sMessage);
                    if (ptE == PacketTypes.Connnection)
                    {  // Creates the new client ONLY if they have sent a 'ConnectionPacket' to avoid timed out clients from being readded when they start transmitting again
                        cliSender = NewClient(endpSender);
                    }

                }
                if (cliSender != null)
                {
                    PacketTypes ptE = HeaderCheck(sMessage);
                    if (ptE != PacketTypes.TransformData || !bHideTransformPackets)
                    { // Writes to console the packet if it isn't a 'TransformDataPacket' (because this is the most sent packet and isn't useful in most test cases
                        Console.WriteLine($"Message received from {endpSender.ToString()}");  // Every packet received by a client will have 'Message Received' header above
                        Console.WriteLine(sMessage);
                    }
                    DeterminePacket(sMessage, cliSender);
                }
            }
            catch (SocketException ex)
            {
                int i;
            }
        }

        /// <summary>
        /// Used to check the header of the message sent and therefore push it into the correct pipeline for its packet type
        /// </summary>
        static void DeterminePacket(string sMessage, Client cliSender)
        {
            for (int i = 0; i < packTypes.Length; i++)
            {
                if (sMessage.Length >= packTypes[i].GetHeader().Length)
                {  // if the message is long enough to even contain the header being compared
                    if (sMessage.Substring(0, packTypes[i].GetHeader().Length) == packTypes[i].GetHeader())
                    {
                        packTypes[i].PacketLoop(sMessage, cliSender);
                        return;
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Used by the server to quickly determine the packet's type
        /// </summary>
        static PacketTypes HeaderCheck(string sMessage)
        {
            for (int i = 0; i < packTypes.Length; i++)
            {
                if (packTypes[i].GetHeader().Length <= sMessage.Length)
                {
                    if (sMessage.Substring(0, packTypes[i].GetHeader().Length) == packTypes[i].GetHeader())
                    {
                        return (PacketTypes)i;
                    }
                }
            }
            return PacketTypes.None;
        }

        /// <summary>
        /// Transmits to a specific 'Client' a message
        /// </summary>
        public static void SendMessage(Client cliReceiver, string sMessage)
        {
            ReallocateBytes();
            abyteAllocation = Encoding.ASCII.GetBytes(sMessage);
            sockConnection.SendTo(abyteAllocation, abyteAllocation.Length, SocketFlags.None, (EndPoint)cliReceiver);
        }

        /// <summary>
        /// Creates a NID (Network IDentifier) and a 'GameObject' for handling future 'TransformDataPacket's with this NID. Stores the 'Client's assigned localID and the PrefabID
        /// </summary>
        /// <param name="iLocalId">'Client's generated ID for this object (doesn't matter if it works in the server's context, so long as the 'Client' keeps it consistant, used only for niche cases)</param>
        /// <param name="iPrefId">Prefab ID to be able to tell other users the type of object this is</param>
        /// <param name="cliOwner">The owner of the object to understand where this information should be coming from</param>
        /// <returns>the NID for this 'GameObject'</returns>
        /// <remarks>This generates an NID every time its ran, so run sparingly</remarks>
        public static int GenerateNetworkID(int iLocalId, int iPrefId, Client cliOwner)
        {
            GameObject goIn = new GameObject(iIdPointer, iPrefId, iLocalId);
            if(iPrefId == 0)
            {
                cliOwner.AttachGameObject(iIdPointer);
            }
            goIn.AttachClient(cliOwner);
            lgoAll.Add(goIn);
            iIdPointer++;
            return iIdPointer - 1;
        }

        /// <summary>
        /// Sets the 'GameObject's transform according to an incoming 'TransformDataPacket'
        /// </summary>
        /// <returns>If the 'GameObject''s transform has been changed</returns>
        public static bool NewGameObjectTransform(int iIdToChange, Vector3 v3NewPos, Vector4 v4NewRot)
        {
            for (int i = 0; i < lgoAll.Count; i++)
            {
                if (lgoAll[i].GetID() == iIdToChange)
                {
                    return lgoAll[i].NewTransform(v3NewPos, v4NewRot);
                }
            }
            return true;
        }

        /// <summary>
        /// Tells the program if the given piece of text is not allowed due to censorship or other reasons.
        /// 
        /// This only checks for 'testbadword', but if it functions for that it could theoretically function for any groups of strings
        /// </summary>
        /// <param name="sText">The text being checked for if its appropriate to show to the player</param>
        /// <returns>true if it is fine by the server's standards</returns>
        public static bool BannedPhraseFilter(string sText)
        {
            for (int i = 0; i < asBannedTerms.Length; i++)
            {
                for (int j = 0; j < sText.Length - asBannedTerms[i].Length; j++)
                {
                    if (sText.Substring(j, asBannedTerms[i].Length).ToLower() == asBannedTerms[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// This used by the server to fake a message coming in by pushing a specific string into the packet checker directly, which will run the logic of the packet it identifies.
        /// The Server runs its packets on a response-only system (meaning it can ONLY send packets in response), so it needs to fake a packet to do things on its own, most of the time 'cliSender' can be null, since this will signal inside of packets that the server is telling itself to do something
        /// This is also quite helpful, when the server knows the client will later ask for some information, it can ahead of time provide it by faking the client sending the request
        /// </summary>
        /// <param name="ptEUsed">Type of packet intended to send (automatically fills the header for this type of packet)</param>
        /// <param name="cliSender">Sender the server wants the packet to appear to have been sent by</param>
        /// <param name="asParams">The parameters that get automatically seperated using the 'Packet' class' own packet creation system</param>
        public static void SpoofPacket(PacketTypes ptEUsed, Client cliSender, params string[] asParams)
        {
            DeterminePacket(Packet.SimplePacket(packTypes[(int)ptEUsed].GetHeader(), asParams), cliSender);
        }

        /// <summary>
        /// Older iteration of 'SpoofPacket' use newer version for ease of use
        /// </summary>
        public static void SpoofPacket(string sMessage, Client cliSender)
        {
            DeterminePacket(sMessage, cliSender);
        }

        /// <summary>
        /// Returns the 'Client' object if the server only has a username
        /// </summary>
        public static Client GetClientByUsername(string sUsername)
        {
            Client[] acliAll = GetAllLoggedInClients();
            for (int i = 0; i < acliAll.Length; i++)
            {
                if (((string)acliAll[i]).Equals(sUsername))
                {
                    return acliAll[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all clients that have passed the login screen
        /// </summary>
        public static Client[] GetAllLoggedInClients()
        {
            Client[] cliAll = GetAllClients();
            int iSum = 0;
            for (int i = 0; i < cliAll.Length; i++)
            {
                if ((bool)cliAll[i])
                {
                    iSum++;
                }
            }
            int iPointer = 0;
            Client[] cliOut = new Client[iSum];
            for (int i = 0; i < cliAll.Length && iPointer != cliOut.Length; i++)
            {
                if ((bool)cliAll[i])
                {
                    cliOut[iPointer] = cliAll[i];
                    iPointer++;
                }
            }
            return cliOut;
        }

        /// <summary>
        /// Creates a new game given a map index and gamemode index (these values are auto-generated, but could be used in a possible map voting system or simply game searcher for a specific gamemode
        /// </summary>
        /// <returns>'Game' object created</returns>
        public static Game StartNewGame(int iMap, int iMode)
        {
            gameCur = new Game(GetAllLoggedInClients(), iMap, iMode);
            gameCur.GenerateSpawnPoints();
            return gameCur;
        }

        /// <summary>
        /// Prompts the 'Game' that the 'Server' wants a new round and therefore the 'Game' checks if a win condition has been met and if the game should continue
        /// </summary>
        /// <returns>null if the game ends, reference to the game if the next round has started</returns>
        public static Game NewRound()
        {
            bool bGameEnd = gameCur.NewRound();
            if(bGameEnd)
            {
                SpoofPacket(PacketTypes.GameEnd, null);
                return null;
            }
            else
            {
                NewGameState(GameState.Loadout);
                SpoofPacket(PacketTypes.RoundStart, null);
                return gameCur;
            }
        }

        public static Game GetCurrentGame()
        {
            return gameCur;
        }

        /// <summary>
        /// Returns the 'Client' object by the NID its instance of the player prefab uses
        /// </summary>
        public static Client GetClientByNID(int iNetId)
        {
            Client[] acliAll = GetAllLoggedInClients();
            for(int i = 0; i < acliAll.Length; i++)
            {
                if (acliAll[i].GetPlayerGameObjectID() == iNetId)
                {
                    return acliAll[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a 'GameObject' by its NID
        /// </summary>
        public static GameObject GetGameObjectByID(int iId)
        {
            GameObject[] agoAll = GetAllGameObjects();
            for(int i = 0; i < agoAll.Length; i++)
            {
                if (agoAll[i].GetID() == iId)
                {
                    return agoAll[i];
                }
            }
            return null;
        }

        public static Client[] GetAllClients() { return lcliClients.ToArray(); }
        public static GameObject[] GetAllGameObjects() { return lgoAll.ToArray(); }
        //public static Prefab[] GetAllPrefabs() { return lprefAll.ToArray(); }
        public static Packet GetPacketObject(PacketTypes ptE) { return packTypes[(int)ptE]; }

        public static GameState GetCurrentGameState() { return gsECur; }
        public static void NewGameState(GameState gsE) { gsECur = gsE; }

        /// <summary>
        /// override for 'GenerateHash' to use both the username and password in a consistant way to avoid passwords being generated in unexpected ways
        /// </summary>
        /// <returns>a hashed string using the hashed password the player typed in and the player's username at the end to create a unique hash difficult to reverse</returns>
        static string GenerateHash(string sUsername, string sPassword)
        {
            return Server.GenerateHash(sPassword + sUsername);
        }

        /*
         * NOTE : This method was used by me in 5035CMD
         */

        public static string GenerateHash(string input)
        {
            byte[] data = SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder inputHash = new StringBuilder();
            foreach (byte b in data)
            {
                inputHash.Append(b.ToString("x2"));
            }
            return inputHash.ToString();
        }

        /*
         * End of reused code
         */
    }
}
