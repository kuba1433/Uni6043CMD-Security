using UnityEngine;

public class NetworkGameObject : MonoBehaviour
{
    [SerializeField] bool bLocallyOwned = false;
    [SerializeField] int iNetworkId = -1;
    [SerializeField] int iLocalId = 0;
    [SerializeField] int iPrefUsed = 0;
    static int iAssignedIds = 0;
    NetworkManager.TransformPacketData tpdCur = new NetworkManager.TransformPacketData();
    NetworkManager.TransformPacketData tpdPrev = new NetworkManager.TransformPacketData();
    float fCurPacketTime = 0f;
    float fPrevPacketTime = 0f;

    private void Awake()
    {
        iLocalId = iAssignedIds++;
    }

    private void Update()
    {
        if (bLocallyOwned)
        {
            UpdateTransformAsTransformData();
        }
        else
        {
            DetermineTransformNowViaPackets();
        }
        TryPushTransformData();
    }

    /// <summary>
    /// This system determines the position of the GameObject based on its previous 2 transform packets and extrapolates from the 2 packet's transforms onwards
    /// </summary>
    void DetermineTransformNowViaPackets()
    {
        float fDeltaRatio = (Time.time - fCurPacketTime) / (fCurPacketTime - fPrevPacketTime) + 1f;
        try
        {
            transform.position = Vector3.Lerp(tpdPrev.v3Pos, tpdCur.v3Pos, fDeltaRatio);
            transform.rotation = Quaternion.Lerp(tpdPrev.quatRot, tpdCur.quatRot, fDeltaRatio);
        }
        catch { }
    }

    /// <summary>
    /// Attempts to send current transform as a transform packet, it won't if the 'NetworkGameObject' could cause problems
    /// </summary>
    /// <returns>Whether the transform packet is queued up</returns>
    public bool TryPushTransformData()
    {
        if(!tpdCur.Equals(tpdPrev) && iNetworkId != -1 && bLocallyOwned)
        {
            NetworkManager.QueueNewTransformData(tpdCur);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public NetworkManager.TransformPacketData UpdateTransformAsTransformData()
    {
        NetworkManager.TransformPacketData tpdThis = new NetworkManager.TransformPacketData();
        tpdThis.iId = iNetworkId;
        tpdThis.v3Pos = transform.position;
        tpdThis.quatRot = transform.rotation;
        return UpdateTransformData(tpdThis);
    }

    /// <summary>
    /// Queues up a transform data packet as a new current packet and pushes the previous current packet as the new previous data packet
    /// </summary>
    /// <param name="tpdNew">The packet data to queue up</param>
    /// <returns>Returns the previous previous packet</returns>
    public NetworkManager.TransformPacketData UpdateTransformData(NetworkManager.TransformPacketData tpdNew)
    {
        NetworkManager.TransformPacketData tpdOut = tpdPrev;
        tpdPrev = tpdCur;
        tpdCur = tpdNew;
        fPrevPacketTime = fCurPacketTime;
        fCurPacketTime = Time.time;
        return tpdOut;
    }

    public int SetNetworkID(int iNew)
    {
        int iTemp = iNetworkId;
        iNetworkId = iNew;
        return iTemp;
    }

    public int GetPrefabID() { return iPrefUsed; }
    public int GetNetworkID() { return iNetworkId; }
    public int GetLocalID() { return iLocalId; }
    public bool IsLocallyOwned() { return bLocallyOwned; }
    public bool ValidNetworkId() { return iNetworkId >= 0; }
}
