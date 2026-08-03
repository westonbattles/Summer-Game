using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Pooling")]
    [SerializeField, Min(0)] private int defaultPoolCapacityPerPrefab = 4;
    [SerializeField, Min(1)] private int maxRetainedPerPrefab = 16;

    [Header("Spawning")]
    [SerializeField] private float navMeshSpawnSampleRadius = 2f;
    [SerializeField] private int maxEntitySpawnCap = 40;

    public int MaxEntitySpawnCap => maxEntitySpawnCap;

    private int currentEnemies = 0; // that tracks the amount of active enemies that should count towards the entity cap
    private readonly Dictionary<Enemy, ObjectPool<Enemy>> poolsByPrefab = new(); // gotta seperate pools so we can get the correct enemy
    private readonly Dictionary<Enemy, Enemy> prefabByInstance = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate EnemySpawner found on {name}; destroying it.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private ObjectPool<Enemy> GetOrCreatePool(Enemy enemyPrefab)
    {
        if (poolsByPrefab.TryGetValue(enemyPrefab, out ObjectPool<Enemy> existingPool)) return existingPool;

        ObjectPool<Enemy> newPool = new ObjectPool<Enemy>(
            createFunc: () => CreateEnemy(enemyPrefab),
            actionOnGet: e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: DestroyEnemy,
            collectionCheck: true,
            defaultCapacity: defaultPoolCapacityPerPrefab,
            maxSize: maxRetainedPerPrefab
        );

        poolsByPrefab.Add(enemyPrefab, newPool);
        return newPool;
    }

    /// <summary>
    /// Instantiates a new enemy and subscribes to its OnDied event. Called by the pool when it needs to create more enemies.
    /// </summary>
    /// <param name="enemyPrefab">The prefab owned by the pool requesting a new instance.</param>
    private Enemy CreateEnemy(Enemy enemyPrefab)
    {
        Enemy e = Instantiate(enemyPrefab, transform);
        prefabByInstance.Add(e, enemyPrefab);
        e.OnDied += ReleaseEnemy;   // subscribe ONCE at creation, not per-spawn please.
        return e;
    }

    private void DestroyEnemy(Enemy enemy)
    {
        enemy.OnDied -= ReleaseEnemy;
        prefabByInstance.Remove(enemy);
        Destroy(enemy.gameObject);
    }

    /// <summary>
    /// Spawns the requested enemy prefab from its prefab-specific pool at the given position.
    /// Resets all IPoolable components.
    /// </summary>
    /// <param name="enemyPrefab">The exact enemy prefab to spawn.</param>
    /// <param name="position"></param>
    /// <param name="countTowardSpawnCap"></param>
    /// <returns></returns>
    public Enemy Spawn(Enemy enemyPrefab, Vector3 position, bool countTowardSpawnCap = true)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner cannot spawn a null enemy prefab.", this);
            return null;
        }

        if (countTowardSpawnCap && currentEnemies >= maxEntitySpawnCap) return null;

        Enemy enemy = GetOrCreatePool(enemyPrefab).Get();
        enemy.CountsTowardSpawnCap = countTowardSpawnCap;

        if (enemy.CountsTowardSpawnCap) currentEnemies += 1;
        
        Vector3 spawnPosition = GetNavMeshSpawnPosition(position);
        PlaceEnemy(enemy, spawnPosition);

        // 1. (future) apply affixes here - they may raise MaxHP, add components.
        // TODO: affixes :)
        // 2. THEN reset all poolable state (Health.OnSpawn refills to the new MaxHP).
        foreach (var poolable in enemy.GetComponentsInChildren<IPoolable>())
            poolable.OnSpawn();

        // 3. Inject the target into every attack component.
        Transform target = Player.Instance != null ? Player.Instance.transform : null;
        foreach (var attack in enemy.GetComponentsInChildren<IEnemyAttack>())
            attack.SetTarget(target);

        return enemy;
    }

    /// <summary>
    /// Spawn card spawn wrapper for prefab spawning
    /// </summary>
    public Enemy Spawn(EnemySpawnCard spawnCard, Vector3 position, bool countTowardSpawnCap = true)
    {
        if (spawnCard == null)
        {
            Debug.LogError("EnemySpawner cannot spawn from a null spawn card.", this);
            return null;
        }

        return Spawn(spawnCard.Prefab, position, countTowardSpawnCap);
    }

    private Vector3 GetNavMeshSpawnPosition(Vector3 requestedPosition)
    {
        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, navMeshSpawnSampleRadius, NavMesh.AllAreas))
            return hit.position;

        Debug.LogWarning($"EnemySpawner could not find a NavMesh position near {requestedPosition}. Spawning at the requested position.", this);
        return requestedPosition;
    }

    private void PlaceEnemy(Enemy enemy, Vector3 position)
    {
        if (enemy.TryGetComponent(out NavMeshAgent agent) && agent.Warp(position))
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
            return;
        }

        enemy.transform.position = position;
    }

    /// <summary>
    /// Releases the enemy back to the pool. Called by the enemy itself when it dies, via the OnDied event.
    /// </summary>
    /// <param name="enemy"></param>
    private void ReleaseEnemy(Enemy enemy)
    {
        if (enemy.CountsTowardSpawnCap)
        {
            currentEnemies = Mathf.Max(0, currentEnemies - 1);
            enemy.CountsTowardSpawnCap = false; // reset state just in case
        }
        
        foreach (var poolable in enemy.GetComponentsInChildren<IPoolable>())
            poolable.OnDespawn();

        foreach (var attack in enemy.GetComponentsInChildren<IEnemyAttack>())
            attack.SetTarget(null);   // safety first! Don't want attacks firing at a dead player's corpse.

        if (!prefabByInstance.TryGetValue(enemy, out Enemy enemyPrefab) ||
            !poolsByPrefab.TryGetValue(enemyPrefab, out ObjectPool<Enemy> owningPool))
        {
            Debug.LogError($"EnemySpawner could not find the owning pool for {enemy.name}. Destroying the orphaned instance.", enemy);
            DestroyEnemy(enemy);
            return;
        }

        owningPool.Release(enemy);
    }
}
