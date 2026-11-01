using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /// <summary>
    /// Client Sends: HEADER LOCALID PREFABID
    /// Server Sends: HEADER LOCALID NETWORKID
    /// The client asks for an NID for every object that gets instanced and needs to be represented to other clients
    /// clients will only send 1 'NIDRequestPacket' per networked game object (or object with the component 'NetworkGameObject')
    /// 
    /// clients provide a local ID for their object, so when they receive the network ID, they know to what object to attribute it to.
    /// Additionally, this local ID is stored in case a client sends multiple requests for the same object
    /// </summary>
    class NIDRequestPacket : Packet
    {
        int iIdUsed;
        public NIDRequestPacket()
        {
            sHeader = "NID Request";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            if (iRepetitionIndex == 0)
            {
                GameObject[] lgoAll = Server.GetAllGameObjects();
                string[] asParams = DeterminePacketParameters(sMessage);
                int[] aiParams = new int[asParams.Length];
                for (int i = 0; i < 2; i++)
                {
                    aiParams[i] = int.Parse(asParams[i]);
                }
                for (int i = 0; i < lgoAll.Length; i++)
                {
                    if (lgoAll[i].GetLocalID() == aiParams[0] && (Client)lgoAll[i] == cliSender)
                    {
                        return new Client[0];
                    }
                }
                iIdUsed = Server.GenerateNetworkID(aiParams[0], aiParams[1], cliSender);
            }
            return new Client[1] { cliSender };
        }

        protected override string PacketResponse(string sMessage)
        {
            return SimplePacket(sHeader, DeterminePacketParameters(sMessage)[0], iIdUsed.ToString());
        }
    }
}
