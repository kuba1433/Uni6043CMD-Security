using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutUI : MonoBehaviour
{
    [SerializeField] TMP_Text tmptextTimer;
    float fCurTime = 0f;
    [SerializeField] Image[] aimgPrimary;
    [SerializeField] Image[] aimgSecondary;
    [SerializeField] Color colrUnselected;
    [SerializeField] Color colrSelected;
    PrimaryWeapons pwESelected = PrimaryWeapons.None;
    SecondaryWeapons swESelected = SecondaryWeapons.None;

    public enum PrimaryWeapons { None = -1, AssaultRifle, Shotgun, SniperRifle, SubmachineGun};
    public enum SecondaryWeapons { None = -1, Pistol, Revolver, RocketLauncher, GrenadeLauncher};

    private void Awake()
    {
        ResetSelected();
    }

    private void Update()
    {
        if(fCurTime > 0f)
        {
            fCurTime -= Time.deltaTime;
            VisualTime(fCurTime);
        }
    }

    public void ResetSelected()
    {
        SelectNewPrimary((int)PrimaryWeapons.None, false);
        SelectNewSecondary((int)SecondaryWeapons.None, false);
    }

    void SelectNewPanel(Image[] aimgRelevant, int iPrev, int iNew)
    {
        if(iPrev >= 0)
        {
            aimgRelevant[iPrev].color = colrUnselected;
        }
        if (iNew >= 0)
        {
            aimgRelevant[iNew].color = colrSelected;
        }
    }

    public void SelectNewPrimary(int iNew, bool bSendPacket)
    {
        if(bSendPacket)
        {
            NetworkManager.SelectWeaponPacket.Enqueue(0, iNew);
        }
        SelectNewPanel(aimgPrimary, (int)pwESelected, iNew);
        pwESelected = (PrimaryWeapons)iNew;
    }

    public void SelectNewPrimary(int iNew)
    {
        SelectNewPrimary(iNew, true);
    }

    public void SelectNewSecondary(int iNew, bool bSendPacket)
    {
        if(bSendPacket)
        {
            NetworkManager.SelectWeaponPacket.Enqueue(1, iNew);
        }
        SelectNewPanel(aimgSecondary, (int)swESelected, iNew);
        swESelected = (SecondaryWeapons)iNew;
    }

    public void SelectNewSecondary(int iNew)
    {
        SelectNewSecondary(iNew, true);
    }

    public float NewTimer(float fNew)
    {
        float fOut = fCurTime;
        fCurTime = fNew;
        VisualTime(fNew);
        return fOut;
    }

    int VisualTime(float fTime)
    {
        int iOut = (int)fTime;
        if (!Mathf.Approximately(iOut, fTime))
        {
            iOut++;
        }
        tmptextTimer.text = iOut.ToString();
        return iOut;
    }
}
