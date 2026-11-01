using UnityEngine;

public class GameLogic : MonoBehaviour
{
    MapLogic mlMap;
    Gamemode gmEUsed;
    int iSetSpawnPoint;
    [SerializeField] SpectatorCameraPositioning scpThis;
    [SerializeField] GameObject goOriginalCamera;
    public enum Gamemode { FFA, Teams};

    private void Awake()
    {
        SpectatorMode(false);
    }

    public void PrepGame(MapLogic mlNew, Gamemode gmENew, int iSpawn)
    {
        mlMap = mlNew;
        gmEUsed = gmENew;
        NewSpawnPoint(iSpawn);
        scpThis.Move(mlMap.transform.position + Vector3.up * 100f, Quaternion.Euler(90f, 0f, 0f));
    }

    public Vector3 GetPlayerPosition()
    {
        return mlMap.GetPositionOfSpawnPoint(iSetSpawnPoint);
    }

    public void NewSpawnPoint(int iNewSpawn)
    {
        iSetSpawnPoint = iNewSpawn;
    }

    public void SpectatorMode(bool bSpectatorMode)
    {
        if(bSpectatorMode)
        {
            goOriginalCamera.SetActive(false);
            scpThis.gameObject.SetActive(true);
        }
        else
        {
            goOriginalCamera.SetActive(true);
            scpThis.gameObject.SetActive(false);
        }
    }

    public void StartNewRound()
    {
        mlMap.NewRound();
    }

    public void ShowPickup(int iIndex, int iPickupType)
    {
        mlMap.ShowPickup(iIndex, iPickupType);
    }

    public Gamemode GetGamemode() { return gmEUsed; }
}
