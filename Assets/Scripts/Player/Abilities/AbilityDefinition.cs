using System;
using UnityEngine;
// this is the data asset. It's shared and note immutable at runtime!!!

[CreateAssetMenu(fileName = "Abilities/Ability Definition", menuName = "NewAbility")]
public class AbilityDefinition : ScriptableObject
{
    public AbilitySlot slot;    // primary, secondary, movement, special. (thats literally the entire enum)
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;         // not sure if this needs to change to something UI based later.

    [Min(0f)] public float cooldown = 0f;       // seconds per charge; 0 = always ready
    [Min(1)] public int maxCharges = 1;         // 1 = single use, 2 = double use
    public float baseDamage = 0f;               // bheadline number; Source/crit assembled at runtime

    [SerializeReference] public AbilityEffect[] effects = Array.Empty<AbilityEffect>();

    public void Execute(in AbilityContext ctx)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i]?.Execute(in ctx);
        }
    }
}
