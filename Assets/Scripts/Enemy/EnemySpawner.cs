using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Transform player;      // injected target for attacks, pew pew
    [SerializeField] private int defaultPoolSize = 32;  // needs to be larger for enemies. Change if needed, but keep base 2 for caching.
    [SerializeField] private float navMeshSpawnSampleRadius = 2f;

    private ObjectPool<Enemy> pool;

    private void Awake()
    {
        pool = new ObjectPool<Enemy>(
            createFunc: CreateEnemy,
            actionOnGet: e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: e => Destroy(e.gameObject),
            collectionCheck: true,
            defaultCapacity: defaultPoolSize
        );
    }

    /// <summary>
    /// Instantiates a new enemy and subscribes to its OnDied event. Called by the pool when it needs to create more enemies.
    /// </summary>
    /// <returns></returns>
    private Enemy CreateEnemy()
    {
        Enemy e = Instantiate(enemyPrefab, transform);
        e.OnDied += ReleaseEnemy;   // subscribe ONCE at creation, not per-spawn please.
        return e;
    }

    /// <summary>
    /// Spawns an enemy from the pool at the given position. Resets all IPoolable components
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Enemy Spawn(Vector3 position)
    {
        Enemy enemy = pool.Get();
        Vector3 spawnPosition = GetNavMeshSpawnPosition(position);
        PlaceEnemy(enemy, spawnPosition);

        // 1. (future) apply affixes here - they may raise MaxHP, add components.
        // TODO: affixes :)
        // 2. THEN reset all poolable state (Health.OnSpawn refills to the new MaxHP).
        foreach (var poolable in enemy.GetComponentsInChildren<IPoolable>())
            poolable.OnSpawn();

        // 3. Inject the target into every attack component.
        foreach (var attack in enemy.GetComponentsInChildren<IEnemyAttack>())
            attack.SetTarget(player);

        return enemy;
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
        foreach (var poolable in enemy.GetComponentsInChildren<IPoolable>())
            poolable.OnDespawn();

        foreach (var attack in enemy.GetComponentsInChildren<IEnemyAttack>())
            attack.SetTarget(null);   // safety first! Don't want attacks firing at a dead player's corpse.

        pool.Release(enemy);
    }
}
