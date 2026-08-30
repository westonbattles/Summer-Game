using System;

// Stateless behavior for a status effect that's shared by every instance of a definition.
[Serializable]
public abstract class StatusEffectBehavior
{
    public virtual void OnApply(in StatusContext ctx) { }
    public virtual void OnRemove(in StatusContext ctx) { }
    public virtual void OnTick(in StatusContext ctx) { }
    public virtual void OnStackChanged(in StatusContext ctx, int oldStacks, int newStacks) { }
}


