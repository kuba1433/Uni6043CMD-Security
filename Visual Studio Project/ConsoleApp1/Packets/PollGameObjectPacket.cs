using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// CLIENT SENDS: HEADER
    /// SERVER SENDS: TRANSFORM PACKETS REPEATED
    /// The client will use this packet to get the transform of objects in the scene when they spawn, otherwise objects won't become visible until they move, which would be troublesome for objects that may be stationary for extended periods of time
    /// 
    /// </summary>
    class PollGameObjectPacket : Packet
    {
        GameObject[] agoToSend;
        int iPointer = 0;
        public PollGameObjectPacket()
        {
            sHeader = "PollGameObjects";
        }

        protected override void NewMessage(string sMessage)
        {
            agoToSend = Server.GetAllGameObjects();
            iPointer = 0;
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            for(int i = iPointer; i < agoToSend.Length; i++)
            {
                iPointer++;
                if (!((Client)agoToSend[iPointer - 1]).Equals(cliReceiver))
                {
                    return true;
                }
            }
            return false;
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(Server.GetPacketObject(Server.PacketTypes.TransformData).GetHeader(), agoToSend[iRepetitionIndex].GetID().ToString(), ((Vector3)agoToSend[iRepetitionIndex]).X.ToString(), ((Vector3)agoToSend[iRepetitionIndex]).Y.ToString(), ((Vector3)agoToSend[iRepetitionIndex]).Z.ToString(), ((Vector4)agoToSend[iRepetitionIndex]).X.ToString(), ((Vector4)agoToSend[iRepetitionIndex]).Y.ToString(), ((Vector4)agoToSend[iRepetitionIndex]).Z.ToString(), ((Vector4)agoToSend[iRepetitionIndex]).W.ToString());
        }
    }
}
