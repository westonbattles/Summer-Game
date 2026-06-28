
using System;

[Serializable]
public abstract class AbilityBehaviour
{   
    public virtual AbilityState CreateState() { return null; }

    public abstract bool TryExecute(AbilityState state);              // abstract so its forced
    public virtual void Tick(AbilityState state, float deltaTime) { }
    public virtual void Cancel(AbilityState state) { }                // useful for when you switch characters mid-ability and need to stop the ability
}
