using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.UIElements;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Unity.VisualScripting;

public class NetworkManager : MonoBehaviour
{
    [SerializeField] GameObject[] agoAssetPoolSetup;
    [SerializeField] MapLogic[] amlMapsSetup;
    static GameObject[] agoAssetPool;
    static MapLogic[] amlMaps;
    [SerializeField, Range(0f, 1f)]public float fPacketDropChance = 0f;
    public enum SendPacket { None = -1, Connection, NIDRequest, Transform, PollGameObjects, GetPrefabID, Chat, Login, Register, Ping, StartGame, SelectWeapon, GetStats, FireWeapon, HealthUpdate, RoundStart, GameEnd, UsePickup, Explosion, Death, SwapWeapon, Reload, RemoveObject, TeamScoreUpdate}
    static IPEndPoint ipendpClient;
    static EndPoint endpSender;
    public Socket sockConnection;
    [SerializeField] Camera camTest;
    static Camera TESTCAMERA;
    static PacketResponse[] apackresDict;
    static bool bAllowDroppingPackets = false;
    byte[] abData;
    int recv = 0;
    static float fTimeoutCheck = -0.5f;  // Set this to a value that will represent how much will be added
    static float fTimeSinceLastPacket = 0f;
    static SendPacket spNextPacket = SendPacket.Connection;
    static string sPacketSeperator = "";
    static string sPrevMessageReceived = "";
    static Queue<TransformPacketData> qtpdAll = new Queue<TransformPacketData>();
    static List<NetworkGameObject> lngoAll = new List<NetworkGameObject>();
    static Queue<SendPacket> qspE = new Queue<SendPacket>();
    static UIManager uim;
    static GameLogic glThis;
    static bool bCredentialsConfirmed = false;
    static bool bNetworkIdAssignmentComplete = false;
    static PlayerMovement pmPlayer;

    public struct TransformPacketData
    {
        public Vector3 v3Pos;
        public Quaternion quatRot;
        public int iId;
    }

    public abstract class PacketResponse
    {
        public enum ErrorType { ServerResponse};
        protected string sHeader = "";
        protected string[] asMessageParams;
        protected int iConsecutivePackets = 0;

        public virtual void Reset()
        {
            iConsecutivePackets = 0;
            fTimeSinceLastPacket = 0f;
        }
        public virtual string GetHeader() { return sHeader; }

        /// <summary>
        /// Does logic to prepare the message, allowing sitations where this packet can loop until this returns false and makes it an invalid packet to send
        /// </summary>
        /// <returns>Whether the initialised values in the class are valid</returns>
        /// <remarks>While this method's function is very similar to 'CreateMessage, the intended purpose is to check if initialised values are set correctly and only allow important messages to be sent to the server, acting as a validation of the message</remarks>
        public virtual bool MessageSetup() { return iConsecutivePackets == 0; }

        /// <summary>
        /// Creates a message using a consistant algorithm, which could be theoretically overriden
        /// </summary>
        /// <returns>What is expected to be sent to the server</returns>
        /// <remarks>This is expected to not be overriden for 99% of circumstances, however unique packets can change that</remarks>
        public virtual string CreateMessage()
        {
            iConsecutivePackets++;
            string sOut = $"{sHeader}{sPacketSeperator}";
            if (asMessageParams != null)
            {
                for (int i = 0; i < asMessageParams.Length; i++)
                {
                    if(asMessageParams[i] != null)
                    {
                        sOut += $"{asMessageParams[i]}{sPacketSeperator}";
                    }   
                }
            }
            return sOut;
        }

        /// <summary>
        /// Does logic in response to the server
        /// </summary>
        /// <param name="sMessage">Response the server provides the client</param>
        public virtual void Response(string sMessage) { return; }

        public string[] UseSeperator(string sMessage)
        {
            List<string> lsOut = new List<string>();
            string sTemp = "";
            bool bBasicResponse = true;
            for(int i = 0; i < sMessage.Length; i++)
            {
                if(sMessage.Length - i > sPacketSeperator.Length)
                {
                    if(sMessage.Substring(i, sPacketSeperator.Length) == sPacketSeperator)
                    {
                        lsOut.Add(sTemp);
                        sTemp = "";
                        bBasicResponse = false;
                    }
                }
                if(bBasicResponse)
                {
                    sTemp += sMessage[i];
                }
                bBasicResponse = true;
            }
            lsOut.Add(sTemp);
            return lsOut.ToArray();
        }

