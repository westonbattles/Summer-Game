using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THROWAWAY test driver for the enemy spawn system. Drives the REAL EnemySpawner,
/// so it exercises pooling, reset, and target injection — not just raw Instantiate.
///
/// Setup:
///   1. Put this on an empty GameObject in the scene.
///   2. Assign the EnemySpawner reference.
///   3. Drop a few empty GameObjects in the scene as spawn points, assign them.
///   4. Press Play.
///
/// Controls (legacy Input — works on Unity's default/Both input setup):
///   [Space] spawn one at a random spawn point
///   [B]     spawn a burst (burstCount at once)
///   auto    if autoSpawn is on, also spawns every 'interval' seconds
///
/// Uses the new Input System (UnityEngine.InputSystem). Reads Keyboard.current
/// directly — no Input Actions asset needed for a throwaway tester.
///
/// Delete this once the spawn loop is proven.
/// </summary>
public class TestSpawnDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Auto spawn")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float interval = 2f;

    [Header("Burst (B key)")]
    [SerializeField] private int burstCount = 5;

    [Header("Spawn placement")]
    [Tooltip("Random offset added around the chosen spawn point so they don't stack.")]
    [SerializeField] private float scatterRadius = 1.5f;

    private float timer;

    private void Update()
    {
        if (autoSpawn)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = interval;
                SpawnOne();
            }
        }

        // New Input System: read the keyboard device directly. Null-guard in case
        // no keyboard is present (e.g. some test/CI contexts).
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame)
            SpawnOne();

        if (kb.bKey.wasPressedThisFrame)
            for (int i = 0; i < burstCount; i++)
                SpawnOne();
    }

    private void SpawnOne()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[TestSpawnDriver] No EnemySpawner assigned.", this);
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[TestSpawnDriver] No spawn points assigned.", this);
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Scatter on the XZ plane so a burst doesn't spawn everything on one spot.
        Vector2 jitter = Random.insideUnitCircle * scatterRadius;
        Vector3 pos = point.position + new Vector3(jitter.x, 0f, jitter.y);

        spawner.Spawn(pos);
    }

    // Visualize spawn points + scatter radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var p in spawnPoints)
        {
            if (p == null) continue;
            Gizmos.DrawWireSphere(p.position, scatterRadius);
        }
    }
}