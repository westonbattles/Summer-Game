using UnityEngine;

/// <summary>
/// Put this on anything that grants XP. If a Health exists on the same GaomeObject, it will automatically drop XP on death. Otherwise, call Drop() manually.
/// Needs a reference to any global system.
/// </summary>
[DisallowMultipleComponent]
public class XPDropper : MonoBehaviour
{
    [SerializeField] private XPOrb orbPrefab;
    [field: SerializeField] public int Value { get; private set; } = 10;

    /// <summary>Drops an orb worth the given value. Enemies pass their own XPValue through this.</summary>
    public void Drop()
    {
        if (orbPrefab == null)
        {
            Debug.LogWarning($"XPDropper: {name} XPDropper has no orb prefab assigned.", this);
            return;
        }

        // TODO: run director multiplier applies here once that system exists.
        // TODO: swap for pool manager once ProjectilePoolManager is generalized:
        // XPOrb orb = PoolManager.Instance.Get(orbPrefab, transform.position);
        XPOrb orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
        orb.Launch(Value);
    }
}
