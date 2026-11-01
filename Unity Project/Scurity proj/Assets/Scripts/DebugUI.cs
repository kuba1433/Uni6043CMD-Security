using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] TMP_Text tmptextCount;
    int iPacketsDropped = 0;
    public void DROPPED_PACKET()
    {
        iPacketsDropped++;
        tmptextCount.text = iPacketsDropped.ToString();
    }
}
