using System;
using UnityEngine;

[Serializable]
public class Revolver : AbilityBehaviour
{
    [Header("Magazine")] 
    [SerializeField, Min(1)] int magazineSize = 6;
    [SerializeField, Min(0f)] float reloadDuration = 0.2f;

    [Header("Firing")]
    [SerializeField, Min(0)] float baseDamage = 12f;
    [SerializeField, Min(0)] float baseBulletSpeed = 70f;
    [SerializeField, Min(0)] float baseMaxRange = 100f;
    [SerializeField, Min(0)] float baseSecondsBetweenShots  = 0.15f; // shot cooldown
    [SerializeField] LayerMask hitLayers = ~0;

    [Header("Prefab")]
    [SerializeField] Projectile bulletPrefab;
    //[Tooltip("Transform for the tip of the gun's muzzle to use as the origin for our revolver shots")]
    //[SerializeField] Transform muzzle;
    
    //[Header("Projectile")]
    //[SerializeField] Projectile projectilePrefab;
    //[SerializeField] float projectileSpeed = 80f;
    //[SerializeField] Transform muzzle;

    class RevolverState : AbilityState
    {
        public RevolverRecoilController RecoilController;
        public PlayerAim PlayerAim;
        public GameObject Shooter;

        public int Ammunition;              // how many shots u have before you need to reload
        public bool IsReloading;            // defaults to false
        public float ShotCooldownRemaining; // how much time left on the shot cooldown
        public float ReloadTimeRemaining;   // how much time left on the reload

        public RevolverState(PlayerAim playerAim, RevolverRecoilController recoilController, int startingAmmunition)
        {
            PlayerAim = playerAim;
            RecoilController = recoilController;
            Shooter = playerAim.gameObject;
            Ammunition = startingAmmunition;
        }
    }

    public override AbilityState CreateState()
    {
        PlayerAim playerAim = Player.Instance.Aim;
        if (playerAim == null)
        {
            Debug.LogError("Revolver couldn't find PlayerAim");
            return null;
        }
        RevolverRecoilController recoilController = playerAim.GetComponentInChildren<RevolverRecoilController>(includeInactive: true);
        RevolverState state = new RevolverState(playerAim, recoilController, magazineSize);
        return state;
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        RevolverState revolver = state as RevolverState;
        if (revolver == null) return; // make sure state variable is of type revolver state
        
        revolver.ShotCooldownRemaining = Mathf.Max(0f, revolver.ShotCooldownRemaining - deltaTime);
        
        if (!revolver.IsReloading)
        {
            if (revolver.Ammunition <= 0) BeginReload(revolver); // automatically reload when we get to 0 bullets
            return;
        }
        
        // if we are reloading currently
        revolver.ReloadTimeRemaining -= deltaTime;
        if (revolver.ReloadTimeRemaining <= 0f) FinishReload(revolver);


    }

    public override bool TryExecute(AbilityState state)
    {
        RevolverState revolver = state as RevolverState;
        if (revolver == null) return false;
        if (revolver.IsReloading) return false;
        if (revolver.ShotCooldownRemaining > 0f) return false; // shot still on cooldown
        
        // TODO - use playerAim to actually shoot bullets
        Vector3 targetPoint = revolver.PlayerAim.GetTargetPoint(baseMaxRange, hitLayers);
        Vector3 shotOrigin = revolver.RecoilController.Muzzle.position;
        Vector3 aimDirection = revolver.PlayerAim.GetShotDirection(shotOrigin, targetPoint); // normalized vector pointing in direction of where shot should be fired

        // visual recoil
        revolver.RecoilController?.Kick(aimDirection);

        // shoot projectile
        bulletPrefab.transform.localScale = Vector3.one * 10; // visual debugging scale LMAOOO
        Projectile projectile = ProjectilePoolManager.Instance.Get(bulletPrefab);

        bool isCrit = false; // Todo: implement crit chance

        DamageInfo damageInfo = new DamageInfo(baseDamage, Player.Instance.gameObject, isCrit);
        projectile.Launch(shotOrigin, aimDirection, baseBulletSpeed, damageInfo);

        
        revolver.Ammunition -= 1;
        Debug.Log($"Ability: Shot Revolver! ({revolver.Ammunition}/{magazineSize})");
        
        revolver.ShotCooldownRemaining = baseSecondsBetweenShots; // update cooldown
        
        return true;
    }

    void BeginReload(RevolverState revolver)
    {
        Debug.Log("Beginning Reload");
        
        if (reloadDuration <= 0f) // if we set reload duration to -1 it'll insta reload giving us frenzy ability potential if we want
        {
            FinishReload(revolver);
            return;
        }

        revolver.IsReloading = true;
        revolver.ReloadTimeRemaining = reloadDuration;
    }

    void FinishReload(RevolverState revolver)
    {
        Debug.Log("Finished Reload");
        
        revolver.Ammunition = magazineSize;
        revolver.ReloadTimeRemaining = 0f; // can remove, just makes things cleaner
        revolver.IsReloading = false;
    }

    // revolver doesn't need a cancel. other abilities might
    /*public override void Cancel(AbilityState state)
    {
    }*/
}
