// on the entity
using UnityEngine;

public class StatusEffectInstance
{
    public readonly StatusEffectDefinition Def;
    public GameObject Applier;
    public int Stacks = 1;
    public float TimeLeft;
    public float TickTimer;
    public object State;    // per-instance

    public StatusEffectInstance(StatusEffectDefinition def, GameObject applier)
    {
        Def = def;
        Applier = applier;
        TimeLeft = def.duration;
        TickTimer = def.tickInterval;
    }

    // returns true when expired and is then removed by the controller
    public bool Tick(in StatusContext ctx, float dt)
    {
        if (Def.tickInterval > 0f)
        {
            TickTimer -= dt;
            while (TickTimer <= 0f)
            {
                Def.behavior?.OnTick(ctx);
                TickTimer += Def.tickInterval;
            }
        }

        if (Def.duration <= 0f) return false;
        TimeLeft -= dt;
        return TimeLeft <= 0f;
    }
}
