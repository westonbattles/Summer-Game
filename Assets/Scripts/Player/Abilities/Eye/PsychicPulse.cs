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
    [SerializeField, Min(0.01f)] float selectionRadius = 1.25f; // world space max radius around camera forward ray enemies are allowed to be in to be targeted
    [SerializeField] float distanceScoreWeight = 0.05f;
    [SerializeField] float targetHighlightCheckInterval = 0.12f;
    [SerializeField] int maxPossibleTargets = 128;
    [SerializeField, Range(-20f, 0f), Tooltip("More negative values = enemies further behind the player relative to the camera are allowed to be targeted")] float backPruneThreshold = 0f;
    
    [Header("VFX")]
    [SerializeField] PooledVfx highlightVFXPrefab;

    [Header("Gizmos")]
    
    LayerMask TargetLayers => Enemy.LayerMask;
    
    
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
        
        // don't do highlight updates if cooldown isn't done yet
        if (psychicPulse.CooldownRemaining > 0f) return;
        
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
        
        
        // if no enemies selected, return false and don't execute
        Enemy target = psychicPulse.CurrentTarget;
        if (target == null)
        {
            Debug.Log("No enemies selected");
            return false;
        }
        
        // apply knockback to target and do damage
        
        
        // clear highlight and target
        ClearTarget(psychicPulse);

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
        Vector3 cameraOrigin = psychicPulse.PlayerAim.AimOrigin;
        Vector3 cameraForward = psychicPulse.PlayerAim.AimDirection;
        
        //float halfVerticalTan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad); // up/down fov for some reason @unity

        int hitCount = Physics.OverlapSphereNonAlloc(
            playerPosition,
            maxTargetDistance,
            psychicPulse.CandidateBuffer,
            TargetLayers,
            QueryTriggerInteraction.Collide);
        
        // if the overlap sphere got an equal number of enemies as our buffer, it means more enemies than our buffer allows for was in the allocsphere haha
        if (hitCount == psychicPulse.CandidateBuffer.Length) Debug.LogWarning("PsychicPulse candidate buffer may be full and missed out on enemies for valid highlight / targeting. Increase maxPossibleTargets if the amount of enemies causing this warning will happen in game");
        
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
            
            // btw, the value of this dot product is just the magnitude of the cameraToTarget vector after it has been projected onto the camera forward (since cameraForward is normalized)
            float targetDistanceAlongAimRay = Vector3.Dot(cameraForward, cameraToTarget);
            
            // prune targets behind the camera
            if (targetDistanceAlongAimRay <= 0f) continue; // || cameraForwardDotTarget > maxTargetDistance) continue; // <-- prunes targets too far from CAMERA but it's a bit redunant since we do this pruning from player position alr. prob only need that one
            
            // prune targets too far behind the player body relative to the camera
            if (Vector3.Dot(cameraForward, targetPoint - playerPosition) < backPruneThreshold) continue;

            Vector3 targetProjectionOnCameraForward = cameraOrigin + cameraForward * targetDistanceAlongAimRay; // the closest point on the camera forward ray to our target -> its 90 deg projection

            float distanceFromAimRaySqr = (targetPoint - targetProjectionOnCameraForward).sqrMagnitude;
            float selectionRadiusSqr = selectionRadius * selectionRadius;
            if (distanceFromAimRaySqr > selectionRadiusSqr) continue;
            
            // NEW METHOD - sort by distance to closest point in the camera forward ray
            float normalizedSideDistanceSqr = distanceFromAimRaySqr / selectionRadiusSqr; // garunteed 0-1 value
            float score = normalizedSideDistanceSqr + targetDistanceAlongAimRay * distanceScoreWeight; // farther away (high
            if (score < bestScore)
            {
                bestScore = score;
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
        if (pp.CooldownRemaining > 0f) return Mathf.Clamp01(pp.CooldownRemaining / CooldownTime);
        return 0f;
    }
    
}
