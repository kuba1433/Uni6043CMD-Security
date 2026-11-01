using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Vector3 v3MoveMult = Vector3.one;
    [SerializeField] public Camera camPlayer;
    float fHeadYaw = 0f;
    [SerializeField] bool bLocked = false;
    [SerializeField] float fMaxHeadAngle = 80f;

    private void Update()
    {
        if(!bLocked)
        {
            transform.eulerAngles += Vector3.up * Input.mousePositionDelta.x;
            fHeadYaw -= Input.mousePositionDelta.y;
            camPlayer.transform.localEulerAngles = Vector3.right * fHeadYaw;
            //camPlayer.transform.eulerAngles = new Vector3(camPlayer.transform.eulerAngles.x, Mathf.Clamp(camPlayer.transform.eulerAngles.x, -fMaxHeadAngle, fMaxHeadAngle), camPlayer.transform.eulerAngles.z);
            Vector3 v3MoveForce = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) v3MoveForce += transform.forward * v3MoveMult.z;
            if (Input.GetKey(KeyCode.S)) v3MoveForce -= transform.forward * v3MoveMult.z;
            if (Input.GetKey(KeyCode.D)) v3MoveForce += transform.right * v3MoveMult.x;
            if (Input.GetKey(KeyCode.A)) v3MoveForce -= transform.right * v3MoveMult.x;
            if (Input.GetKeyDown(KeyCode.Space)) v3MoveForce += transform.up * v3MoveMult.y;
            GetComponent<Rigidbody>().AddForce(v3MoveForce * Time.deltaTime, ForceMode.Impulse);
            
        }
        if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public bool SetActiveCamera(bool bNew)
    {
        bool bCur = camPlayer.enabled;
        camPlayer.enabled = bNew;
        return bCur ^ bNew;
    }

    public bool LockState(bool bNew)
    {
        bool bOut = bLocked;
        bLocked = bNew;
        return bOut;
    }
}
