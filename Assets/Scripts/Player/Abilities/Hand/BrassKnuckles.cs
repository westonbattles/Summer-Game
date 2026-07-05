using System;
using System.Collections.Generic;
using UnityEngine;


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
    public int MaxCombo => baseComboStages + Player.Instance.Modifiers.BrassKnucklesComboStagesBonus;
    public float ComboIncrease => baseComboIncrease * Player.Instance.Modifiers.BrassKnucklesComboIncreaseDamageMultiplier;
    
    // todo: list of animations for each state?
    
    // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapBoxNonAlloc.html properties
    [Header("Boxcast")]
    [SerializeField] Vector3 boxCastHalfExtents = new Vector3(0.385f, 0.6f, 0.345f);
    [SerializeField] float boxCastOffsetDistance;
    [SerializeField] Vector3 boxCastOffsetVector3;
    [SerializeField] int maxEntityCount = 16;
    [SerializeField] LayerMask hitMask = ~0;
    
    
    class BrassKnucklesState : AbilityState
    {
        public Player Player;

        public int ComboState = 0;
        // public bool ComboActive => ComboState != ComboStage.First;
        public float CooldownRemaining;         // how much time left on the ability cooldown
        public float ComboResetTimeRemaining;   // how much time left on the combo reset timer

        public BrassKnucklesState(Player player)
        {
            Player = player;
        }

        public float GetActiveMultiplier(float comboIncrease)
        {
            return 1f + ComboState * comboIncrease;
        }
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

        //Debug.Log($"{brassKnuckles.ComboResetTimeRemaining}");
        if (brassKnuckles.ComboResetTimeRemaining == 0)
        {
            brassKnuckles.ComboState = 0; // reset the state when combo timer runs out
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
        
        // boxcast
        Quaternion rotation = Quaternion.AngleAxis(brassKnuckles.Player.Look.Yaw, Vector3.up);
        Vector3 positionOffset = rotation * Vector3.forward * boxCastOffsetDistance + boxCastOffsetVector3;

        Collider[] results = new Collider[maxEntityCount];

        if (Physics.OverlapBoxNonAlloc(
                positionOffset + brassKnuckles.Player.transform.position,
                boxCastHalfExtents,
                results,
                rotation,
                hitMask) > 0)
        {
            float damage = baseDamage * brassKnuckles.GetActiveMultiplier(ComboIncrease);
            DamageInfo damageInfo = new DamageInfo(damage, brassKnuckles.Player.gameObject, isCrit: false); // todo : implement crit chance?
            
            foreach (Collider result in results)
            {
                IDamageable target = result?.GetComponentInParent<IDamageable>();
                target?.TakeDamage(damageInfo);
            }
            
            // todo: punch hit vfx / particles. maybe individual on each enemy? but prob not
            
            // update combo state 
            if (brassKnuckles.ComboState < MaxCombo) brassKnuckles.ComboState += 1;
            brassKnuckles.ComboResetTimeRemaining = baseComboResetTime;
        }
        
        // todo: swing animation + vfx
        
        brassKnuckles.CooldownRemaining = baseCooldownTime; // start cooldown countdown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return 0f;
        
        return brassKnuckles.CooldownRemaining / baseCooldownTime;
    }
    // no cancel since carrying momentum after character swap is kinda cool
    /*public override void Cancel(AbilityState state)
    {
    }*/
    
    
    // gizmo debug for hitbox. can just call in player.cs OnDrawGizmo or something
    public void DrawGizmos(Transform playerTransform)
    {

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