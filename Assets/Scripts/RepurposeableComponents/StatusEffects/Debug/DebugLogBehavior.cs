using System;
using UnityEngine;

/// Step 2 of the plan: proves apply / tick / expire / stack in the console.
[Serializable]
public sealed class DebugLogBehavior : StatusEffectBehavior
{
    public override void OnApply(in StatusContext ctx)
        => Debug.Log($"{ctx.Instance.Def.name} + {ctx.Target.name}");

    public override void Tick(in StatusContext ctx)
        => Debug.Log($"{ctx.Instance.Def.name} tick x{ctx.Stacks}");

    public override void OnStackChanged(in StatusContext ctx, int oldStacks, int newStacks)
        => Debug.Log($"{ctx.Instance.Def.name} stacks {oldStacks} -> {newStacks}");

    public override void OnRemove(in StatusContext ctx)
        => Debug.Log($"{ctx.Instance.Def.name} - {ctx.Target.name}");
}
