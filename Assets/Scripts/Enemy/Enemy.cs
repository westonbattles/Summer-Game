using UnityEngine;

/// <summary>
/// Base for all enemies (including bosses). Composition-first: this holds only
/// what every enemy shares for pooling/generalization. HP lives on a Health
/// component; attacks/affixes are separate components added per-enemy.
/// </summary>
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [field: SerializeField] public int XPValue { get; private set; }
    [field: SerializeField] public bool AffixEligible { get; private set; }

    private Health health;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
    }

    private void HandleDeath(DamageInfo killingBlow)
    {
        // TODO: grant XPValue (credit killingBlow.Source), spawn drops,
        //       return to pool instead of Destroy.
    }
}
