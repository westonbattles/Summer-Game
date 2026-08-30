using System;
using UnityEngine;

// The result of applying a status effect to a target
public enum ApplyResult
{
    Applied,    // added, stacked, or refreshed
    Immune
}

// Every behavior hook gets this
public readonly struct StatusContext
{
    public readonly StatusEffectInstance Instance;
    public readonly StatusEffectController Controller;

    public StatusContext(StatusEffectInstance i, StatusEffectController c)
    {
        Instance = i;
        Controller = c;
    }

    public GameObject Target => Controller.gameObject;
    public GameObject Applier => Instance.Applier;
    public int Stacks => Instance.Stacks;
}
