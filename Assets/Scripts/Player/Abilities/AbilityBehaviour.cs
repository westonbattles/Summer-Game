
using System;

[Serializable]
public abstract class AbilityBehaviour
{   
    public virtual AbilityState CreateState() { return null; }

    public abstract bool TryExecute(AbilityState state);              // abstract so its forced
    public virtual void Tick(AbilityState state, float deltaTime) { }
    public virtual void Cancel(AbilityState state) { }                // useful for when you switch characters mid-ability and need to stop the ability

    // UI (not forced to be overridden) (default returns as follows)
    public virtual float GetCooldownFraction(AbilityState state) { return 0f; } // 0 = ready, 1 = just used
    public virtual int GetCurrentCharges(AbilityState state) { return 1; } // 0 = no charges, 1 = 1 charge, etc
    public virtual int GetMaxCharges(AbilityState state) { return 1; } // 0 = no charges, 1 = 1 charge, etc
}
