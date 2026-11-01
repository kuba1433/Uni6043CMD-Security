using UnityEngine;

public class SpectatorCameraPositioning : MonoBehaviour
{
    public void Move(Vector3 v3Pos, Quaternion quatRot)
    {
        transform.position = v3Pos;
        transform.rotation = quatRot;
    }
}
