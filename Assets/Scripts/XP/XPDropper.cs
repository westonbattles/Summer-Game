using UnityEngine;

/// <summary>
/// Put this on anything that grants XP. If a Health exists on the same GaomeObject, it will automatically drop XP on death. Otherwise, call Drop() manually.
/// Needs a reference to any global system.
/// </summary>
[DisallowMultipleComponent]
public class XPDropper : MonoBehaviour
{
    [SerializeField] private XPOrb orbPrefab;
    [SerializeField] private Vector3 spawnOffset = new(0f, 0.75f, 0f);
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
        Vector3 spawnPosition = transform.position + spawnOffset;
        XPOrb orb;

        if (VFXPoolManager.Instance == null)
        {
            Debug.LogWarning("XPDropper: no VFXPoolManager found. Instantiating XP orb without pooling.", this);
            orb = Instantiate(orbPrefab, spawnPosition, Quaternion.identity);
            orb.Launch(Value);
            return;
        }

        if (!orbPrefab.TryGetComponent(out PooledVfx pooledVfxPrefab))
        {
            Debug.LogWarning($"XPDropper: {orbPrefab.name} has no PooledVfx component. Instantiating XP orb without pooling.", orbPrefab);
            orb = Instantiate(orbPrefab, spawnPosition, Quaternion.identity);
            orb.Launch(Value);
            return;
        }

        PooledVfx pooledVfx = VFXPoolManager.Instance.Play(pooledVfxPrefab, spawnPosition, Quaternion.identity);
        if (pooledVfx == null) return;

        if (!pooledVfx.TryGetComponent(out orb))
        {
            Debug.LogError($"XPDropper: pooled VFX '{pooledVfx.name}' has no XPOrb component.", pooledVfx);
            VFXPoolManager.Instance.Release(pooledVfx);
            return;
        }

        orb.Launch(Value, true);
    }
}