        protected void ErroniousPacket(string sError, ErrorType etE)
        {
            string sMessage = "";
            switch(etE)
            {
                case ErrorType.ServerResponse:
                    sMessage += "Server Response to ";
                    break;
            }
            sMessage += $"Packet : {sHeader} {sError}";
            Debug.LogWarning(sMessage);
        }
    }

    /// <summary>
    /// Packet is sent to the server to confirm that this client is known to now be in the game, the response packet the server provides also gives this the centrally defined seperator that the server would like its clients to use
    /// </summary>
    public class ConnectionPacket : PacketResponse
    {
        Socket sockConnection;
        IPEndPoint ipendpClient;
        public ConnectionPacket()
        {
            sHeader = "Connected";
        }

        public ConnectionPacket(Socket sock, IPEndPoint ipendp) : this()
        {
            sockConnection = sock;
            ipendpClient = ipendp;
        }

        public override bool MessageSetup()
        {
            if (sockConnection == null || ipendpClient == null || iConsecutivePackets > 0)
            {
                return false;
            }
            sockConnection.Connect(ipendpClient);
            return true;
        }

        public override void Response(string sMessage)
        {
            string sRelevant = sMessage.Substring(sHeader.Length);
            string sPart1 = sRelevant.Substring(0, sRelevant.Length / 2);
            string sPart2 = sRelevant.Substring(sRelevant.Length / 2, sRelevant.Length / 2);
            for(int i = sPart1.Length; i >= 1; i--)
            {
                bool bValid = true;
                for(int j = 0; j < sPart1.Length && bValid; j++)
                {
                    if (sPart1[j] != sPart2[j])
                    {
                        bValid = false;
                    }
                }
                if(bValid)
                {
                    break;
                }
                else
                {
                    sPart1 = sPart1.Substring(0, sPart1.Length - 1);
                    sPart2 = sRelevant.Substring(sPart1.Length, sPart1.Length);
                }
            }
            sPacketSeperator = sPart1;
            fTimeoutCheck += int.Parse(sRelevant.Substring(sPart1.Length * 2)) / 1000f;
        }
    }

    /// <summary>
    /// Requests the 'iNetworkId' for each locally owned 'NetworkGameObject' ('bLocallyOwned' == true)
    /// </summary>
    public class NIDRequestPacket : PacketResponse
    {
        NetworkGameObject[] angoObjectsForID;
        public static int iSent = 0;
        int iReceived = 0;
        public NIDRequestPacket()
        {
            sHeader = "NID Request";
        }

        public override bool MessageSetup()
        {
            if(angoObjectsForID == null)
            {
                NetworkGameObject[] angoTemp = FindObjectsByType<NetworkGameObject>(FindObjectsSortMode.None);
                bool[] abValid = new bool[angoTemp.Length];
                int iValidSum = 0;
                for(int i = 0; i < angoTemp.Length; i++)
                {
                    lngoAll.Add(angoTemp[i]);
                    abValid[i] = angoTemp[i].IsLocallyOwned() && !angoTemp[i].ValidNetworkId();
                    if (abValid[i]) { iValidSum++; }
                }
                int iPointer = 0;
                angoObjectsForID = new NetworkGameObject[iValidSum];
                for(int i = 0; i < abValid.Length; i++)
                {
                    if (abValid[i]) 
                    {
                        angoObjectsForID[iPointer++] = angoTemp[i];
                    }
                }
            }
            if(iConsecutivePackets >= angoObjectsForID.Length)
            {
                angoObjectsForID = null;
                return false;
            }
            NetworkGameObject ngoCur = angoObjectsForID[iConsecutivePackets];
            asMessageParams = new string[2] { ngoCur.GetLocalID().ToString(), ngoCur.GetPrefabID().ToString() };
            iSent++;
            return true;
        }

