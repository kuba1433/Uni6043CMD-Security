using UnityEngine;

/// <summary>
/// Does logic on whether a player has collected the pickup, theoretically the player doesn't need to be the collector, but the client as a whole gets attributed regardless
/// </summary>
public class PickupLogic : MonoBehaviour
{
    [HideInInspector] public Transform[] atransCanPickup;  // The objects that can pick this object up (colliders aren't used, which is why this is a transform)
    [SerializeField] GameObject[] goVisuals;  // All of the possible visuals that could be used to represent the many possible pickups at this pickup location
    [SerializeField, Min(0f)] float fRange = 5f;  // The distance at which any valid object needs to be within to have considered this pickup collected
    int iType = -1;
    bool bPickupable = false;
    public enum PickupType { Health, Ammo };

    private void Awake()
    {
        CanPickup(false);
    }

    private void Update()
    {
        if(bPickupable)
        {
            for (int i = 0; i < atransCanPickup.Length; i++)
            {
                if (Vector3.Distance(atransCanPickup[i].position, transform.position) < fRange)
                {
                    NetworkManager.UsePickupPacket.Enqueue(iType);
                    AppearPickup(-1);
                }
            }
        }
    }

    public void NewRound()
    {
        AppearPickup(-1);
    }

    public void AppearPickup(int iThisType)
    {
        iType = iThisType;
        CanPickup(iThisType >= 0);
    }

    public void CanPickup(bool bNew)
    {
        for(int i = 0; i <  goVisuals.Length; i++)
        {
            goVisuals[i].SetActive(iType == i);
        }
        bPickupable = bNew;
    }

    public void AddObjectThatCanPickup(Transform transNew)
    {
        Transform[] atransNew = new Transform[atransCanPickup.Length + 1];
        for(int i = 0; i < atransCanPickup.Length; i++)
        {
            atransNew[i] = atransCanPickup[i];
        }
        atransNew[atransNew.Length - 1] = transNew;
        atransCanPickup = atransNew;
    }
}
