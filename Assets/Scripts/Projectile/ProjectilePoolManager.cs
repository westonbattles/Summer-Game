using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Central registry of projectile pools, keyed by prefab. Any attack can
/// request a projectile of a given type without knowing or caring whether a pool
/// for that type already exists. First request for a prefab lazily creates its pool.
/// </summary>
public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    private readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // dont make a second instance if one alr exists
            return;
        }
        Instance = this;
    }

    /// <summary> Returns a projectile instance from the pool for the given prefab. If no pool exists for that prefab, one is created. </summary>
    public Projectile Get(Projectile prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = CreatePool(prefab);
            pools[prefab] = pool;
        }
        return pool.Get();
    }

    /// <summary> Creates a new object pool for the given projectile prefab. </summary>
    private ObjectPool<Projectile> CreatePool(Projectile prefab)
    {
        ObjectPool<Projectile> pool = null;

        pool = new ObjectPool<Projectile>(
            createFunc: () =>
            {
                Projectile instance = Instantiate(prefab, transform);
                instance.Initialize(p => pool.Release(p));
                return instance;
            },
            actionOnGet: p =>
            {
                p.gameObject.SetActive(true);
                p.OnSpawn();
            },
            actionOnRelease: p =>
            {
                p.OnDespawn();
                p.gameObject.SetActive(false);
            },
            actionOnDestroy: p => Destroy(p.gameObject),
            collectionCheck: true,
            defaultCapacity: prefab.PoolCapacity
        );

        return pool;
    }
}
