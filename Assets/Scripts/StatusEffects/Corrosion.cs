using System;
using UnityEngine;

/// Delete the hooks you don't use. Per-entity state goes in ctx.Instance.State, not in fields.

/*
 * CORROSION DAMAGE FORMULA (Current HP Scale)
 * --------------------------------------------------------------------------------------
 * Damage = CurrentHP * Base% * Stacks
 * 
 * EXAMPLE:
 *   initialTickDamageHealthPercent = 0.05 (5% Current HP per tick), Stacks = 1
 * 
 *   - At 1000 HP: 1000 * 0.05 = 50 damage
 *   - At  500 HP:  500 * 0.05 = 25 damage
 *   - At  100 HP:  100 * 0.05 =  5 damage
 */

[Serializable]
public sealed class Corrosion : StatusEffectBehavior
{
    [SerializeField] private GameObject hitVFX;                     // green splat on hit
    //[SerializeField] private GameObject continousCorrosionVFX;       // poison bubbles
    [SerializeField] private SoundEvent hitSFX;                     // corrosive sizzle

    [SerializeField, Range(0f, 1f)] private float initialTickDamageHealthPercent = 0.05f;      // 5% of max HP
    [SerializeField, Range(0f, 1f)] private float reductionEveryTick = 0.50f;                  // damage reduction factor based on current health percentage (50% less damage at 0% health)

    public override void OnApply(in StatusContext ctx)
    {
        Debug.Log($"[Corrosion] Effect applied to {ctx.Target.name} by {ctx.Applier?.name}");
        // Play VFX on hit
        if (hitVFX != null)
        {
            Vector3 contactPoint = ctx.Target.transform.position;
            GameObject vfxInstance = GameObject.Instantiate(hitVFX, contactPoint, Quaternion.identity);
            UnityEngine.Object.Destroy(vfxInstance, 1.0f);
        }
        // play continous corrosion VFX on the target
        //if (continousCorrosionVFX != null)
        //{
        //    continousCorrosionVFX = GameObject.Instantiate(continousCorrosionVFX, ctx.Target.transform);
        //}
        // Play SFX on hit
        if (hitSFX != null)
        {
            hitSFX.PlayAttached(ctx.Target.transform);
        }

        base.OnApply(ctx);
    }

    public override void Tick(in StatusContext ctx)
    {
        if (ctx.Target.TryGetComponent<IDamageable>(out var target))
        {
            if (ctx.Applier != null && ctx.Applier.TryGetComponent<Combat>(out var combat))
            {
                Health health = ctx.Target.GetComponentInParent<Health>();
                if (health == null || health.MaxHP <= 0f) return;

                float healthPercentage = Mathf.Clamp01(health.CurrentHP / health.MaxHP);

                float basePercentDamage = health.MaxHP * initialTickDamageHealthPercent;

                // taper off as health % drops
                float damageMultiplier = 1f - ((1f - healthPercentage) * reductionEveryTick);

                int stacks = Mathf.Max(1, ctx.Stacks);

                float finalDamage = basePercentDamage * damageMultiplier * stacks;

                combat.TryDamage(target, new DamageInfo(finalDamage, DamageType.Magic, ctx.Applier, isProc: true));
            }
        }
    }

    public override void OnStackChanged(in StatusContext ctx, int oldStacks, int newStacks)
    {
        // none
    }

    public override void OnRemove(in StatusContext ctx)
    {
        //UnityEngine.Object.Destroy(continousCorrosionVFX);
        base.OnRemove(ctx);
    }
}