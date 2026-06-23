using UnityEngine;

/// <summary>
/// THROWAWAY. Put on the enemy cube. Injects the target into its attack component
/// directly, so you can test combat without the spawner/pool. Set 'target' in inspector.
/// </summary>
public class TestEnemyDriver : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void Start()
    {
        foreach (var attack in GetComponentsInChildren<AttackBehavior>())
            attack.SetTarget(target);
    }
}