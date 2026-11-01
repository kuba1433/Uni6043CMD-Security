using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] TMP_Text[] tmptextChatSlots;
    [SerializeField] TMP_Text tmptextGameTime;
    [SerializeField] TMP_InputField tmpinputChatSend;
    [SerializeField] TMP_Text tmptextHealth;
    [SerializeField] TMP_Text tmptextAmmo;
    [SerializeField] TMP_Text[] atmptextWeaponSelection;
    [SerializeField] TMP_Text[] atmptextWeaponNames;
    [SerializeField] GameObject[] goFFAUI;
    [SerializeField] GameObject[] goTeamsUI;
    [SerializeField] GroupTextEdit[] agteLeaderboardElements;
    [SerializeField] GroupTextEdit[] agteTabMenuElements;
    [SerializeField] GameObject goTabMenu;
    [SerializeField] GameObject goReloadingGraphic;
    [SerializeField] GameObject goWinnerPanel;
    [SerializeField] TMP_Text tmptextWinnerName;
    [SerializeField] GameObject goDeathMessagePanel;
    [SerializeField] TMP_Text tmptextDeathMessage;
    [SerializeField] TMP_Text[] atmptextTeamScores;
    bool bCreatingChatMessage = false;
    int iCurAmmo = 1;
    int iMaxAmmo = 1;
    int iHealth = 100;
    float fGameTimeLeft = 0f;
    float fWinnerShowingTimeLeft = 0f;
    const float fWinnerShowingTimerStart = 10f;
    string[] asUsersRepresentedOnLeaderboard = new string[10];
    float fStatInfoAge = 0f;
    float fGetStatThreshold = 10f;
    [HideInInspector] public int iUsersOnLeaderboards = 0;
    Dictionary<string, int[]> dictsaiStats = new Dictionary<string, int[]>();
    float fGameRoundLength;
    public enum UIModes { Basic, FFA, Teams};

    private void Awake()
    {
        SetActiveReloadGraphic(false);
        goWinnerPanel.SetActive(false);
        HideDeathMessage();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            ToggleChatSend();
        }
        if(fGameTimeLeft > 0f)
        {
            fGameTimeLeft -= Time.deltaTime;
            SetTime(fGameTimeLeft);
        }
        if(fWinnerShowingTimeLeft > 0f)
        {
            fWinnerShowingTimeLeft -= Time.deltaTime;
            if(fWinnerShowingTimeLeft < 0f)
            {
                goWinnerPanel.SetActive(false);
            }
        }
        if(fStatInfoAge < fGetStatThreshold)
        {
            fStatInfoAge += Time.deltaTime;
            if(fGetStatThreshold < fStatInfoAge)
            {
                fStatInfoAge = 0f;
                NetworkManager.AddPacketTypeToQueue(NetworkManager.SendPacket.GetStats);
            }
        }
        goTabMenu.SetActive(Input.GetKey(KeyCode.Tab));
    }

    public void ResetResources()
    {
        SetHealth(100);
        SetMaxAmmo(10);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sUsername"></param>
    /// <param name="iStat"></param>
    /// <param name="iVal"></param>
    /// <returns>Whether a new dictionary element was created for this username</returns>
    public bool NewStats(string sUsername, int[] iVal)
    {
        fStatInfoAge = 0f;
        bool bAdded = true;

        if (dictsaiStats.ContainsKey(sUsername))
        {
            dictsaiStats[sUsername] = iVal;
            for(int i = 0; i < asUsersRepresentedOnLeaderboard.Length; i++)
            {
                if (asUsersRepresentedOnLeaderboard[i] == sUsername)
                {
                    UpdateAllLeaderboardAttributes(i);
                }
            }
            bAdded = false;
        }
        else
        {
            dictsaiStats.Add(sUsername, iVal);
            if(iUsersOnLeaderboards < asUsersRepresentedOnLeaderboard.Length)
            {
                asUsersRepresentedOnLeaderboard[iUsersOnLeaderboards] = sUsername;
                UpdateAllLeaderboardAttributes(iUsersOnLeaderboards);
                iUsersOnLeaderboards++;
            }
        }
        return bAdded;
    }

    public void ResetStats(bool bGlobalStat)
    {
        dictsaiStats = new Dictionary<string, int[]>();
        asUsersRepresentedOnLeaderboard = new string[10];
        iUsersOnLeaderboards = 0;
        fStatInfoAge = fGetStatThreshold - 1f;
        ResetAllLeaderboardAttributes();
        NetworkManager.GetStatsPacket.bGlobal = bGlobalStat;
    }

    public void SetupRoundTimer(float fTime)
    {
        fGameRoundLength = fTime;
        fGameTimeLeft = fTime;
    }

    public void SetupRoundTimer()
    {
        fGameTimeLeft = fGameRoundLength;
    }

    public string SetTime(float fTime)
    {
        string sOut = ((int)fTime).ToString();
        tmptextGameTime.text = sOut;
        return sOut;
    }

    public string SetHealth(int iNewHealth)
    {
        iHealth = iNewHealth;
        string sOut = iHealth.ToString();
        tmptextHealth.text = sOut;
        return sOut;
    }

    public string SetMaxAmmo(int iNewMaxAmmo)
    {
        iMaxAmmo = iNewMaxAmmo;
        string sOut = iNewMaxAmmo.ToString() + "/" + iNewMaxAmmo.ToString();
        iCurAmmo = iNewMaxAmmo;
        tmptextAmmo.text = sOut;
        return sOut;
    }

    public string SetCurrentAmmo(int iNewCurrentAmmo)
    {
        iCurAmmo = iNewCurrentAmmo;
        string sOut = iNewCurrentAmmo.ToString() + "/" + iMaxAmmo.ToString();
        tmptextAmmo.text = sOut;
        return sOut;
    }

    public bool DecrementAmmo()
    {
        if(iCurAmmo > 0)
        {
            SetCurrentAmmo(iCurAmmo - 1);
            return true;
        }
        return false;
    }

    public string PushChatMessage(string sMessage)
    {
        string sOut = "";
        for(int i = tmptextChatSlots.Length - 1; i >= 0; i--)
        {
            if(i == 0)
            {
                sOut = tmptextChatSlots[i].text;
                tmptextChatSlots[i].text = sMessage;
            }
            else
            {
                tmptextChatSlots[i].text = tmptextChatSlots[i - 1].text;
            }
        }
        return sOut;
    }

    public string PushChatMessage(string sOwner, string sMessage)
    {
        return PushChatMessage($"{sOwner} : {sMessage}");
    }

    public bool ToggleChatSend()
    {
        bCreatingChatMessage = !bCreatingChatMessage;
        if(bCreatingChatMessage && !tmpinputChatSend.isFocused)
        {
            tmpinputChatSend.Select();
            tmpinputChatSend.text = "";
        }
        else
        {
            NetworkManager.TrySendChatMessage(tmpinputChatSend.text);
        }
        return bCreatingChatMessage;
    }

    public int SetNewUIMode(UIModes uimE)
    {
        switch(uimE)
        {
            case UIModes.Basic:
                return MassSetActive(goTeamsUI, false) + MassSetActive(goFFAUI, false);
            case UIModes.FFA:
                return MassSetActive(goFFAUI, true);
            case UIModes.Teams:
                return MassSetActive(goTeamsUI, true);
        }
        return 0;
    }

    public int MassSetActive(GameObject[] agoThis, bool bNewActivity)
    {
        for(int i = 0; i < agoThis.Length; i++)
        {
            agoThis[i].SetActive(bNewActivity);
        }
        return agoThis.Length;
    }

    public void SetWeaponName(int iWeaponNum, string sWeaponName)
    {
        atmptextWeaponNames[iWeaponNum].text = sWeaponName;
    }

    public void WeaponSelected(int iSelected)
    {
        for(int i = 0; i < atmptextWeaponSelection.Length; i++)
        {
            atmptextWeaponSelection[i].text = (iSelected == i) ? "Selected" : "";
        }
    }

    void ResetAllLeaderboardAttributes()
    {
        for(int i = 0; i < agteLeaderboardElements.Length; i++)
        {
            for(int j = 0; j < agteLeaderboardElements[i].MaxSetIndex(); j++)
            {
                agteLeaderboardElements[i].SetField(j, "");
            }
        }
        for(int i = 0; i < agteTabMenuElements.Length; i++)
        {
            for(int j = 0; j < agteTabMenuElements[i].MaxSetIndex(); j++)
            {
                agteTabMenuElements[i].SetField(j, "");
            }
        }
    }

    void UpdateAllLeaderboardAttributes(int iIndex)
    {
        for(int i = 0; i < 5; i++)
        {
            UpdateLeaderboards(iIndex, i);
        }
    }

    void UpdateLeaderboards(int iIndex, int iAttrib)
    {
        string sText = "";
        if (iAttrib == 0)
        {
            sText = asUsersRepresentedOnLeaderboard[iIndex];
        }
        else if (iAttrib == 4)
        {
            if (dictsaiStats[asUsersRepresentedOnLeaderboard[iIndex]][iAttrib] == 0)
            {
                sText = "0";
            }
            else
            {
                    sText = ((float)dictsaiStats[asUsersRepresentedOnLeaderboard[iIndex]][iAttrib - 1] / (float)dictsaiStats[asUsersRepresentedOnLeaderboard[iIndex]][iAttrib] * 100f).ToString() + "%";
            }
        }
        else
        {
            sText = dictsaiStats[asUsersRepresentedOnLeaderboard[iIndex]][iAttrib - 1].ToString();
        }
        if(iAttrib < 3)
        {
            SetLeaderboardElement(iIndex, iAttrib, sText);
        }
        SetTabMenuElement(iIndex, iAttrib, sText);
    }

    public void SetLeaderboardElement(int iLeaderboardEntry, int iLeaderboardAttribute, string sText)
    {
        agteLeaderboardElements[iLeaderboardEntry].SetField(iLeaderboardAttribute, sText);
    }

    public void SetLeaderboardColour(int iLeaderboardEntry, Color colr)
    {
        agteLeaderboardElements[iLeaderboardEntry].SetColour(colr);
    }

    public void SetTabMenuElement(int iTabEntry, int iTabAttrib, string sText)
    {
        agteTabMenuElements[iTabEntry].SetField(iTabAttrib, sText);
    }

    public int MaxUsersStoredOnLeaderboards()
    {
        return asUsersRepresentedOnLeaderboard.Length;
    }

    public void SetActiveReloadGraphic(bool bActivity)
    {
        goReloadingGraphic.SetActive(bActivity);
    }

    public void ShowWinner(string sWinnerName)
    {
        fWinnerShowingTimeLeft = fWinnerShowingTimerStart;
        goWinnerPanel.SetActive(true);
        tmptextWinnerName.text = sWinnerName;
    }

    public void ShowDeathMessage(string sMessage)
    {
        goDeathMessagePanel.SetActive(true);
        tmptextDeathMessage.text = $"Killed by {sMessage}";
    }

    public void HideDeathMessage()
    {
        goDeathMessagePanel.SetActive(false);
    }

    public void SetTeamScore(int iTeam, int iScore)
    {
        atmptextTeamScores[iTeam].text = iScore.ToString();
    }
}