        public override void Response(string sMessage)
        {
            string[] asSeperated = UseSeperator(sMessage);
            if (asSeperated.Length >= 3)
            {
                iReceived++;
                GetNetworkedGameObjectByLocalID(int.Parse(asSeperated[1])).SetNetworkID(int.Parse(asSeperated[2]));
                if(iReceived >= iSent)
                {
                    bNetworkIdAssignmentComplete = true;
                }
            }
            else
            {
                ErroniousPacket("parameter count mismatch", ErrorType.ServerResponse);
            }
        }
    }

    class TransformPacket : PacketResponse
    {
        public TransformPacket()
        {
            sHeader = "TransformData";
        }

        public override bool MessageSetup()
        {
            if(qtpdAll.Count == 0)
            {
                return false;
            }
            TransformPacketData tpdTemp = qtpdAll.Dequeue();
            asMessageParams = new string[8] { tpdTemp.iId.ToString(), tpdTemp.v3Pos.x.ToString(), tpdTemp.v3Pos.y.ToString(), tpdTemp.v3Pos.z.ToString(), tpdTemp.quatRot.x.ToString(), tpdTemp.quatRot.y.ToString(), tpdTemp.quatRot.z.ToString(), tpdTemp.quatRot.w.ToString() };
            return true;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            int iNetIdToCheck = int.Parse(asParams[1]);

            TransformPacketData tpdIn = new TransformPacketData();
            tpdIn.iId = iNetIdToCheck;
            tpdIn.v3Pos = new Vector3(float.Parse(asParams[2]), float.Parse(asParams[3]), float.Parse(asParams[4]));
            tpdIn.quatRot = new Quaternion(float.Parse(asParams[5]), float.Parse(asParams[6]), float.Parse(asParams[7]), float.Parse(asParams[8]));

            for (int i = 0; i < lngoAll.Count; i++)
            {
                if (lngoAll[i].GetNetworkID() == iNetIdToCheck)
                {
                    lngoAll[i].UpdateTransformData(tpdIn);
                    return;
                }
            }

            GetPrefabIDPacket.AddBufferedTransform(tpdIn);
        }
    }

    class PollGameObjectPacket : PacketResponse
    {
        public PollGameObjectPacket()
        {
            sHeader = "PollGameObjects";
        }
    }

    class GetPrefabIDPacket : PacketResponse
    {
        static List<TransformPacketData> qtpdBuffered = new List<TransformPacketData>();
        static Queue<int> iToSend = new Queue<int>();
        public GetPrefabIDPacket()
        {
            sHeader = "GetPrefabID";
        }

        public override bool MessageSetup()
        {
            if(iToSend.Count == 0)
            {
                return false;
            }
            asMessageParams = new string[1] { iToSend.Dequeue().ToString() };
            return true;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            int iNetId = int.Parse(asParams[1]);
            int iPref = int.Parse(asParams[2]);
            for(int i = 0; i < qtpdBuffered.Count; i++)
            {
                if (qtpdBuffered[i].iId == iNetId)
                {
                    CreateNetworkedGameObject(iPref, qtpdBuffered[i]);
                    qtpdBuffered.RemoveAt(i);
                    return;
                }
            }
        }
        
        public static int AddBufferedTransform(TransformPacketData tpd)
        {
            if(!iToSend.Contains(tpd.iId))
            {
                iToSend.Enqueue(tpd.iId);
                AddPacketTypeToQueue(SendPacket.GetPrefabID);
            }
            for(int i = 0; i < qtpdBuffered.Count; i++)
            {
                if (qtpdBuffered[i].iId == tpd.iId)
                {
                    qtpdBuffered[i] = tpd;
                    return iToSend.Count;
                }
            }
            qtpdBuffered.Add(tpd);
            return iToSend.Count;
        }
    }

    class ChatPacket : PacketResponse
    {
        static Queue<string> qsMessages = new Queue<string>();
        public ChatPacket()
        {
            sHeader = "Chat";
        }

        public override bool MessageSetup()
        {
            if(qsMessages.Count == 0)
            {
                return false;
            }
            asMessageParams = new string[1] { qsMessages.Dequeue() };
            return true;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            uim.NewChatMessage(asParams[1], asParams[2]);
        }

        public static int AddToQueue(string sMessage)
        {
            qsMessages.Enqueue(sMessage);
            return qsMessages.Count;
        }
    }

    class LoginPacket : PacketResponse
    {
        public static string sInputUsername = "";
        public static string sInputPassword = "";
        public LoginPacket()
        {
            sHeader = "Login";
        }

