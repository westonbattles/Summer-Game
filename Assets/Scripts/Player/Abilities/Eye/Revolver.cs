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
    LayerMask HitLayers => Enemy.LayerMask | LayerMask.GetMask("Hittable", "Ground");

    [Header("Prefab")]
    [SerializeField] Projectile bulletPrefab;
    
    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public int MagazineSize => magazineSize + Player.Instance.Modifiers.RevolverBulletsBonus.GetValue();
    public float LastBulletDamageMultiplier => Player.Instance.Modifiers.RevolverLastBulletDamageMultiplier.GetValue();
    public float ReloadDuration => reloadDuration;
    public float Damage => baseDamage;
    public float BulletSpeed => baseBulletSpeed;
    public float MaxRange => baseMaxRange;
    public float SecondsBetweenShots => baseSecondsBetweenShots;

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
        RevolverState state = new RevolverState(playerAim, recoilController, MagazineSize);
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
        if (revolver.ShotCooldownRemaining > 0f) return false; // shot still on cooldown
        if (revolver.IsReloading) return false;
        
        Vector3 targetPoint = revolver.PlayerAim.GetTargetPoint(MaxRange, HitLayers);
        Vector3 shotOrigin = revolver.RecoilController.Muzzle.position;
        Vector3 aimDirection = revolver.PlayerAim.GetShotDirection(shotOrigin, targetPoint); // normalized vector pointing in direction of where shot should be fired

        // shoot projectile
        Projectile projectile = ProjectilePoolManager.Instance.Get(bulletPrefab);

        float damage = Damage; // apply lucky bullet damage modifier
        if (revolver.Ammunition == 1) damage *= LastBulletDamageMultiplier;
        DamageInfo damageInfo = new DamageInfo(damage, DamageType.Projectile, Player.Instance.gameObject);
        
        // if obstruction exists between player and muzzle, place bullet at the obstruction there (should auto-impact)
        Vector3 playerOrigin = Player.Instance.transform.position;
        playerOrigin.y = shotOrigin.y; // same height as muzzle
        Vector3 playerToMuzzle = shotOrigin - playerOrigin;
        float obstructionDistance = playerToMuzzle.magnitude;

        if (obstructionDistance > 0.001f)
        {
            Vector3 direction = playerToMuzzle / obstructionDistance; // manually calculate normal with our magnitude
            if (Physics.Raycast(playerOrigin, direction, out RaycastHit hitInfo, obstructionDistance, HitLayers, QueryTriggerInteraction.Ignore))
            {
                shotOrigin = hitInfo.point + direction * 0.001f; // move a bit into the obstruction so we can detect overlap later in projectile code
            }
        }
        
        
        projectile.Launch(Player.Instance.Combat, shotOrigin, aimDirection, BulletSpeed, damageInfo);
        revolver.Ammunition -= 1;
        //Debug.Log($"Ability: Shot Revolver! ({revolver.Ammunition}/{MagazineSize})");
        
        revolver.ShotCooldownRemaining = SecondsBetweenShots; // update cooldown
        
        // visual recoil
        revolver.RecoilController?.Kick(aimDirection);
        
        return true;
    }

    void BeginReload(RevolverState revolver)
    {
        if (ReloadDuration <= 0f) // if we set reload duration to -1 it'll insta reload giving us frenzy ability potential if we want
        {
            FinishReload(revolver);
            return;
        }

        revolver.IsReloading = true;
        revolver.ReloadTimeRemaining = ReloadDuration;
    }

    void FinishReload(RevolverState revolver)
    {
        revolver.Ammunition = MagazineSize;
        revolver.ReloadTimeRemaining = 0f; // can remove, just makes things cleaner
        revolver.IsReloading = false;
    }

    public override int GetCurrentCharges(AbilityState state) => 
        state is RevolverState r ? r.Ammunition : 0;

    public override int GetMaxCharges(AbilityState state) => MagazineSize;

    public override float GetCooldownFraction(AbilityState state)
    {
        if (state is not RevolverState r) return 0f;
        if (r.IsReloading && ReloadDuration > 0f)
            return Mathf.Clamp01(r.ReloadTimeRemaining / ReloadDuration);
        if (SecondsBetweenShots > 0f)
            return Mathf.Clamp01(r.ShotCooldownRemaining / SecondsBetweenShots);
        return 0f;
    }


    // revolver doesn't need a cancel. other abilities might
    /*public override void Cancel(AbilityState state)
    {
    }*/
}
