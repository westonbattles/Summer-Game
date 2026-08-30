// The only happy little code that appliers can see.
using UnityEngine;

public interface IStatusEffectTarget
{
    ApplyResult Apply(StatusEffectDefinition def, GameObject applier);
    bool Remove(StatusEffectDefinition def);
    bool Has(StatusEffectDefinition def);
}
