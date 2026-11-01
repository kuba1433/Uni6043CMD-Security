using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /*
     * A packet is any information transferred between server and client
     * 
     * The format allows for lots of kinds of packets, ones that relay information from 1 user to all, ones that only receive information and ones that need to bulk send packets
     * This is, however a bulky way of doing this, a more efficient form would be using many interfaces to have packets function on their own, while being kept lightweight, in this system 'PacketLoop' exists in all instances of 'Packet'
     * another form could be an expressly static one, where there is a static 'PacketManager' and that dictates how all packet types function, possibly paired with the interfaces method
     * 
     * To expand this implementation, more features could be added, because if you look across many 'Packet' type objects, you can see lots of very similar things going on with groups of them, such as many of them store the sender for later use
     * maybe an interface could dictate what the packet could store in addition to these base attributes each packet has
     */
    internal abstract class Packet
    {
        protected string sHeader;  // The header for this specific packet
        protected int iRepetitionIndex = 0;  // The amount of times this packet has ran 'GetReceivers' - 1 (helpful to iterate packets being sent, since under this system it can only send 1 packet at a time, it can't bulk send them)
        public string GetHeader() { return sHeader; }

        /// <summary>
        /// Basic function used to create packet strings in a consistant manner, used by practically every packet
        /// </summary>
        public static string SimplePacket(string sHeaderText, params string[] asPacketParams)
        {
            string sOut = sHeaderText + Server.sSeperator;
            for (int i = 0; i < asPacketParams.Length; i++)
            {
                sOut += $"{asPacketParams[i]}{Server.sSeperator}";
            }
            return sOut;
        }

        /// <summary>
        /// The loop every 'Packet' object goes through
        /// Runs 'NewMessage' once
        /// Runs 'GetReceivers' until the output of 'PacketSendCheck' for each client is false
        /// Runs 'PacketSendCheck' for the total amount of clients 'GetReceivers' outputs across possibly multiple iterations
        /// Runs 'PacketResponse' every time 'PacketSendCheck' returns true
        /// </summary>
        public void PacketLoop(string sMessage, Client cliSender)
        {
            iRepetitionIndex = 0;
            NewMessage(sMessage);
            if (cliSender != null)
            {
                cliSender.NewPacket();
            }
            bool bLoop = true;
            while (bLoop)
            {
                Client[] acliReceivers = GetReceivers(sMessage, cliSender);
                bLoop = false;
                for (int i = 0; i < acliReceivers.Length; i++)
                {
                    if (PacketSendCheck(sMessage, acliReceivers[i]))
                    {
                        bLoop = true;
                        string sSending = PacketResponse(sMessage);
                        Server.SendMessage(acliReceivers[i], sSending);
                        if(sHeader != "TransformData" || !Server.bHideTransformPackets) Console.WriteLine(sSending);
                    }
                }
                iRepetitionIndex++;
            }
        }

        /// <summary>
        /// This is used to possibly instantiate information in a way such that it is guarenteed to occur once per packet
        /// </summary>
        protected virtual void NewMessage(string sMessage) { }

        /// <summary>
        /// returns a string given the message and having just ran 'PacketSendCheck' for the client sender
        /// </summary>
        /// <returns>This string is directly sent to the user, so use 'SimplePacket' to keep it consistant</returns>
        protected virtual string PacketResponse(string sMessage) { return sHeader; }

        /// <summary>
        /// Used to determine if thie specific recipient should receive a message as a response to this message sent by a sender (NOTE: sender and recipient in this context could be the same client)
        /// </summary>
        protected virtual bool PacketSendCheck(string sMessage, Client cliReceiver) { return iRepetitionIndex == 0; }

        /// <summary>
        /// Used to retrieve all clients that should be checked for whether they need to receive a response to this packet sent by this client
        /// </summary>
        protected virtual Client[] GetReceivers(string sMessage, Client cliSender) { return new Client[1] { cliSender }; }

        /// <summary>
        /// Used quite often on 'sMessage' in many contexts to split the parameters and removing the header
        /// </summary>
        /// <returns>array of strings containing the parameters in order they appear in the entire packet (header is omitted)</returns>
        public static string[] DeterminePacketParameters(string sMessage)
        {
            List<string> lsOut = new List<string>();
            string sTemp = "";
            for (int i = 0; i < sMessage.Length; i++)
            {
                if (sMessage.Substring(i, Server.sSeperator.Length) == Server.sSeperator)
                {
                    if (sTemp.Length != i)
                    {
                        lsOut.Add(sTemp);
                    }
                    sTemp = "";
                }
                else
                {
                    sTemp += sMessage[i];
                }
            }
            lsOut.Add(sTemp);
            return lsOut.ToArray();
        }
    }
}