        public override bool MessageSetup()
        {
            asMessageParams = new string[2] {sInputUsername, sInputPassword};
            return iConsecutivePackets == 0;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            if (asParams[1] == "1")
            {
                uim.NewUI(UIManager.UIUsed.Gameplay);
                bCredentialsConfirmed = true;
            }
            else
            {
                uim.SendCredentialWarning(asParams[2]);
            }
        }
    }

    class RegisterPacket : PacketResponse
    {
        public static string sInputUsername = "";
        public static string sInputPassword = "";
        public RegisterPacket()
        {
            sHeader = "Register";
        }

        public override bool MessageSetup()
        {
            asMessageParams = new string[2] { sInputUsername, sInputPassword };
            return iConsecutivePackets == 0;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            if (asParams[1] == "1")
            {
                uim.NewUI(UIManager.UIUsed.Gameplay);
                bCredentialsConfirmed = true;
            }
            else
            {
                uim.SendCredentialWarning(asParams[2]);
            }
        }
    }

    class PingPacket : PacketResponse
    {
        public PingPacket()
        {
            sHeader = "Ping";
        }

        public override bool MessageSetup()
        {
            return iConsecutivePackets == 0;
        }
    }

    class StartGamePacket : PacketResponse
    {
        public StartGamePacket()
        {
            sHeader = "StartGame";
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            int iGamemode = int.Parse(asParams[2]);
            glThis.PrepGame(GetMap(int.Parse(asParams[1])), (GameLogic.Gamemode)iGamemode + 1, int.Parse(asParams[4]));
            uim.StartPreGame(float.Parse(asParams[3]), float.Parse(asParams[5]), (GameplayUI.UIModes)iGamemode + 1);
            uim.ResetPlayerStats(false);
        }
    }

    public class SelectWeaponPacket : PacketResponse
    {
        static Queue<(int, int)> qiiPacks = new Queue<(int, int)>();
        public SelectWeaponPacket()
        {
            sHeader = "SelectWeapon";
        }

        public override bool MessageSetup()
        {
            if(qiiPacks.Count > 0)
            {
                (int, int) iiThis = qiiPacks.Dequeue();
                asMessageParams = new string[2] { iiThis.Item1.ToString(), iiThis.Item2.ToString() };
                return true;
            }
            return false;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            bool bFirstSlot = asParams[1] == "0";
            if(bFirstSlot)
            {
                pmPlayer.transform.position = glThis.GetPlayerPosition();
                glThis.StartNewRound();
                pmPlayer.GetComponent<MeshRenderer>().enabled = true;
            }
            WeaponAttachment.WeaponType wtEThis = (WeaponAttachment.WeaponType)(int.Parse(asParams[2]) + (bFirstSlot ? 0 : System.Enum.GetValues(typeof(WeaponAttachment.WeaponType)).GetUpperBound(0) / 2));
            WeaponAttachment waPlayer = pmPlayer.GetComponent<WeaponAttachment>();
            waPlayer.SetWeapon(int.Parse(asParams[1]), wtEThis, int.Parse(asParams[3]));
            uim.NewUI(UIManager.UIUsed.Gameplay);
        }

        public static int Enqueue(int iSlot, int iWeapon)
        {
            qiiPacks.Enqueue((iSlot, iWeapon));
            AddPacketTypeToQueue(SendPacket.SelectWeapon);
            return qiiPacks.Count;
        }
    }

    public class GetStatsPacket : PacketResponse
    {
        public static bool bGlobal = true;
        NetworkGameObject[] angoPlayers = null;
        public GetStatsPacket()
        {
            sHeader = "GetStats";
        }

        public override bool MessageSetup()
        {
            if(angoPlayers == null)
            {
                angoPlayers = GetNetworkedGameObjectByPrefabID(0);
            }
            if (angoPlayers.Length <= iConsecutivePackets)
            {
                angoPlayers = null;
                return false;
            }
            else
            {
                asMessageParams = new string[2] { angoPlayers[iConsecutivePackets].GetNetworkID().ToString(), bGlobal ? "0" : "1" };
                return true;
            }
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            int[] aiVals = new int[5];
            for(int i = 0; i < aiVals.Length; i++)
            {
                aiVals[i] = int.Parse(asParams[i + 3]);
            }
            uim.AddPlayerStats(asParams[1], aiVals);
        }
    }

