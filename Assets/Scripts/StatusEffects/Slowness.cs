using System;
using UnityEngine;

/// Delete the hooks you don't use. Per-entity state goes in ctx.Instance.State, not in fields.
[Serializable]
public sealed class Slowness : StatusEffectBehavior
{
    [SerializeField] private float baseSlowAmount = 5f;

    public override void OnApply(in StatusContext ctx)
    {
        if (ctx.Target.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.movementSpeed = baseSlowAmount * ctx.Stacks;
        }
        base.OnApply(ctx);
    }

    public override void OnStackChanged(in StatusContext ctx, int oldStacks, int newStacks)
    {
        if (ctx.Target.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.movementSpeed = baseSlowAmount * newStacks;
        }
        base.OnStackChanged(ctx, oldStacks, newStacks);
    }

    public override void OnRemove(in StatusContext ctx)
    {
        base.OnRemove(ctx);
    }
}