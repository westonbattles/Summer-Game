using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


[Serializable]
public class BrassKnuckles : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] public float baseDamage = 10f;
    [Tooltip("Percentage that damage scales with each subsequent combo state")]
    [SerializeField] public float baseComboIncrease = 0.25f;
    [SerializeField] public int baseComboStages = 3;
    [SerializeField] public float baseCooldownTime = 0.65f;
    [SerializeField] public float baseComboResetTime = 2.5f;
    
    // Modifiers
    public int MaxCombo => baseComboStages - 1 + Player.Instance.Modifiers.BrassKnucklesComboStagesBonus; // minus 1 bc 0 is our first 'combo' index
    public float ComboIncrease => baseComboIncrease * Player.Instance.Modifiers.BrassKnucklesComboIncreaseDamageMultiplier;
    
    [Header("Animation Syncing")]
    [SerializeField] float[] punchHitDelays = { 0.13f, 0.13f, 0.4f };
    
    // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapBoxNonAlloc.html properties
    [Header("Boxcast")]
    [SerializeField] Vector3 boxCastHalfExtents = new Vector3(0.385f, 0.6f, 0.345f);
    [SerializeField] float boxCastOffsetDistance;
    [SerializeField] Vector3 boxCastOffsetVector3;
    [SerializeField] int maxEntityCount = 16;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] bool showGizmo = false;
    
    
    class BrassKnucklesState : AbilityState
    {
        public Player Player;

        public int ComboState = 0;
        public int PunchIndex = 0;              // increases every punch that happens so we can have suequntial aniamtions
        // public bool ComboActive => ComboState != ComboStage.First;
        public float CooldownRemaining;         // how much time left on the ability cooldown
        public float ComboResetTimeRemaining;   // how much time left on the combo reset timer
        public bool HitPending;
        public float HitDelayRemaining;
        public int LastAnimation;
        
        public HashSet<IDamageable> DamagedTargets = new();
        

        public BrassKnucklesState(Player player)
        {
            Player = player;
        }

        public float GetActiveMultiplier(float comboIncrease) { return 1f + ComboState * comboIncrease; }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        BrassKnucklesState state = new BrassKnucklesState(player);
        return state;
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return;
        
        brassKnuckles.CooldownRemaining = Mathf.Max(0f, brassKnuckles.CooldownRemaining- deltaTime);
        brassKnuckles.ComboResetTimeRemaining = Mathf.Max(0f, brassKnuckles.ComboResetTimeRemaining - deltaTime);

        
        if (brassKnuckles.HitPending)
        {
            brassKnuckles.HitDelayRemaining = Mathf.Max(0f, brassKnuckles.HitDelayRemaining -= deltaTime);
            if (brassKnuckles.HitDelayRemaining == 0)
            {
                brassKnuckles.HitPending = false;
                Punch(brassKnuckles);
            }
        }
        
        //Debug.Log($"{brassKnuckles.ComboResetTimeRemaining}");
        if (!brassKnuckles.HitPending && brassKnuckles.ComboResetTimeRemaining == 0)
        {
            brassKnuckles.ComboState = 0; // reset the state when combo timer runs out
            brassKnuckles.PunchIndex = 0;
        }
    }

    public override bool TryExecute(AbilityState state)
    {
        
        // make sure we actually can/should execute
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return false;
        if (brassKnuckles.CooldownRemaining > 0f) return false;
  
        //Debug.Log($"Combo State: {brassKnuckles.ComboState}"); // 0 -> 1 -> 2 ->...
        
        // succesful execution

        int animationState = brassKnuckles.PunchIndex % baseComboStages;
        
        brassKnuckles.HitPending = true;
        brassKnuckles.HitDelayRemaining = punchHitDelays[animationState];
        
        // swing animation
        // random 0,1 if we are swinging and have hit nothing yet
        if (brassKnuckles.PunchIndex == 0) animationState = Random.Range(0, 2);
        // if are in a combo, play opposite animation to the last one if it was a sepcific animation
        else if (brassKnuckles.LastAnimation == 1 && animationState == 1) animationState = 0;
        brassKnuckles.Player.Animator.PlayBrassKnucklesAnimation(animationState);
        brassKnuckles.LastAnimation = animationState;
        
        // todo: swing vfx
        
        brassKnuckles.PunchIndex += 1;
        brassKnuckles.CooldownRemaining = baseCooldownTime; // start cooldown countdown
        return true;
    }

    void Punch(BrassKnucklesState brassKnuckles)
    {
        // boxcast
        Quaternion rotation = Quaternion.AngleAxis(brassKnuckles.Player.Look.Yaw, Vector3.up);
        Vector3 positionOffset = rotation * Vector3.forward * boxCastOffsetDistance + boxCastOffsetVector3;
        
        Collider[] results = new Collider[maxEntityCount];
        int hitCount = Physics.OverlapBoxNonAlloc(
            positionOffset + brassKnuckles.Player.transform.position,
            boxCastHalfExtents,
            results,
            rotation,
            hitMask);
        
        if (hitCount > 0)
        {
            
            Debug.Log($"Hit! Combo State: {brassKnuckles.ComboState}, Multiplier: {brassKnuckles.GetActiveMultiplier(ComboIncrease)}");
            
            // build damageinfo
            float damage = baseDamage * brassKnuckles.GetActiveMultiplier(ComboIncrease);
            DamageInfo damageInfo = new DamageInfo(damage, brassKnuckles.Player.gameObject, isCrit: false); // todo : implement crit chance?
            
            // de-duplicate
            brassKnuckles.DamagedTargets.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                IDamageable target = results[i].GetComponentInParent<IDamageable>();
                if (target != null && brassKnuckles.DamagedTargets.Add(target))
                {
                    target?.TakeDamage(damageInfo);
                }
            }
            
            // todo: punch hit vfx / particles. maybe individual on each enemy? but prob not

            // update combo state 
            if (brassKnuckles.ComboState < MaxCombo) brassKnuckles.ComboState += 1;
            brassKnuckles.ComboResetTimeRemaining = baseComboResetTime;
            
        }
        else
        { Debug.Log($"Miss! Combo State: {brassKnuckles.ComboState}, Multiplier: {brassKnuckles.GetActiveMultiplier(ComboIncrease)}"); }
    }
    
    public override float GetCooldownFraction(AbilityState state)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return 0f;
        
        return brassKnuckles.CooldownRemaining / baseCooldownTime;
    }

    public override void Cancel(AbilityState state)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return;

        brassKnuckles.HitPending = false; // make sure we don't do a delayed hit after cancel
    }
    
    
    // gizmo debug for hitbox. can just call in player.cs OnDrawGizmo or something
    public void DrawGizmos(Transform playerTransform)
    {
        if (!showGizmo) return;

        Quaternion rotation =
            Player.Instance != null && Player.Instance.Look != null
                ? Quaternion.AngleAxis(Player.Instance.Look.Yaw, Vector3.up)
                : playerTransform.rotation;
        
        Vector3 center =
            playerTransform.position +
            rotation * Vector3.forward * boxCastOffsetDistance +
            boxCastOffsetVector3;

        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = Color.limeGreen;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxCastHalfExtents * 2f);

        Gizmos.matrix = previousMatrix;
    }
}