using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;


/// <summary>
/// This is the player's weapons in their internal forms, All weapons are stored here and when the player fires it looks in its dictionary to dictate how the player should act based on what the player is internally holding
/// 
/// A more sophisticated form would require 1 or more weapon objects attached to the player and those objects would store how it would work, lightening how much code is in each player, however this form allows for a very easy way to create same-y weapons without needing completely unique logic per weapon
/// </summary>
public class WeaponAttachment : MonoBehaviour
{
    [SerializeField] GameObject[] agoAttachedWeaponAsset;  // The asset that can be swapped in for each kind of weapon, this requires all assets to exist on every character and simply enabling and disabling causes the difference
    [SerializeField] GameObject goBaseProjectile;  // The prefab used for projectile weapons when firing (not dummy version for networking)
    [SerializeField] GameplayUI gameui;  // Reference to GameplayUI
    [SerializeField] bool bDummyMode = false;  // Dummy mode is for visualisation of weapon switching of another client's character without any internal simulation
    [SerializeField] PlayerMovement pmOriginOfBullet;  // Player object that the bullet originates from (this is locked to only player objects)
    [SerializeField] Collider colUsedToIgnoreProjectile;
    
    // The actual weapon types the player has (when the selected is none, the player doesn't have weapon functionality, regardless of it is in dummy mode)
    WeaponType wtESelected = WeaponType.None;
    WeaponType wtEReserve = WeaponType.None;

    // These elements are used to coordinate with the UI, while the player doesn't necessarilly know its own ammo, it can infer based on what happened from its perspective and always has stats in case of lost packets
    int iSelectedSlot = 0;
    int iMaxAmmoSelected;
    int iMaxAmmoReserve;
    int iCurAmmoSelected;
    int iCurAmmoReserve;
    static Dictionary<WeaponType, WeaponStats> dictwtEwsAll;
    public enum WeaponType { None = -1, AssaultRifle, Shotgun, SniperRifle, SubmachineGun, Pistol, Revolver, RocketLauncher, GrenadeLauncher};
    

    public struct WeaponStats
    {
        public string sName;
        public bool bHitscan;
        public float fUpwardRecoil;  // measured in degrees
        public float fVariedHorizontalRecoil;
        public float fShotSpread;
        public int iBulletsPerShot;
        public float fMaxDistance; // for hitscan
        public bool bProjectileGravity; // for non-hitscan
        public float fProjectileVelocity; // for non-hitscan
        public bool bExplosionOnDisappear; // for non-hitscan
        public bool bDisappearOnContact; // for non-hitscan
        public float fProjectileDisappearTime; // for non-hitscan
        public int iExplosionType;  // for exploding non-hitscan
    }

    private void Awake()
    {
        ResetWeapons();
        if(!bDummyMode && dictwtEwsAll == null)
        {
            dictwtEwsAll = new Dictionary<WeaponType, WeaponStats>();
            WeaponStats wsThis = new WeaponStats();
            // Assault Rifle
            wsThis.sName = "Assault Rifle";
            wsThis.bHitscan = true;
            wsThis.fUpwardRecoil = 1f;
            wsThis.fVariedHorizontalRecoil = 1f;
            wsThis.fShotSpread = 0.1f;
            wsThis.iBulletsPerShot = 1;
            wsThis.fMaxDistance = 20f;
            for (int i = 0; i < 8; i++)
            {
                if(i != 0)
                {
                    wsThis = dictwtEwsAll[0];
                }
                switch((WeaponType)i)
                {
                    case WeaponType.AssaultRifle:
                        break;
                    case WeaponType.Shotgun:
                        wsThis.iBulletsPerShot = 8;
                        wsThis.fMaxDistance = 10f;
                        wsThis.fShotSpread = 0.3f;
                        break;
                    case WeaponType.SniperRifle:
                        wsThis.fUpwardRecoil = 4f;
                        wsThis.fVariedHorizontalRecoil = 1f;
                        wsThis.fShotSpread = 0.02f;
                        break;
                    case WeaponType.SubmachineGun:
                        wsThis.fShotSpread = 0.25f;
                        break;
                    case WeaponType.Pistol:
                        wsThis.fUpwardRecoil = 0.4f;
                        wsThis.fVariedHorizontalRecoil = 0.2f;
                        break;
                    case WeaponType.Revolver:
                        wsThis.fUpwardRecoil = 1.2f;
                        wsThis.fVariedHorizontalRecoil = 0.5f;
                        break;
                    case WeaponType.RocketLauncher:
                        wsThis.bHitscan = false;
                        wsThis.bExplosionOnDisappear = true;
                        wsThis.bDisappearOnContact = true;
                        wsThis.bProjectileGravity = false;
                        wsThis.fProjectileDisappearTime = 5f;
                        wsThis.iExplosionType = 0;
                        break;
                    case WeaponType.GrenadeLauncher:
                        wsThis.bHitscan = false;
                        wsThis.bExplosionOnDisappear = true;
                        wsThis.bDisappearOnContact = false;
                        wsThis.fProjectileDisappearTime = 4f;
                        wsThis.bProjectileGravity = true;
                        wsThis.iExplosionType = 1;
                        break;
                }
                wsThis.sName = System.Enum.GetName(typeof(WeaponType), i);
                dictwtEwsAll.Add((WeaponType)i, wsThis);
            }
        }
    }

