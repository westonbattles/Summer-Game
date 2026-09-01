using UnityEngine;
using System;
using System.Collections.Generic;

public class StatusEffectController : MonoBehaviour, IStatusEffectTarget
{
    [SerializeField] private List<StatusEffectDefinition> immuneTo = new();
    private readonly List<StatusEffectInstance> _active = new();

    // Fires on apply, stack, refresh, and remove.
    public event Action<StatusEffectInstance> OnChanged;
    public IReadOnlyList<StatusEffectInstance> Active => _active;

    public ApplyResult Apply(StatusEffectDefinition def, GameObject applier)
    {
        // immune
        if (immuneTo.Contains(def))
        {
            return ApplyResult.Immune;
        }

        int i = IndexOf(def);

        // not on the target yet
        if (i < 0)
        {
            var instance = new StatusEffectInstance(def, applier);
            _active.Add(instance);
            def.behavior?.OnApply(new StatusContext(instance, this));
            OnChanged?.Invoke(instance);
            return ApplyResult.Applied;
        }

        // already on the target
        var existing = _active[i];
        if (existing.Stacks < def.maxStacks)
        {
            int old = existing.Stacks;
            existing.AddStack();
            def.behavior?.OnStackChanged(new StatusContext(existing, this), old, existing.Stacks);
        }
        else if (def.maxStacks == 1)
        {
            existing.RefreshOldestStack();
        }

        OnChanged?.Invoke(existing);
        return ApplyResult.Applied;
    }

    public bool Remove(StatusEffectDefinition def)
    {
        int i = IndexOf(def);
        if (i < 0) return false;
        RemoveAt(i);
        return true;
    }

    public bool Has(StatusEffectDefinition def) => IndexOf(def) >= 0;

    public void ClearAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
            RemoveAt(i);
    }

    // Updates the clock on all active effects
    private void Update()
    {
        float dt = Time.deltaTime;
        float now = Time.time;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var instance = _active[i];
            var def = instance.Def;
            var ctx = new StatusContext(instance, this);

            instance.TickTimer -= dt;

            int old = instance.Stacks;
            // Remove expired stacks
            while (instance.StackExpiry.Count > 0 && instance.StackExpiry.Peek() <= now)
            {
                instance.StackExpiry.Dequeue();
            }

            // no stacks
            if (instance.Stacks == 0)
            {
                RemoveAt(i);
                continue;
            }

            // notify when a stack has expired
            if (instance.Stacks != old)
            {
                def.behavior?.OnStackChanged(ctx, old, instance.Stacks);
                OnChanged?.Invoke(instance);
            }

            def.behavior?.Tick(ctx);
        }
    }

    // Index based removal. If you find a case for this, I commend you.
    private void RemoveAt(int i)
    {
        var instance = _active[i];
        _active.RemoveAt(i);
        instance.Def.behavior?.OnRemove(new StatusContext(instance, this));
        OnChanged?.Invoke(instance);
    }

    private int IndexOf(StatusEffectDefinition def)
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i].Def == def) return i;
        return -1;
    }
}
