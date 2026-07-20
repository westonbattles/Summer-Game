using System;
using UnityEngine;

[RequireComponent(typeof(Combat))]
[RequireComponent(typeof(EnemyAttackController))]
public class EnemyRangedAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField] private DamageType damageType = DamageType.Projectile;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float damage = 8f;
    [SerializeField] private float cooldown = 1.5f;
    [SerializeField] private float maxRange = 12f;
    [SerializeField] private float projectileSpeed = 10f;

    private Combat ownerCombat;
    private Transform target;
    private float cooldownRemaining;

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
    }

    public bool TryExecute()
    {
        if (cooldownRemaining > 0f)
        {
            return false;
        }
        if (target == null)
        {
            return false;
        }
        if (Vector3.Distance(transform.position, target.position) > maxRange)
        {
            return false;
        }

        Vector3 direction = (target.position + Vector3.up - firePoint.position).normalized;
        Projectile proj = ProjectilePoolManager.Instance.Get(projectilePrefab);
        proj.Launch(ownerCombat, firePoint.position, direction, projectileSpeed, new DamageInfo(damage, damageType, gameObject));

        cooldownRemaining = cooldown;
        return true;
    }
    
    // do not need cancel.
}