    public class FireWeaponPacket : PacketResponse
    {
        static Queue<FireWeaponInfo> qfwinfoAll = new Queue<FireWeaponInfo>();
        public struct FireWeaponInfo
        {
            public Vector3 v3Pos;
            public Vector3 v3Dir;
            public float fStopDist;
            public int iNetIdUser;
        }
        public FireWeaponPacket()
        {
            sHeader = "FireWeapon";
        }

        public override bool MessageSetup()
        {
            if (qfwinfoAll.Count > 0)
            {
                FireWeaponInfo fwinfo = qfwinfoAll.Dequeue();
                asMessageParams = new string[7 + ((fwinfo.iNetIdUser >= 0) ? 1 : 0)];
                asMessageParams[0] = fwinfo.v3Pos.x.ToString();
                asMessageParams[1] = fwinfo.v3Pos.y.ToString();
                asMessageParams[2] = fwinfo.v3Pos.z.ToString();
                asMessageParams[3] = fwinfo.v3Dir.x.ToString();
                asMessageParams[4] = fwinfo.v3Dir.y.ToString();
                asMessageParams[5] = fwinfo.v3Dir.z.ToString();
                asMessageParams[6] = fwinfo.fStopDist.ToString();
                if(asMessageParams.Length >= 8)
                {
                    asMessageParams[7] = fwinfo.iNetIdUser.ToString();
                }
                return true;
            }
            return false;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            uim.DrawLine(new Vector3(float.Parse(asParams[1]), float.Parse(asParams[2]), float.Parse(asParams[3])), new Vector3(float.Parse(asParams[4]), float.Parse(asParams[5]), float.Parse(asParams[6])), float.Parse(asParams[7]));
            if (int.TryParse(asParams[8], out int iNewAmmo))
            {
                uim.SetCurrentAmmo(iNewAmmo);
            }
        }

        public static int QueueUpPacket(Vector3 v3P, Vector3 v3D, float fDist, int aiId)
        {
            FireWeaponInfo fwinfo = new FireWeaponInfo();
            fwinfo.v3Pos = v3P;
            fwinfo.v3Dir = v3D;
            fwinfo.fStopDist = fDist;
            fwinfo.iNetIdUser = aiId;
            qfwinfoAll.Enqueue(fwinfo);
            return qfwinfoAll.Count;
        }
    }

    public class HealthUpdatePacket : PacketResponse
    {
        public HealthUpdatePacket()
        {
            sHeader = "HealthUpdate";
        }

        public override void Response(string sMessage)
        {
            uim.SetHealth(int.Parse(UseSeperator(sMessage)[1]));
        }
    }

    public class RoundStartPacket : PacketResponse
    {
        public RoundStartPacket()
        {
            sHeader = "RoundStart";
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            glThis.NewSpawnPoint(int.Parse(asParams[2]));
            glThis.SpectatorMode(false);
            uim.NewRound(int.Parse(asParams[1]));
            uim.HideDeath();
        }
    }

    public class GameEndPacket : PacketResponse
    {
        public GameEndPacket()
        {
            sHeader = "GameEnd";
        }

        public override void Response(string sMessage)
        {
            uim.EndGame(UseSeperator(sMessage)[1]);
            glThis.SpectatorMode(false);
            pmPlayer.transform.position = Vector3.zero;
            uim.HideDeath();
            uim.ResetPlayerStats(true);
        }
    }

    public class UsePickupPacket : PacketResponse
    {
        public static Queue<int> qiType = new Queue<int>();

        public UsePickupPacket()
        {
            sHeader = "UsePickup";
        }

        public override bool MessageSetup()
        {
            if(qiType.Count > 0)
            {
                asMessageParams = new string[1] { qiType.Dequeue().ToString() };
                return true;
            }
            return false;
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            int iStatNew;
            int iPickupType = int.Parse(asParams[2]);
            if (int.TryParse(asParams[3], out iStatNew))
            {
                glThis.ShowPickup(int.Parse(asParams[1]), -1);
                switch ((PickupLogic.PickupType)iPickupType)
                {
                    case PickupLogic.PickupType.Health:
                        uim.SetHealth(iStatNew);
                        break;
                    case PickupLogic.PickupType.Ammo:
                        uim.SetCurrentAmmo(iStatNew);
                        break;
                }
            }
            else
            {
                glThis.ShowPickup(int.Parse(asParams[1]), iPickupType);
            }
        }

