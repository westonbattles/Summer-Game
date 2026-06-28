using System.Collections.Generic;
using UnityEngine;


// goes on the player, handles swapping 
public class AbilityController : MonoBehaviour
{

    public const int Slots = 4;
    
    [SerializeField] AbilityLoadout startingLoadout;
    Dictionary<AbilityLoadout, AbilityLoadoutInstance> _loadoutInstances = new(); // map that 
    
    public AbilityLoadout CurrentLoadout { get; private set; }
    AbilityLoadoutInstance _currentLoadoutInstance;

    void Awake()
    {
        SetLoadout(startingLoadout);
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        
        // tick every ability in either loadout so we keep their cooldown counters counting 
        foreach (AbilityLoadoutInstance loadout in _loadoutInstances.Values)
        {
            loadout.TickAbilities(deltaTime);
        }
    }

    void SetLoadout(AbilityLoadout loadout)
    {

        if (loadout == CurrentLoadout) return;

        _currentLoadoutInstance?.CancelAbilities();
        
        CurrentLoadout = loadout;
        _currentLoadoutInstance = loadout != null ? GetOrCreateLoadoutInstance(loadout) : null;
    }

    public bool ExecuteAbility(AbilitySlot slot)
    {
        AbilityInstance instance = _currentLoadoutInstance?.GetAbilityInstance(slot);
        return instance != null && instance.TryExecute();
    }

    public Ability GetCurrentAbility(AbilitySlot slot)
    {
        return _currentLoadoutInstance?.GetAbility(slot);
    }
    
    AbilityLoadoutInstance GetOrCreateLoadoutInstance(AbilityLoadout loadout)
    {
        if (_loadoutInstances.TryGetValue(loadout, out AbilityLoadoutInstance instance))
        {
            return instance;
        }

        instance = new AbilityLoadoutInstance(loadout);
        _loadoutInstances.Add(loadout, instance); // store it in the dict of our instances
        return instance;
    }
}
