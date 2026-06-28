using UnityEngine;

// another asset, immutable at runtime!!
// the two loadouts we make with it will be thus used to create mirrored runtime instances that we can mutilate (mut!!!) as needed yayyyy pog champ!!! woo.... oh my god i am so tired ive been working this out all day fml AHHHHHH FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK FUCK  ok lock in time
[CreateAssetMenu(fileName = "New Ability Loadout", menuName = "Abilities/Loadout")]
public class AbilityLoadout : ScriptableObject
{
    [Header("Starting Loadout")]
    [SerializeField] Ability primary;
    [SerializeField] Ability secondary;
    [SerializeField] Ability movement;
    [SerializeField] Ability special;

    public Ability GetAbility(AbilitySlot slot)
    {
        switch (slot)
        {
            case AbilitySlot.Primary: return primary;
            case AbilitySlot.Secondary: return secondary;
            case AbilitySlot.Movement: return movement;
            case AbilitySlot.Special: return special;
            default: return null;
        }
    }

    public Ability GetAbility(int i)
    {
        return GetAbility( (AbilitySlot) i ); // https://stackoverflow.com/questions/29482/how-do-i-cast-int-to-enum-in-c
    }
}

// mirrored instances attatched to seperate assets that get created with the above scriptable object, similar to Ability / AbiltiyInstance in Ability.cs
sealed class AbilityLoadoutInstance
{
    const int Slots = AbilityController.Slots;
    
    Ability[] _abilities = new Ability[Slots];
    AbilityInstance[] _instances = new AbilityInstance[Slots];

    public AbilityLoadoutInstance(AbilityLoadout source)
    {
        for (int i = 0; i < Slots; i++)
        {
            _abilities[i] = source.GetAbility(i);
            _instances[i] = _abilities[i]?.CreateInstance(); // _abilities[i] could be null in theory if slots != AbilitySlot enum length, but that shouldn't ever happen... right guys? 
        }
    }
        
    public Ability GetAbility(AbilitySlot slot)
    {
        return _abilities[(int) slot]; // my goat https://stackoverflow.com/questions/29482/how-do-i-cast-int-to-enum-in-c
    }

    //
    public AbilityInstance GetAbilityInstance(AbilitySlot slot)
    {
        return _instances[(int)slot];
    }

    // replaces ability in a given slot with the given ability
    public void ReplaceAbility(Ability ability, AbilitySlot slot)
    {
        int i = (int) slot;
        // maybe stop the ability being swapped out with .Cancel() before swapping? or maybe not its prob fine
        _abilities[i] = ability;
        _instances[i] = ability?.CreateInstance(); // i guess if we want to replace a slot with null it means we just deleting the ability from existence lollll, i think thats ok
    }

    public void TickAbilities(float deltaTime)
    {
        for (int i = 0; i < Slots; i++)
        {
            _instances[i]?.Tick(deltaTime); // don't tick null abilities
        }
    }

    public void CancelAbilities()
    {
        for (int i = 0; i < Slots; i++)
        {
            _instances[i]?.Cancel();
        }
    }
}

