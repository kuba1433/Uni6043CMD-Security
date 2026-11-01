using UnityEngine;
using UnityEngine.Rendering.LookDev;

public class UIManager : MonoBehaviour
{
    public enum UIUsed { Credentials, Gameplay, LoadoutSelect, DEBUG};
    [SerializeField] CredentialsUI credui;
    [SerializeField] GameplayUI gameui;
    [SerializeField] LoadoutUI loadui;
    [SerializeField] DebugUI debugui;
    [SerializeField] UIUsed uiuECur = UIUsed.Credentials;
    [SerializeField] PlayerMovement pmPlayer;
    [SerializeField] LineDrawer ldThis;

    private void Awake()
    {
        ResetUI();
        Cursor.lockState = CursorLockMode.None;
        gameui.SetNewUIMode(GameplayUI.UIModes.Basic);
    }

    public int ResetUI()
    {
        int iOut = 0;
        for(int i = 0; i < System.Enum.GetValues(typeof(UIUsed)).Length; i++)
        {
            SetActiveUI((UIUsed)i, false);
            iOut++;
        }
        NewUI(uiuECur, true);
        return iOut;
    }

    public void SendCredentialWarning(string sMessage)
    {
        if(uiuECur == UIUsed.Credentials)
        {
            credui.ShowWarning(sMessage);
        }
    }

    public string NewChatMessage(string sOwner, string sMessage)
    {
        string sOut = "";
        if(uiuECur == UIUsed.Gameplay)
        {
            sOut = gameui.PushChatMessage(sOwner, sMessage);
        }
        return sOut;
    }

    public void StartPreGame(float fLoadoutTime, float fRoundTimer, GameplayUI.UIModes uimE)
    {
        NewUI(UIUsed.LoadoutSelect);
        loadui.ResetSelected();
        loadui.NewTimer(fLoadoutTime);
        gameui.SetupRoundTimer(fRoundTimer);
        gameui.SetNewUIMode(uimE);
    }

    public void NewRound(float fLoadoutTime)
    {
        NewUI(UIUsed.LoadoutSelect);
        loadui.ResetSelected();
        loadui.NewTimer(fLoadoutTime);
        pmPlayer.GetComponent<WeaponAttachment>().ResetWeapons();
        gameui.SetupRoundTimer();
    }

    public void NewUI(UIUsed uiuE, bool bForce = false)
    {
        if(uiuECur != uiuE || bForce)
        {
            Cursor.lockState = (uiuE == UIUsed.Gameplay) ? CursorLockMode.Locked : CursorLockMode.None;
            pmPlayer.LockState(uiuE != UIUsed.Gameplay);
            SetActiveUI(uiuECur, false);
            SetActiveUI(uiuE, true);
            uiuECur = uiuE;
        }
    }

    void SetActiveUI(UIUsed uiuE, bool bNew)
    {
        switch(uiuE)
        {
            case UIUsed.Credentials:
                credui.gameObject.SetActive(bNew);
                break;
            case UIUsed.Gameplay:
                gameui.gameObject.SetActive(bNew);
                Cursor.lockState = CursorLockMode.Locked;
                gameui.ResetResources();
                break;
            case UIUsed.LoadoutSelect:
                loadui.gameObject.SetActive(bNew);
                Cursor.lockState = CursorLockMode.None;
                break;
            case UIUsed.DEBUG:
                debugui.gameObject.SetActive(bNew);
                break;
        }
    }

    public bool AddPlayerStats(string sUsername, int[] aiVal)
    {
        return gameui.NewStats(sUsername, aiVal);
    }

    public void ResetPlayerStats(bool bGlobal)
    {
        gameui.ResetStats(bGlobal);
    }

    public (int, int) GetPlayersRepresentedOnLeaderboard()
    {
        return (gameui.iUsersOnLeaderboards, gameui.MaxUsersStoredOnLeaderboards());
    }

    public void SetCurrentAmmo(int iNewCurAmmo)
    {
        gameui.SetCurrentAmmo(iNewCurAmmo);
    }

    public void SetMaxAmmo(int iNewMaxAmmo)
    {
        gameui.SetMaxAmmo(iNewMaxAmmo);
    }

    public void DrawLine(Vector3 v3Start, Vector3 v3Dir, float fDist)
    {
        ldThis.DrawNewLine(v3Start, v3Dir, fDist);
    }

    public void SetHealth(int iNewHealth)
    {
        gameui.SetHealth(iNewHealth);
    }

    public void StartReload()
    {
        gameui.SetActiveReloadGraphic(true);
    }

    public void EndReload()
    {
        gameui.SetActiveReloadGraphic(false);
    }

    public void EndGame(string sWinnerName)
    {
        gameui.ShowWinner(sWinnerName);
        gameui.SetNewUIMode(GameplayUI.UIModes.Basic);
    }

    public void ShowDeath(string sDeathMessage)
    {
        gameui.ShowDeathMessage(sDeathMessage);
    }
    
    public void HideDeath()
    {
        gameui.HideDeathMessage();
    }

    public void SetTeamScores(int iTeam1Score, int iTeam2Score)
    {
        gameui.SetTeamScore(0, iTeam1Score);
        gameui.SetTeamScore(1, iTeam2Score);
    }

    public void DROP_PACKETS_TOGGLE()
    {
        NewUI(UIUsed.DEBUG);
    }

    public void DROPPED_PACKET()
    {
        debugui.DROPPED_PACKET();
    }
}
