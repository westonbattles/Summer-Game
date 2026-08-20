using UnityEngine;
#if UNITY_EDITOR
using NaughtyAttributes;
#endif

public class BoltProjectileTrapSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Projectile boltProjectilePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Combat ownerCombat;

    [Header("Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private DamageInfo damageInfo;

    private void Awake()
    {
        if (ownerCombat == null)
        {
            ownerCombat = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Combat>();
        }
    }

    public void SpawnBolt()
    {
        if (boltProjectilePrefab == null)
        {
            Debug.LogWarning($"[BoltProjectileTrapSpawner] Prefab missing on {gameObject.name}");
            return;
        }

        if (ProjectilePoolManager.Instance == null)
        {
            Debug.LogError("[BoltProjectileTrapSpawner] ProjectilePoolManager instance not found!");
            return;
        }

        Transform launchPoint = spawnPoint != null ? spawnPoint : transform;
        Vector3 origin = launchPoint.position;
        Vector3 direction = -launchPoint.forward;

        Projectile projectile = ProjectilePoolManager.Instance.Get(boltProjectilePrefab);

        projectile.Launch(ownerCombat, origin, direction, speed, damageInfo);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Transform drawTransform = spawnPoint != null ? spawnPoint : transform;

        Gizmos.DrawWireSphere(drawTransform.position, 0.1f);            // spawn point
        Gizmos.DrawRay(drawTransform.position, drawTransform.forward);  // spawn direction
    }

#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowIf(nameof(showDebugButtons))]
    [Button("Spawn Bolt Projectile", EButtonEnableMode.Playmode)]
    private void TestSpawnBolt()
    {
        SpawnBolt();
    }
    #endregion
#endif
}
