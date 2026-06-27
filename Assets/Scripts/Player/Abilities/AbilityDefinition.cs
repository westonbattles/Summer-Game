using System;
using UnityEngine;
// this is the data asset. It's shared and note immutable at runtime!!!

[CreateAssetMenu(fileName = "Abilities/Ability Definition", menuName = "NewAbility")]
public class AbilityDefinition : ScriptableObject
{
    //public AbilitySlot slot;    // primary, secondary, movement, special. (thats literally the entire enum) - note from weston: we don't need to keep track of the slot since abilities are slot-agnostic meaning only the inventory/ability controller systems need to know which slot the ability is in probably. can change if we want, very ez fix
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;         // not sure if this needs to change to something UI based later.
}
