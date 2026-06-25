using UnityEngine;
// this is what goes on the player. Owns the 4 instances, ticks them, reads input
// This is the sauceeeee.

[RequireComponent(typeof(PlayerInputReader))]
public class AbilityController : MonoBehaviour
{
    [SerializeField] AbilityDefinition primary, secondary, movement, special;
    [SerializeField] Transform aimOrigin;   // muzzle or camera, not sure how you wanted to hook this up weston. Feel free to remove this comment after reading. - jake

    readonly AbilityInstance[] _slots = new AbilityInstance[4];
    PlayerInputReader _input;

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _slots[0] = Make(primary);
        _slots[1] = Make(secondary);
        _slots[2] = Make(movement);
        _slots[3] = Make(special);
    }

    /// <summary>
    /// Creates an AbilityInstance for the given AbilityDefinition.
    /// </summary>
    /// <param name="def"></param>
    /// <returns></returns>
    AbilityInstance Make(AbilityDefinition def)
    {
        if (def != null)
        {
            return new AbilityInstance(def);
        }
        return null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _slots.Length; i++)
        {
            var inst = _slots[i];
            if (inst == null) continue;

            inst.Tick(dt);
            if (WasSlotPressed((AbilitySlot)i) && inst.IsReady)
            {
                var ctx = BuildContext(inst.Definition);
                inst.TryActivate(in ctx);
            }
        }
    }

    /// <summary>
    /// Builds an AbilityContext for the given AbilityDefinition. 
    /// This context includes the origin and direction of the ability, as well as the base damage defined in the ability.
    /// Uses factory pattern.
    /// </summary>
    /// <param name="def"></param>
    /// <returns></returns>
    AbilityContext BuildContext(AbilityDefinition def)
    {
        // TODO (aim): pull origin/direction from your real aim source.
        // Placeholer uses aimOrigin's forward, but I know you're really skilled with controllers, and so I trust the expert here :)
        Transform src = aimOrigin != null ? aimOrigin : transform;
        return new AbilityContext(gameObject, src.position, src.forward, def.baseDamage);
    }

    /// <summary>
    /// Checks if the ability slot was pressed this frame.
    /// </summary>
    /// <param name="slot"></param>
    /// <returns></returns>
    bool WasSlotPressed(AbilitySlot slot)
    {
        // TODO (input): add 4 actions to the project input asset, expose them on
        // PlayerInputReader liek Move/Look/Jump, then return the matching pressed flag here.
        return false;
    }

    /// <summary>
    /// Gets the AbilityInstance for the given AbilitySlot. 
    /// This is used for UI purposes to display the current state of the ability (e.g., charges, cooldown).
    /// </summary>
    /// <param name="slot"></param>
    /// <returns></returns>
    public AbilityInstance GetInstance(AbilitySlot slot) => _slots[(int)slot];  // for UI
}
