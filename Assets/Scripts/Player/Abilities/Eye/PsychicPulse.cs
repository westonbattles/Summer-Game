using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PsychicPulse : AbilityBehaviour
{
    [Header("Properties")]
    // dmg stuff?
    [SerializeField] float baseCooldownTime = 2f;
    
    [Header("Targetting")]
    [SerializeField] float maxTargetDistance = 15f;
    [SerializeField] float maxDistanceFromCrosshair = 0.25f; // screenspace
    [SerializeField] float targetHighlightCheckInterval = 0.12f;
    [SerializeField] int maxPossibleTargets = 32;
    
    [Header("VFX")]
    [SerializeField] PooledVfx highlightVFXPrefab;
    
    LayerMask TargetLayers = Enemy.LayerMask;
    
    
    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float CooldownTime => baseCooldownTime;
    

    class PsychicPulseState : AbilityState
    {
        public readonly Player Player;
        public readonly PlayerAim PlayerAim;
        public HashSet<Enemy> Targets = new(); // hashmap to de-dupe targets selected for finding which enemy to highlight

        public Enemy CurrentTarget;
        
        public float CooldownRemaining;
        public float HighlightCheckRemaining;
        
        public Collider[] CandidateBuffer;
        //Enemy HighlightedEnemy;

        public PsychicPulseState(Player player, int maxPossibleTargets)
        {
            Player = player;
            PlayerAim = player.Aim;
            CandidateBuffer = new Collider[maxPossibleTargets];
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        return new PsychicPulseState(player, maxPossibleTargets);
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        PsychicPulseState psychicPulse = state as PsychicPulseState;
        if (psychicPulse == null) return;

        // cooldown update
        psychicPulse.CooldownRemaining = Mathf.Max(0f, psychicPulse.CooldownRemaining - deltaTime);
        
        // make sure eye loadout is equipped
        if (psychicPulse.Player.AbilityController.CurrentLoadout != psychicPulse.Player.EyeLoadout) return;
        

        // ---------- ENEMY HIGHLIGHTING / TARGETING -------------
        // only check for highlights using our interval rate, not every frame
        psychicPulse.HighlightCheckRemaining -= deltaTime;
        if (psychicPulse.HighlightCheckRemaining > 0f) return;
        psychicPulse.HighlightCheckRemaining = targetHighlightCheckInterval;

        // set target (handles highlighting / dehighlighting)
        Enemy target = FindBestTarget(psychicPulse);
        SetTarget(psychicPulse, target);
    }
    
    public override bool TryExecute(AbilityState state)
    {
        PsychicPulseState psychicPulse = state as PsychicPulseState;
        if (psychicPulse == null) return false;
        if (psychicPulse.CooldownRemaining > 0f) return false;
        
        // if get enemy target thing return no enemies, return false and don't execute
        Enemy target = FindBestTarget(psychicPulse);
        if (target == null) return false;
        
        // apply knockback to target and do damage

        psychicPulse.CooldownRemaining = CooldownTime;
        return true;
    }
    
    public override void Cancel(AbilityState state)
    {
        PsychicPulseState psychicPulse = state as PsychicPulseState;
        if (psychicPulse == null) return;
        
        // remove highlighted target
        ClearTarget(psychicPulse);
    }
    
    Enemy FindBestTarget(PsychicPulseState psychicPulse)
    {
        Vector3 playerPosition = psychicPulse.Player.transform.position;
        Vector3 playerForward = psychicPulse.Player.transform.forward;
        Vector3 cameraOrigin = psychicPulse.PlayerAim.AimOrigin;
        Quaternion cameraRotation = psychicPulse.PlayerAim.AimRotation;
        Quaternion inverseCameraRotation = Quaternion.Inverse(cameraRotation);
        
        float fov = psychicPulse.PlayerAim.AimCamera.fieldOfView;
        float aspect = psychicPulse.PlayerAim.AimCamera.aspect;
        float halfVerticalTan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad); // up/down fov for some reason @unity
        float halfHorizontalTan = aspect * halfVerticalTan;

        int hitCount = Physics.OverlapSphereNonAlloc(
            playerPosition,
            maxTargetDistance,
            psychicPulse.CandidateBuffer,
            TargetLayers,
            QueryTriggerInteraction.Collide);
        
        Enemy target = null;
        float bestScore = Mathf.Infinity;
        psychicPulse.Targets.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = psychicPulse.CandidateBuffer[i];
            if (collider == null) continue;

            Enemy enemy = collider.GetComponentInParent<Enemy>();
            if (enemy == null || !psychicPulse.Targets.Add(enemy)) continue;
            
            // check if enemy is closer than current target enemy
            
            Vector3 targetPoint = enemy.GetCenterPoint();
            Vector3 cameraToTarget = targetPoint - cameraOrigin;
            Vector3 localCameraToTarget = inverseCameraRotation * cameraToTarget; // apparently inverse quat calculations is literally just negating the xyz components
            
            // btw localCameraToTarget just takes our cameraToTarget vector and rotates it such that the camera forward vector would now be pointing along the z axis. it undoes the camera rotation and applies it to our vector
            
            // skip if our target is behind the camera or behind the player
            if (localCameraToTarget.z <= 0) continue;
            if (Vector3.Dot(playerForward, targetPoint - playerPosition) < 0f) continue;
            
            // convert to screenspace and make sure its within our screen radius
            float tanX = localCameraToTarget.x / localCameraToTarget.z;
            float tanY = localCameraToTarget.y / localCameraToTarget.z;

            float screenX = tanX / halfHorizontalTan;
            float screenY = tanY / halfVerticalTan;

            float screenDistanceSquared = screenX*screenX + screenY*screenY;
            if (screenDistanceSquared > maxDistanceFromCrosshair * maxDistanceFromCrosshair) continue; // prune enemies outside of max screen dist from the center of the screen

            if (screenDistanceSquared < bestScore)
            {
                bestScore = screenDistanceSquared;
                target = enemy;
            }
            
        }
        return target;
    }

    /// sets and highlights target
    void SetTarget(PsychicPulseState psychicPulse, Enemy target)
    {
        if (psychicPulse.CurrentTarget == target) return;
        
        ClearTarget(psychicPulse);
        psychicPulse.CurrentTarget = target;
        
        if (target == null) return;
        
        // HIGHLIGHT
        target.Highlighter.Highlight(highlightVFXPrefab);
    }

    // stops vfx on current target and sets current target to null
    void ClearTarget(PsychicPulseState psychicPulse)
    {
        // UNHIGHLIGHT
        if (psychicPulse.CurrentTarget == null) return;
        psychicPulse.CurrentTarget.Highlighter.Unhighlight();
        
        psychicPulse.CurrentTarget = null;
    }
    
    public override float GetCooldownFraction(AbilityState state)
    {
        if (state is not PsychicPulseState pp) return 0f;
        Debug.Log(pp.CooldownRemaining);
        if (pp.CooldownRemaining > 0f) return Mathf.Clamp01(pp.CooldownRemaining / CooldownTime);
        return 0f;
    }
    
}
