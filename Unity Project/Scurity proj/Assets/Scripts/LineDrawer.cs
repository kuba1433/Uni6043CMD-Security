using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    [SerializeField, Min(0f)] float fLifetime = 5f;
    [SerializeField] float fWidth = 1f;
    [SerializeField] Color colr = Color.red;
    [SerializeField] Material matBase;
    List<LineLifetime> lll = new List<LineLifetime>();
    int iCount = 0;
    public class LineLifetime
    {
        public LineRenderer lrThis;
        public float fTime;
        public LineLifetime(LineRenderer lr, float fLifetime)
        {
            fTime = fLifetime;
            lrThis = lr;
        }
        public bool Update(float fDeltaTime)
        {
            fTime -= fDeltaTime;
            if(fTime <= 0f)
            {
                Destroy(lrThis.gameObject);
                return true;
            }
            return false;
        }
    }

    private void Update()
    {
        for(int i = lll.Count - 1; i >= 0; i--)
        {
            if(lll[i].Update(Time.deltaTime))
            {
                lll.RemoveAt(i);
            }
        }
    }

    LineRenderer CreateLineObject()
    {
        GameObject goThis = new GameObject($"Line{iCount}", typeof(LineRenderer));
        goThis.transform.parent = transform;
        iCount++;
        LineRenderer lrOut = goThis.GetComponent<LineRenderer>();
        lrOut.material = matBase;
        lrOut.startColor = colr;
        lrOut.endColor = colr;
        lrOut.startWidth = fWidth;
        lrOut.endWidth = fWidth;
        lll.Add(new LineLifetime(lrOut, fLifetime));
        return lrOut;
    }

    public void DrawNewLine(Vector3 v3Start, Vector3 v3Dir, float fLength)
    {
        LineRenderer lr = CreateLineObject();
        lr.SetPositions(new Vector3[2] { v3Start, v3Start + v3Dir * fLength });
    }
}
