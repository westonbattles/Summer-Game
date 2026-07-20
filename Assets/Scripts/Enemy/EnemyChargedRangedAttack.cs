using System;
using UnityEngine;

public class EnemyChargedRangedAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField] private DamageType damageType = DamageType.Projectile;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float damage = 16f;
    [SerializeField] private float cooldown = 2.5f;
    [SerializeField] private float maxRange = 14f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private GameObject chargeVFXPrefab;

    private Combat ownerCombat;
    private Transform target;
    private float cooldownRemaining;
    private float chargeRemaining;
    private bool isCharging;
    private GameObject activeChargeVFX;

    void Awake()
    {
        ownerCombat = GetComponent<Combat>();
        if (ownerCombat == null) Debug.LogError($"Could not find combat script on gameobject: {gameObject}");
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Update()
    {
        if (cooldownRemaining > 0f)
            cooldownRemaining -= Time.deltaTime;
        if (!isCharging) return;

        if (target == null)
        {
            Cancel();
            return;
        }

        chargeRemaining -= Time.deltaTime;
        if (chargeRemaining <= 0f)
            Fire();
    }

    public bool TryExecute()
    {
        if (isCharging || cooldownRemaining > 0f) return false;
        if (target == null) return false;
        if (Vector3.Distance(transform.position, target.position) > maxRange) return false;

        isCharging = true;
        chargeRemaining = chargeDuration;

        if (chargeVFXPrefab != null)
            activeChargeVFX = Instantiate(chargeVFXPrefab, firePoint.position, firePoint.rotation, firePoint);

        return true;
    }

    /// <summary>
    /// Cancels the charge if it's in progress. No cooldown is applied, as this is not a completed attack.
    /// </summary>
    public void Cancel()
    {
        if (!isCharging) return;

        isCharging = false;
        chargeRemaining = 0f;

        if (activeChargeVFX != null)
        {
            Destroy(activeChargeVFX);
            activeChargeVFX = null;
        }
        // no cooldown applied here. interrupted charges aren't used attacks.
    }

    /// <summary>
    /// Fires the projectile at the target. Called when charge is complete.
    /// </summary>
    private void Fire()
    {
        isCharging = false;

        if (activeChargeVFX != null)
        {
            Destroy(activeChargeVFX);
            activeChargeVFX = null;
        }

        // re-aim at fire time. Leads ahead towards where the ttarget is now, not at charge start.
        Vector3 direction = (target.position - firePoint.position).normalized;
        Projectile proj = ProjectilePoolManager.Instance.Get(projectilePrefab);
        proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(direction));
        proj.Launch(ownerCombat, transform.position, direction, projectileSpeed, new DamageInfo(damage, damageType, gameObject));

        cooldownRemaining = cooldown;
    }
}
