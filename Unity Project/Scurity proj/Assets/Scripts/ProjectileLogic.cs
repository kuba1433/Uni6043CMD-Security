using UnityEngine;

/// <summary>
/// This stores all information that a projectile fired from a weapon should do
/// In my game all projectiles explode, however theoretically another condition could be implemented not requiring 'BasicInformation' or another override that has differing projectile types
/// </summary>
public class ProjectileLogic : MonoBehaviour
{
    int iType;  // The type of explosion sent to the server when it explodes
    float fFuse = 0f;  // The timer the projectile has before it disappears (not required, but expected (to stop infinite projectiles))
    bool bFuseActive = false;  // Whether the fuse should tick down (this stops the projectile from exploding after creating, but before 'BasicInformation' is called
    bool bOnContactDisappear = false;  // Whether the projectile disappears when entering a collider
    public Collider colToIgnore;  // Player's collider to stop the projectile from exploding in the player's face, because it detects the shooter's collider

    private void Update()
    {
        if(bFuseActive)
        {
            fFuse -= Time.deltaTime;
            if(fFuse <= 0f)
            {
                Explode();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(bOnContactDisappear && collision.collider != colToIgnore)
        {
            Explode();
        }
    }

    /// <summary>
    /// Starts a timer on the projectile, forcing it to explode at the end
    /// </summary>
    /// <param name="fTime">time specified</param>
    void StartFuse(float fTime)
    {
        fFuse = fTime;
        bFuseActive = true;
    }

    /// <summary>
    /// Only public facing function, used to instantiate itself and then simulates itself on its own
    /// </summary>
    /// <param name="iExplosionType">Type specified when it explodes and sends an 'ExplosionPacket'</param>
    /// <param name="bOnContact">Whether the </param>
    /// <param name="bUseGrav"></param>
    /// <param name="v3Launch"></param>
    /// <param name="fFuse"></param>
    public void BasicInformation(int iExplosionType, bool bOnContact, bool bUseGrav, Vector3 v3Launch, float fFuse = -1f)
    {
        GetComponent<Rigidbody>().AddForce(v3Launch, ForceMode.Impulse);
        GetComponent<Rigidbody>().useGravity = bUseGrav;
        if(fFuse >= 0f)
        {
            StartFuse(fFuse);
        }
        iType = iExplosionType;
        bOnContactDisappear = bOnContact;
    }

    /// <summary>
    /// Sends an 'ExplosionPacket' to the server when any condition for it is met
    /// </summary>
    void Explode()
    {
        NetworkManager.ExplosionPacket.Enqueue(new NetworkManager.ExplosionPacket.ExplosionData(iType, transform.position, GetComponent<NetworkGameObject>().GetNetworkID()));
        Destroy(gameObject);
    }
}
