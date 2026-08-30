using UnityEngine;
using System;

/// To make a real effect:
///   1. Create -> Scripting -> Status Effect Behavior Script
///   2. Create a Status Effect asset, assign this behaviour to its `behavior` field.
///   3. Apply it from anywhere with:
///        target.Apply(definition, gameObject)
[Serializable]
public class ExampleStatusEffect : StatusEffectBehavior
{
    // ---------- SETTINGS ----------
    // These are per-effect settings which are shared by every entity that has an effect.

    public float valuePerTick = 5f;

    // ---------- PER-INSTANCE STATE ----------
    // This is ONE object shared with every entity with the effect. Could be useful for a gravitaional pull as a cool example.
    // Anything that varies per entity goes in ctx.Instance.State.

    private class RuntimeState
    {
        public int ticksSoFar;
    }

    private static RuntimeState GetState(in StatusContext ctx)
        => (RuntimeState)(ctx.Instance.State ??= new RuntimeState());

    // Fires ONCE, the first time this effect lands on an entity.
    // Does NOT fire on refresh or on any extra stacks. (use OnStackChanged for that)
    // Use for: VFX/SFX on ignite, pushing a stat modifier, initializing state.
    public override void OnApply(in StatusContext ctx)
    {
        var state = GetState(ctx);

        /* useful things on ctx:
         *      ctx.Target              the target the effect is being applied to
         *      ctx.Applier             the thing applying the status effect
         *      ctx.Controller          its StatusEffectController (Has(), Active, etc.)
         *      ctx.Instance            this copy: Stacks, TimeLeft, Source, State
         *      ctx.Stacks              shortcut for ctx.Instance.Stacks
        */

        Debug.Log($"{ctx.Target.name} landed on {ctx.Applier.name} and OnApply() triggered");
    }

    // Fires every 'tickInterval' seconds (from the definition). tickInterval >= 0 or it doesn't fire.
    // May fire more than once in a frame after a hitch...
    // Use for: DOT damage, HOT healing, anything periodic.
    public override void OnTick(in StatusContext ctx)
    {
        var state = GetState(ctx);

        // Scale by stacks
        float amount = valuePerTick * ctx.Stacks;

        // TODO in a real effect: route to whatever thing it needs to update like Health or a DamageInfo
    }

    // Fires when stack count changed on the existing instance
    // Use for: threshold effects ("at 5 stacks, detonate"), scaling VFX.
    public override void OnStackChanged(in StatusContext ctx, int oldStacks, int newStacks)
    {
        Debug.Log($"{ctx.Instance.Def.name} stacks {oldStacks} -> {newStacks}");
        
        // valuePerTick * # of stacks * 2x scalar 
        float additionalBurstDamage = valuePerTick * ctx.Stacks * 2f;
    }
    
    // Fires ONCE when the effect leaves: expiry, explicit Remove, ClearAll.
    // Always paired with OnApply.
    // Use for: undoing whatever OnApply did lol.
    public override void OnRemove(in StatusContext ctx)
    {
        // e.g. entity gets new status effect that makes them take 50% more fire damage for 10 seconds.
        // No need to clear State. Controller discards the instance on expiry.
    }
}
