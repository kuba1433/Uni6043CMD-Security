using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GroupTextEdit : MonoBehaviour
{
    [SerializeField] TMP_Text[] atmptextAll;
    [SerializeField] Image imgThis;

    public void SetField(int iIndex, string sNew)
    {
        atmptextAll[iIndex].text = sNew;
    }

    public void SetColour(Color colrNew)
    {
        imgThis.color = colrNew;
    }

    public int MaxSetIndex()
    {
        return atmptextAll.Length;
    }
}
