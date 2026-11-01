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
    /// Client Sends: HEADER NETWORKID POSX POSY POSZ ROTX ROTY ROTZ ROTW
    /// Server Sends: HEADER NETWORKID POSX POSY POSZ ROTX ROTY ROTZ ROTW
    /// The client will update the server with their 'Vector3' position and 'Quaternion' rotation, allowing the server to build a mental model of the clients in the game.
    /// The server relays this information to all other clients and the clients can do dead-reckoning with the packets they've received
    /// 
    /// The client ONLY sends when either position and rotation change, so no problems could occur with unecessary packets, the player also queues up many transforms and only sends the most recent one, since outdated information is useless by then
    /// </summary>
    class TransformPacket : Packet
    {
        bool bSend = false;
        public TransformPacket()
        {
            sHeader = "TransformData";
        }

        protected override void NewMessage(string sMessage)
        {
            string[] asParams = DeterminePacketParameters(sMessage);
            if (asParams.Length >= 8)
            {
                bSend = !Server.NewGameObjectTransform(int.Parse(asParams[0]), new Vector3(float.Parse(asParams[1]), float.Parse(asParams[2]), float.Parse(asParams[3])), new Vector4(float.Parse(asParams[4]), float.Parse(asParams[5]), float.Parse(asParams[6]), float.Parse(asParams[7])));
            }
            else
            {
                bSend = false;
            }
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            return bSend && iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            return sMessage;
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            Client[] acliAll = Server.GetAllLoggedInClients();
            if (acliAll.Length != 0)
            {
                Client[] acliOut = new Client[acliAll.Length - 1];
                int iPointer = 0;
                for (int i = 0; i < acliAll.Length; i++)
                {
                    if (!cliSender.Equals(acliAll[i]))
                    {
                        acliOut[iPointer] = acliAll[i];
                        iPointer++;
                    }
                }
                return acliOut;
            }
            return new Client[0];
        }
    }
}