    private void Update()
    {
        if(!bDummyMode && wtESelected != WeaponType.None)
        {  // weapon controls are inaccessible without weapons selected
            if(Input.GetKeyDown(KeyCode.E))
            {
                SwapWeapon();
                NetworkManager.GetUIManager().EndReload();
                NetworkManager.AddPacketTypeToQueue(NetworkManager.SendPacket.SwapWeapon);
            }
            if(Input.GetKeyDown(KeyCode.R))
            {
                NetworkManager.StartReload();
            }
            if(Input.GetMouseButtonDown(0))
            {
                (Vector3, float, int)[] info = FireWeapon(wtESelected);
                for(int i = 0; i < info.Length; i++)
                {
                    NetworkManager.FireWeaponPacket.QueueUpPacket(pmOriginOfBullet.transform.position, info[i].Item1, info[i].Item2, info[i].Item3);
                    //NetworkManager.GetUIManager().DrawLine(pmOriginOfBullet.transform.position, info[i].Item1, info[i].Item2);
                }
                NetworkManager.AddPacketTypeToQueue(NetworkManager.SendPacket.FireWeapon);
            }
        }
    }

    /// <summary>
    /// Fires the weapon and does any effects on the player it needs while outputting the information needed for 'FireWeaponPacket'(s)
    /// </summary>
    /// <param name="wtE">Weapon type that is fired</param>
    /// <returns>'FireWeaponPacket' attributes, in an array in case the weapon fires multiple shots</returns>
    (Vector3, float, int)[] FireWeapon(WeaponType wtE)
    {
        WeaponStats wsThis = dictwtEwsAll[wtE];
        (Vector3, float, int)[] av3fiShots = new (Vector3, float, int)[wsThis.iBulletsPerShot];
        for(int i = 0; i < av3fiShots.Length; i++)
        {
            Vector3 v3Dir = Vector3.Normalize(pmOriginOfBullet.camPlayer.transform.forward + pmOriginOfBullet.camPlayer.transform.up * Random.Range(-wsThis.fShotSpread, wsThis.fShotSpread) + pmOriginOfBullet.camPlayer.transform.right * Random.Range(-wsThis.fShotSpread, wsThis.fShotSpread));
            if(wsThis.bHitscan)
            {
                RaycastHit rayhit;
                bool bHit = Physics.Raycast(pmOriginOfBullet.camPlayer.transform.position, v3Dir, out rayhit, wsThis.fMaxDistance, LayerMask.GetMask("Player", "Scenery"));
                NetworkGameObject ngoHit = null;
                if (bHit)
                {
                    rayhit.collider.TryGetComponent<NetworkGameObject>(out ngoHit);
                }
                av3fiShots[i] = (v3Dir, bHit ? rayhit.distance : wsThis.fMaxDistance, (ngoHit == null) ? -1 : ngoHit.GetNetworkID());
            }
            else
            {
                ProjectileLogic projectile = GameObject.Instantiate(goBaseProjectile, transform.position, Quaternion.identity).GetComponent<ProjectileLogic>();
                projectile.colToIgnore = colUsedToIgnoreProjectile;
                projectile.BasicInformation(wsThis.iExplosionType, wsThis.bDisappearOnContact, wsThis.bProjectileGravity, v3Dir, wsThis.fProjectileDisappearTime);
                NetworkManager.AddPacketTypeToQueue(NetworkManager.SendPacket.NIDRequest);
            }
        }
        return av3fiShots;
    }

