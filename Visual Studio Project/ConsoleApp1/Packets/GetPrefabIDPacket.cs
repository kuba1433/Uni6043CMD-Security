using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS : HEADER NETWORKID
    /// SERVER SENDS : HEADER NETWORKID PREFABID
    /// When a client receives a new NID that they haven't seen, they need to instance the object and therefore need to know what prefab it should use.
    /// This occurs when a client receives 'TransformPacket', so the client in the meantime stores the 2 most recent packets (for dead-reckoning) to be applied to the object, once they know what it is.
    /// </summary>
    class GetPrefabIDPacket : Packet
    {
        int iNetId;
        int iPrefId;
        public GetPrefabIDPacket()
        {
            sHeader = "GetPrefabID";
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            if (iRepetitionIndex > 0)
            {
                return false;
            }
            GameObject[] agoAll = Server.GetAllGameObjects();
            iNetId = int.Parse(DeterminePacketParameters(sMessage)[0]);
            for (int i = 0; i < agoAll.Length; i++)
            {
                if (iNetId == agoAll[i].GetID())
                {
                    iPrefId = agoAll[i].GetPrefabID();
                    return true;
                }
            }
            return false;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, iNetId.ToString(), iPrefId.ToString());
        }
    }
}
