using UnityEngine;

public class Revolver : AbilityBehaviour
{
    [Header("Magazine")] 
    [SerializeField, Min(1)] int magazineSize = 6;
    [SerializeField, Min(0f)] float reloadDuration = 0.2f;

    [Header("Firing")] 
    [SerializeField, Min(0)] float baseDamage = 12f;
    [SerializeField, Min(0)] float baseRange = 100f;
    [SerializeField, Min(0)] float baseSecondsBetweenShots  = 0.15f; // shot cooldown
    [SerializeField] LayerMask hitLayers = ~0;

    class RevolverState : AbilityState
    {
        public PlayerAim PlayerAim;
        public GameObject Shooter;

        public int Ammunition;              // how many shots u have before you need to reload
        public bool IsReloading;            // defaults to false
        public float ShotCooldownRemaining; // how much time left on the shot cooldown
        public float ReloadTimeRemaining;   // how much time left on the reload

        public RevolverState(PlayerAim playerAim, int startingAmmunition)
        {
            PlayerAim = playerAim;
            Shooter = playerAim.gameObject;
            Ammunition = startingAmmunition;
        }
    }

    public override AbilityState CreateState()
    {
        PlayerAim playerAim = Object.FindFirstObjectByType<PlayerAim>(); // ewwwwwwwww, its prob fine but should we throw an instance on the player class?
        if (playerAim == null)
        {
            Debug.LogError("Revolver couldn't find PlayerAim");
            return null;
        }
        
        RevolverState state = new RevolverState(playerAim, magazineSize);
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
        

        Debug.Log("Ability: Shot Revolver!");
        revolver.Ammunition -= 1;
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
}