    /// <summary>
    /// This function is called when the server sends 'SelectWeaponPacket's to tell the player what weapons it has.
    /// For security, the server insigates this in case of a client exploit allowing the player to pick unselectable weapons (if the game were to have an unlock system) or when the player has unexpected behaviour (like not selecting a weapon within the time limit)
    /// </summary>
    /// <param name="iSlot">Weapon slot that this weapon should be attached to (while the player shouldn't care logic-wise (since the server does that for it) it needs to know for UI and packet loss)</param>
    /// <param name="wtEThis">The type of weapon assigned for this slot</param>
    /// <param name="iMaxAmmoThis">Max ammo of the weapon, this isn't stored as weapon information, since reloading is handled by the server</param>
    /// <returns>weapon type that this weapon replaces, most of the time this will be None(-1)</returns>
    /// <remarks>In the case that the player selects an invalid weapon or no weapon is selected, the server should pick 1 randomly, meaning this function will always be called twice per round per player</remarks>
    public WeaponType SetWeapon(int iSlot, WeaponType wtEThis, int iMaxAmmoThis)
    {
        iSelectedSlot = 0;
        WeaponType wtEOut;
        gameui.SetWeaponName(iSlot, dictwtEwsAll[wtEThis].sName);
        if(iSlot == 0)
        {
            wtEOut = wtESelected;
            wtESelected = wtEThis;
            iMaxAmmoSelected = iMaxAmmoThis;
            iCurAmmoSelected = iMaxAmmoThis;
            gameui.SetMaxAmmo(iMaxAmmoThis);
            gameui.WeaponSelected(0);
            OnlyShowWeapon(wtEThis);
        }
        else
        {
            wtEOut = wtEReserve;
            wtEReserve = wtEThis;
            iMaxAmmoReserve = iMaxAmmoThis;
            iCurAmmoReserve = iMaxAmmoThis;
        }
        return wtEOut;
    }

    /// <summary>
    /// Swaps 'reserve' and 'selected' information, to coordinate with UI elements
    /// </summary>
    public void SwapWeapon()
    {
        WeaponType wtE = wtESelected;
        wtESelected = wtEReserve;
        wtEReserve = wtE;
        int iTemp = iMaxAmmoSelected;
        iMaxAmmoSelected = iMaxAmmoReserve;
        iMaxAmmoReserve = iTemp;
        iTemp = iCurAmmoSelected;
        iCurAmmoSelected = iCurAmmoReserve;
        iCurAmmoReserve = iTemp;
        iSelectedSlot++;
        iSelectedSlot %= 2;
        OnlyShowWeapon(wtESelected);
        gameui.WeaponSelected(iSelectedSlot);
    }

    /// <summary>
    /// This version of 'SwapWeapon' is called when a 'SwapWeaponPacket is received, so the server can force what weapon the player is holding to make sure no foul play occurs
    /// </summary>
    /// <param name="iSlot">Forces the player to swap to this weapon</param>
    /// <param name="iForceCurAmmo">Forces the weapon in slot 'iSlot' to have this much ammo</param>
    public void SwapWeapon(int iSlot, int iForceCurAmmo)
    {
        if(iSelectedSlot != iSlot)
        {
            SwapWeapon();
        }
        iCurAmmoSelected = iForceCurAmmo;
    }

    /// <summary>
    /// Activates the weapon object specified and deactivates all others
    /// </summary>
    /// <param name="wtE">Weapon specified</param>
    public void OnlyShowWeapon(WeaponType wtE)
    {
        for(int i = 0; i < agoAttachedWeaponAsset.Length; i++)
        {
            if (agoAttachedWeaponAsset[i].activeSelf != i.Equals((int)wtE))
            {
                agoAttachedWeaponAsset[i].SetActive(i.Equals((int)wtE));
            }
        }
    }

    public void ResetWeapons()
    {
        wtESelected = WeaponType.None;
        wtEReserve = WeaponType.None;
        OnlyShowWeapon(WeaponType.None);
    }
}
