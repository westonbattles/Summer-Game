using NaughtyAttributes;
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
            actionOnGet: null,
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
        Enemy e = Instantiate(enemyPrefab, pendingSpawnPosition, enemyPrefab.transform.rotation, transform);
        e.gameObject.SetActive(false);
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

        if (!TryGetNavMeshSpawnPosition(enemyPrefab, position, out Vector3 spawnPosition)) return null;

        ObjectPool<Enemy> owningPool = GetOrCreatePool(enemyPrefab);
        pendingSpawnPosition = spawnPosition;
        Enemy enemy = owningPool.Get();

        if (!PlaceEnemy(enemy, spawnPosition))
        {
            owningPool.Release(enemy);
            return null;
        }

        enemy.CountsTowardSpawnCap = countTowardSpawnCap;

        if (enemy.CountsTowardSpawnCap) currentEnemies += 1;

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

    private Vector3 pendingSpawnPosition;

    private bool TryGetNavMeshSpawnPosition(Enemy enemyPrefab, Vector3 requestedPosition, out Vector3 spawnPosition)
    {
        spawnPosition = requestedPosition;

        if (!enemyPrefab.TryGetComponent(out NavMeshAgent agent)) return true;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, navMeshSpawnSampleRadius, filter))
        {
            spawnPosition = hit.position;
            return true;
        }

        Debug.LogWarning($"EnemySpawner could not find a NavMesh position near {requestedPosition}. Spawn cancelled.", this);
        return false;
    }

    private bool PlaceEnemy(Enemy enemy, Vector3 position)
    {
        enemy.transform.position = position;
        enemy.gameObject.SetActive(true);

        if (!enemy.TryGetComponent(out NavMeshAgent agent)) return true;

        if (agent.isOnNavMesh && agent.Warp(position))
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
            return true;
        }

        Debug.LogWarning($"EnemySpawner could not place {enemy.name} on the NavMesh at {position}. Spawn cancelled.", enemy);
        enemy.gameObject.SetActive(false);
        return false;
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

#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowIf(nameof(showDebugButtons))]
    [SerializeField] private Enemy debugEnemyPrefab;

    [ShowIf(nameof(showDebugButtons))]
    [SerializeField] private float debugSpawnDistance = 5f;

    [ShowNativeProperty] private int DebugActiveEnemyCount => currentEnemies;
    [ShowNativeProperty] private int DebugPoolCount => poolsByPrefab.Count;

    [ShowIf(nameof(showDebugButtons))]
    [Button("Spawn Debug Enemy", EButtonEnableMode.Playmode)]
    public void TestSpawn()
    {
        if (debugEnemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner - Debug]: Assign a debug enemy prefab first.", this);
            return;
        }

        Vector3 pos = Player.Instance != null
            ? Player.Instance.transform.position + Player.Instance.transform.forward * debugSpawnDistance
            : transform.position;

        Spawn(debugEnemyPrefab, pos);
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Spawn Debug Enemy (Ignore Cap)", EButtonEnableMode.Playmode)]
    private void TestSpawnIgnoreCap()
    {
        if (debugEnemyPrefab == null) return;
        Vector3 pos = Player.Instance != null
            ? Player.Instance.transform.position + Player.Instance.transform.forward * debugSpawnDistance
            : transform.position;
        Spawn(debugEnemyPrefab, pos, countTowardSpawnCap: false);
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Fill to Spawn Cap", EButtonEnableMode.Playmode)]
    private void TestFillToCap()
    {
        if (debugEnemyPrefab == null) return;
        while (currentEnemies < maxEntitySpawnCap)
        {
            Vector3 offset = UnityEngine.Random.insideUnitSphere * debugSpawnDistance;
            offset.y = 0f;
            Vector3 pos = (Player.Instance != null ? Player.Instance.transform.position : transform.position) + offset;
            if (Spawn(debugEnemyPrefab, pos) == null)
            {
                Debug.LogWarning("[EnemySpawner - Debug]: Could not spawn more enemies. Possibly hit the spawn cap or no valid NavMesh position.", this);
                break;
            }
        }
    }
    #endregion
#endif
}
