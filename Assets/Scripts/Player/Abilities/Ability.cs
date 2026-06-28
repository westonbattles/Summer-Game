using UnityEngine;
// this is the data asset. It's shared and note immutable at runtime!!!
// these abilities will be thus used to create mirrored runtime instances that will have custom functionality attached
[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/New Ability")]
public class Ability : ScriptableObject
{
    [Header("Info/Display")]
    //public AbilitySlot slot;    // primary, secondary, movement, special. (thats literally the entire enum) - note from weston: we don't need to keep track of the slot since abilities are slot-agnostic meaning only the inventory/ability controller systems need to know which slot the ability is in probably. can change if we want, very ez fix
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;         // not sure if this needs to change to something UI based later.

    [Header("Behaviour")]
    [SerializeReference] 
    [SerializeField]
    AbilityBehaviour behaviour; // this is how our ability asset knows which behavior to use

    public AbilityInstance CreateInstance()
    {
        if (behaviour == null)
        {
            Debug.LogError($"{name} does not have an ability behaviour.", this);
            return null;
        }

        return new AbilityInstance(this, behaviour);
    }
}


// mirrored instance of the Ability class - can be used during runtime to run monobehaviour functionality attached to each ability
public sealed class AbilityInstance
{
    public Ability Ability { get; }

    AbilityBehaviour _behaviour;
    AbilityState _state;

    public AbilityInstance(Ability ability, AbilityBehaviour behaviour)
    {
        Ability = ability;
        _behaviour = behaviour;
        _state = behaviour.CreateState();
    }

    public bool TryExecute()
    {
        return _behaviour.TryExecute(_state);
    }

    public void Tick(float deltaTime)
    {
        _behaviour.Tick(_state, deltaTime);
    }

    public void Cancel()
    {
        _behaviour.Cancel(_state);
    }

}
