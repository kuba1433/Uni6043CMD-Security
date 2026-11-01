using TMPro;
using UnityEngine;

public class CredentialsUI : MonoBehaviour
{
    [System.Serializable]
    public struct InputCredentials
    {
        [SerializeField] public NetworkManager.SendPacket spE;
        [SerializeField] public TMP_InputField tmpinputUsername;
        [SerializeField] public TMP_InputField tmpinputPassword;
    }

    [SerializeField] InputCredentials[] aicAll;
    [SerializeField] GameObject goWarningPanel;
    [SerializeField] TMP_Text tmptextWarning;
    [SerializeField, Min(0f)] float fWarningTime = 5f;
    float fWarningTimer = -1f;

    private void Update()
    {
        if(fWarningTimer > 0f)
        {
            fWarningTimer -= Time.deltaTime;
            if(fWarningTimer <= 0f)
            {
                goWarningPanel.SetActive(false);
            }
        }
    }

    public void SendInformation(int iInputCredUsed)
    {
        InputCredentials icUsed = aicAll[iInputCredUsed];
        switch (icUsed.spE)
        {
            case NetworkManager.SendPacket.Login:
                NetworkManager.LoginAttempt(icUsed.tmpinputUsername.text, icUsed.tmpinputPassword.text);
                break;
            case NetworkManager.SendPacket.Register:
                NetworkManager.RegisterAccountAttempt(icUsed.tmpinputUsername.text, icUsed.tmpinputPassword.text);
                break;
        }
    }

    public void ShowWarning(string sText)
    {
        tmptextWarning.text = sText;
        goWarningPanel.SetActive(true);
        fWarningTimer = fWarningTime;
    }
}
