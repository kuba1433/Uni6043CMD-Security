using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Packets
{
    /* Use Pickup
     * CLIENT SENDS : HEADER PICKUPINDEX
     * SERVER SENDS : HEADER PICKUPINDEX PICKUPTYPE RELEVANTSTAT
     * Client sends when they collect a pickup and expects a response from the server to confirm that it has been affective or deny for some reason
     * Server sends -1 for 'PICKUPTYPE' when the specific pickup location should be empty,
     * When the client picks up a pickup, the 'PICKUPTYPE' is the type of stat editted and it is implied that the pickup should be now none (implied by the existence of RELEVANTSTAT)
     * if RELEVANTSTAT isn't a part of the packet, then PICKUPTYPE dictates the type of pickup that should appear at that pickup location
     * 
     * server sends relevant information that would change as a result
     */
    class UsePickupPacket : Packet
    {
        int iRelevantInfo = 0;
        int iRelevantType = -1;
        Client cliPicker;
        bool bSendingToPicker = false;
        public UsePickupPacket()
        {
            sHeader = "UsePickup";
        }

        protected override Client[] GetReceivers(string sMessage, Client cliSender)
        {
            Game.PickupType ptEThis;
            if (cliSender == null)
            {
                iRelevantType = -1;
                return (Client[])Server.GetCurrentGame();
            }
            cliPicker = cliSender;
            ptEThis = Server.GetCurrentGame().UsePickup(int.Parse(DeterminePacketParameters(sMessage)[0]));
            if (ptEThis == Game.PickupType.None)
            {
                return new Client[1] { cliSender };
            }
            if(iRepetitionIndex == 0)
            {
                iRelevantInfo = cliSender.UsePickup(ptEThis);
                iRelevantType = (int)ptEThis;
            }
            return (Client[])Server.GetCurrentGame();
        }

        protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
        {
            if (cliPicker == null)
            {
                bSendingToPicker = false;
            }
            else
            {
                bSendingToPicker = cliPicker.Equals(cliReceiver);
            }
            return iRepetitionIndex == 0;
        }

        protected override string PacketResponse(string sMessage)
        {
            string sIndex = DeterminePacketParameters(sMessage)[0];
            if(iRelevantType < 0 || !bSendingToPicker)
            {
                return SimplePacket(sHeader, sIndex, ((int)Server.GetCurrentGame().GetPickupType(int.Parse(sIndex))).ToString());
            }
            return SimplePacket(sHeader, sIndex, iRelevantType.ToString(), iRelevantInfo.ToString());
        }
    }
}
