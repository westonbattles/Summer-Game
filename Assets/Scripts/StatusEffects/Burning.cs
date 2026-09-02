using System;
using UnityEngine;

/// "every so often"             Tick
/// "while here, number"         OnApply pushes modifier, OnRemove pops it
/// "once on land / leave"       OnApply / OnRemove
/// Delete the hooks you don't use. Per-entity state goes in ctx.Instance.State, not in fields.
[Serializable]
public sealed class Burning : StatusEffectBehavior
{
    [Header("References")]
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private SoundEvent hitSFX;

    [Header("Settings")]
    [SerializeField] private float tickDamage = 1f;

    public override void OnApply(in StatusContext ctx)
    {
        Debug.Log($"[Burning] Effect applied to {ctx.Target.name} by {ctx.Applier?.name}");
        // Play VFX on hit
        if (hitVFX != null)
        {
            Vector3 contactPoint = ctx.Target.transform.position;

            GameObject vfxInstance = GameObject.Instantiate(hitVFX, contactPoint, Quaternion.identity);
            UnityEngine.Object.Destroy(vfxInstance, 1.0f);
        }
        if (hitSFX != null)
        {
            hitSFX.PlayAttached(ctx.Target.transform);
        }

        base.OnApply(ctx);
    }

    public override void Tick(in StatusContext ctx)
    {
        Debug.Log($"[Burning] Tick dealing {tickDamage} damage to {ctx.Target.name}");
        if (ctx.Target.TryGetComponent<IDamageable>(out var target))
        {
            if (ctx.Applier != null && ctx.Applier.TryGetComponent<Combat>(out var combat))
            {
                combat.TryDamage(target, new DamageInfo(tickDamage, DamageType.Fire, ctx.Applier, isProc: true));
            }
        }
    }

    public override void OnStackChanged(in StatusContext ctx, int oldStacks, int newStacks)
    {
        // none
    }

    public override void OnRemove(in StatusContext ctx)
    {
        // none
    }
}