        public static void Enqueue(int iType)
        {
            qiType.Enqueue(iType);
            AddPacketTypeToQueue(SendPacket.UsePickup);
        }
    }

    public class ExplosionPacket : PacketResponse
    {
        public struct ExplosionData
        {
            public int iExplosionType;
            public Vector3 v3Pos;
            public int iNetId;

            public ExplosionData(int iType, Vector3 v3ExplodePos, int iId)
            {
                v3Pos = v3ExplodePos;
                iExplosionType = iType;
                iNetId = iId;
            }
        }
        static Queue<ExplosionData> qed = new Queue<ExplosionData>();

        public ExplosionPacket()
        {
            sHeader = "Explosion";
        }

        public override bool MessageSetup()
        {
            if(qed.Count > 0)
            {
                ExplosionData edThis = qed.Dequeue();
                asMessageParams = new string[5] { edThis.v3Pos.x.ToString(), edThis.v3Pos.y.ToString(), edThis.v3Pos.z.ToString(), edThis.iExplosionType.ToString(), edThis.iNetId.ToString() };
                return true;
            }
            return false;
        }

        public static void Enqueue(ExplosionData ed)
        {
            qed.Enqueue(ed);
            AddPacketTypeToQueue(SendPacket.Explosion);
        }
    }

    public class DeathPacket : PacketResponse
    {
        public DeathPacket()
        {
            sHeader = "Death";
        }

        public override void Response(string sMessage)
        {
            glThis.SpectatorMode(true);
            pmPlayer.GetComponent<MeshRenderer>().enabled = false;
            uim.ShowDeath(UseSeperator(sMessage)[1]);
        }
    }

    public class SwapWeaponPacket : PacketResponse
    {
        public SwapWeaponPacket()
        {
            sHeader = "SwapWeapon";
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            NetworkGameObject ngoThis = GetNetworkedGameObjectByNetworkID(int.Parse(asParams[1]));
            if(ngoThis.IsLocallyOwned())
            {
                int[] aiAmmo = new int[2] { int.Parse(asParams[3]), int.Parse(asParams[4]) };
                ngoThis.GetComponent<WeaponAttachment>().SwapWeapon(int.Parse(asParams[2]), aiAmmo[0]);
                uim.SetMaxAmmo(aiAmmo[1]);
                uim.SetCurrentAmmo(aiAmmo[0]);
            }
            else
            {
                ngoThis.GetComponent<WeaponAttachment>().OnlyShowWeapon((WeaponAttachment.WeaponType)int.Parse(asParams[2]));
            }
        }
    }

    public class ReloadPacket : PacketResponse
    {
        public ReloadPacket()
        {
            sHeader = "Reload";
        }

        public override void Response(string sMessage)
        {
            EndReload();
            uim.SetMaxAmmo(int.Parse(UseSeperator(sMessage)[1]));
        }
    }

    public class RemoveObjectPacket : PacketResponse
    {
        public RemoveObjectPacket()
        {
            sHeader = "RemoveObject";
        }

        public override void Response(string sMessage)
        {
            RemoveNetworkedGameObject(GetNetworkedGameObjectByNetworkID(int.Parse(UseSeperator(sMessage)[1])));
        }
    }

    public class TeamScoreUpdatePacket : PacketResponse
    {
        public TeamScoreUpdatePacket()
        {
            sHeader = "TeamScoreUpdate";
        }

        public override void Response(string sMessage)
        {
            string[] asParams = UseSeperator(sMessage);
            uim.SetTeamScores(int.Parse(asParams[1]), int.Parse(asParams[2]));
        }
    }

