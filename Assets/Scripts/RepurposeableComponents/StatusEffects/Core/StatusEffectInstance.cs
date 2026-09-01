// on the entity
using UnityEngine;
using System.Collections.Generic;

public class StatusEffectInstance
{
    public readonly StatusEffectDefinition Def;
    public GameObject Applier;
    public float TickTimer;
    public object State;    // per-instance

    public readonly Queue<float> StackExpiry = new();
    public int Stacks => StackExpiry.Count;

    public StatusEffectInstance(StatusEffectDefinition def, GameObject applier)
    {
        Def = def;
        Applier = applier;
        TickTimer = def.tickInterval;
    }

    public void AddStack()
    {
        StackExpiry.Enqueue(Def.duration > 0f ? Time.time + Def.duration : float.PositiveInfinity);
    }

    public void RefreshOldestStack()
    {
        if (StackExpiry.Count > 0)
            StackExpiry.Dequeue();
        AddStack();
    }

    // Seconds until the oldest stack drops. Inf for perma effect
    // use OldestStackTimeLeft for the icon countdown on the UI
    public float OldestStackTimeLeft => StackExpiry.Count > 0 ? StackExpiry.Peek() - Time.time : 0f;
}
