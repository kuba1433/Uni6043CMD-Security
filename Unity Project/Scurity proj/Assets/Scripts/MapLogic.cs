using UnityEngine;

/// <summary>
/// The information stored for each map
/// </summary>
public class MapLogic : MonoBehaviour
{
    [SerializeField] Transform goTeam1SpawnPoints;  // Transform, where all of the children's transforms are the spawn points for team 1
    [SerializeField] Transform goTeam2SpawnPoints;  // Transform, where all of the children's transforms are the spawn points for team 1
    [SerializeField] Transform transAllPickups;  // Transform, where all of the children's transforms are the pickup locations
    [SerializeField] Transform[] atransCanPickup;  // The list of transforms that can pick up a pickup, these transforms have no condition on the component, since it is expected that the client as a whole will be attributed to this, this list is transposed to all pickups
    PickupLogic[] aplAll;  // All pickups on this map (automatically populated)

    private void Awake()
    {
        aplAll = new PickupLogic[transAllPickups.childCount];
        for(int i = 0; i < transAllPickups.childCount; i++)
        {
            aplAll[i] = transAllPickups.GetChild(i).GetComponent<PickupLogic>();
            aplAll[i].atransCanPickup = atransCanPickup;
        }
    }

    /// <summary>
    /// Retrieves the position of a spawn point specified
    /// </summary>
    /// <param name="iSpawn">This index treats both team 1 spawn points and team 2 spawn points as if they were in 1 array</param>
    /// <returns>Position of the spawn point</returns>
    public Vector3 GetPositionOfSpawnPoint(int iSpawn)
    {
        if(goTeam1SpawnPoints.childCount <= iSpawn)
        {
            iSpawn -= goTeam1SpawnPoints.childCount;
            return goTeam2SpawnPoints.GetChild(iSpawn).position;
        }
        return goTeam1SpawnPoints.GetChild(iSpawn).position;
    }

    /// <summary>
    /// Coordinates the pickups on the map, at the start of a round pickups aren't there yet, so they need to disappear when a new round starts
    /// </summary>
    public void NewRound()
    {
        for(int i = 0; i < aplAll.Length; i++)
        {
            aplAll[i].NewRound();
        }
    }

    /// <summary>
    /// Used by 'GameLogic' to cause a pickup to do something (either appear or disappear)
    /// </summary>
    /// <param name="iIndex">The pickup's index in 'aplAll', this is kept consistent despite most system not knowing what the index means</param>
    /// <param name="iPickup">The pickup type that should be seen here (-1 means to force hide this pickup)</param>
    public void ShowPickup(int iIndex, int iPickup)
    {
        aplAll[iIndex].AppearPickup(iPickup);
    }
}