    void InitalisePacketResponses()
    {
        apackresDict = new PacketResponse[23]
        {
            new ConnectionPacket(sockConnection, ipendpClient),
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
        qspE.Enqueue(SendPacket.PollGameObjects);
        qspE.Enqueue(SendPacket.GetStats);
    }

    private void Awake()
    {
        uim = GetComponent<UIManager>();
        glThis = GetComponent<GameLogic>();
        pmPlayer = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        if(apackresDict == null)
        {
            agoAssetPool = agoAssetPoolSetup;
            amlMaps = amlMapsSetup;
            agoAssetPoolSetup = null;
            TESTCAMERA = camTest;
            Screen.SetResolution(1000, 1000, false);
            Application.runInBackground = true;
            ReallocateBytes();
            ipendpClient = new IPEndPoint(new IPAddress(new byte[4] { 127, 0, 0, 1 }), 9143);
            endpSender = new IPEndPoint(IPAddress.Any, 0);
            sockConnection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockConnection.Blocking = false;
            sockConnection.Bind(endpSender);
            InitalisePacketResponses();
            spNextPacket = SendPacket.Connection;
            GetStatsPacket.bGlobal = true;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            bAllowDroppingPackets = !bAllowDroppingPackets;
            uim.DROP_PACKETS_TOGGLE();
        }
        if(sPacketSeperator != "" && bCredentialsConfirmed)
        {
            fTimeSinceLastPacket += Time.deltaTime;
            if(!bNetworkIdAssignmentComplete)
            {
                if(NIDRequestPacket.iSent == 0)
                {
                    spNextPacket = SendPacket.NIDRequest;
                }
                else
                {
                    spNextPacket = SendPacket.None;
                }
            }
            else if (qspE.Count > 0)
            {
                spNextPacket = qspE.Dequeue();
            }
            else if (qtpdAll.Count > 0)
            {
                spNextPacket = SendPacket.Transform;
            }
            else if (fTimeSinceLastPacket > fTimeoutCheck)
            {
                spNextPacket = SendPacket.Ping;
            }
        }
        if (spNextPacket != SendPacket.None)
        {
            SendPacketToServer(spNextPacket);
            spNextPacket = SendPacket.None;
        }
        try
        {
            ListenForMessage();
        }
        catch (SocketException ex)
        {
            if(ex.ErrorCode != 10035)
            {
                Debug.Log("Exception: " + ex.Message);
            }
        }

    }

    public static MapLogic GetMap(int iIndex)
    {
        return amlMaps[iIndex];
    }

    bool ListenForMessage()
    {
        ReallocateBytes();
        recv = sockConnection.ReceiveFrom(abData, ref endpSender);
        if (recv > 0 && (UnityEngine.Random.Range(0f, 1f) > fPacketDropChance || !bAllowDroppingPackets))
        {
            sPrevMessageReceived = Encoding.ASCII.GetString(abData, 0, abData.Length);
            Debug.Log(sPrevMessageReceived);
            PacketResponse packres = DeterminePacket(sPrevMessageReceived);
            if(packres != null)
            {
                packres.Response(sPrevMessageReceived);
            }
            return true;
        }
        else if(recv > 0)
        {
            uim.DROPPED_PACKET();
        }
        return false;
    }

    PacketResponse DeterminePacket(string sMessage)
    {
        for(int i = 0; i < apackresDict.Length; i++)
        {
            if (apackresDict[i].GetHeader() == sMessage.Substring(0, apackresDict[i].GetHeader().Length))
            {
                return apackresDict[i];
            }
        }
        return null;
    }

    void SendPacketToServer(SendPacket spEType)
    {
        apackresDict[(int)spEType].Reset();
        while (apackresDict[(int)spEType].MessageSetup())
        {
            SendMessageToServer(sockConnection, apackresDict[(int)spEType].CreateMessage());
        }
    }

    void ReallocateBytes()
    {
        abData = new byte[1024];
    }

    void SendMessageToServer(Socket socketVia, string message)
    {
        ReallocateBytes();
        abData = Encoding.ASCII.GetBytes(message);
        socketVia.Send(abData, abData.Length, SocketFlags.None);
    }

    public static void LoginAttempt(string sUsername, string sPassword)
    {
        if(sUsername == "CAMERA ADMIN")
        {
            pmPlayer.camPlayer.gameObject.SetActive(false);
            TESTCAMERA.gameObject.SetActive(true);
            uim.NewUI(UIManager.UIUsed.Gameplay);
        }
        LoginPacket.sInputUsername = sUsername;
        LoginPacket.sInputPassword = GenerateHash(sPassword);
        spNextPacket = SendPacket.Login;
        
    }

    public static void RegisterAccountAttempt(string sUsername, string sPassword)
    {
        RegisterPacket.sInputUsername = sUsername;
        RegisterPacket.sInputPassword = GenerateHash(sPassword);
        spNextPacket = SendPacket.Register;
    }

    public static bool TrySendChatMessage(string sMessage)
    {
        sMessage = RemoveSeperatorFromString(sMessage);
        if(sMessage.Length > 0)
        {
            qspE.Enqueue(SendPacket.Chat);
            ChatPacket.AddToQueue(sMessage);
            return true;
        }
        return false;
    }

    public static void StartReload()
    {
        qspE.Enqueue(SendPacket.Reload);
        uim.StartReload();
    }

    public static void EndReload()
    {
        uim.EndReload();
    }

    public static TransformPacketData QueueNewTransformData(int i, Vector3 v3, Quaternion quat)
    {
        TransformPacketData tpdOut = new TransformPacketData();
        tpdOut.iId = i;
        tpdOut.v3Pos = v3;
        tpdOut.quatRot = quat;
        return QueueNewTransformData(tpdOut);
    }

    public static TransformPacketData QueueNewTransformData(TransformPacketData tpdA)
    {
        qtpdAll.Enqueue(tpdA);
        return tpdA;
    }

    public static GameObject CreateNetworkedGameObject(GameObject goThis, TransformPacketData tpdInfo)
    {
        GameObject clone = Instantiate<GameObject>(goThis, tpdInfo.v3Pos, tpdInfo.quatRot);
        NetworkGameObject ngoThis = clone.GetComponent<NetworkGameObject>();
        ngoThis.SetNetworkID(tpdInfo.iId);
        ngoThis.UpdateTransformData(tpdInfo);
        lngoAll.Add(ngoThis);
        return clone;
    }

    public static GameObject CreateNetworkedGameObject(int iAssetId, TransformPacketData tpdInfo)
    {
        return CreateNetworkedGameObject(agoAssetPool[iAssetId], tpdInfo);
    }

    public static NetworkGameObject[] GetNetworkedGameObjectByPrefabID(int iPrefId)
    {
        List<NetworkGameObject> lngoThis = new List<NetworkGameObject>();
        for(int i = 0; i < lngoAll.Count; i++)
        {
            if (lngoAll[i].GetPrefabID() == iPrefId)
            {
                lngoThis.Add(lngoAll[i]);
            }
        }
        return lngoThis.ToArray();
    }

    public static NetworkGameObject GetNetworkedGameObjectByNetworkID(int iNetId)
    {
        for(int i = 0; i < lngoAll.Count; i++)
        {
            if (lngoAll[i].GetNetworkID() == iNetId)
            {
                return lngoAll[i];
            }
        }
        return null;
    }

    public static NetworkGameObject GetNetworkedGameObjectByLocalID(int iLocalId)
    {
        for(int i = 0; i < lngoAll.Count; i++)
        {
            if (lngoAll[i].GetLocalID() == iLocalId)
            {
                return lngoAll[i];
            }
        }
        return null;
    }

    public static void RemoveNetworkedGameObject(NetworkGameObject ngoToRemove)
    {
        Destroy(ngoToRemove.gameObject);
        lngoAll.Remove(ngoToRemove);
    }

    public static UIManager GetUIManager()
    {
        return uim;
    }

    public static void AddPacketTypeToQueue(SendPacket spE) { qspE.Enqueue(spE); }

    public Socket GetSocket() { return sockConnection; }

    private void OnApplicationQuit()
    {
        try
        {
            sockConnection.Close();
        }
        catch { }

    }

    static string RemoveSeperatorFromString(string sText)
    {
        string sOut = "";
        for(int i = 0; i < sText.Length - sPacketSeperator.Length + 1; i++)
        {
            if(sText.Substring(i, sPacketSeperator.Length) == sPacketSeperator)
            {
                i += sPacketSeperator.Length - 1;
            }
            else
            {
                sOut += sText[i];
            }
        }
        return sOut;
    }

    /*
     * NOTE : This method was used by me in 5035CMD
     */

    static string GenerateHash(string input)
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
