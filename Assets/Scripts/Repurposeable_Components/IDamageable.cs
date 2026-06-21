using UnityEngine;

// interface that defines the contract for any object that can take damage. This is useful for enemies, players, destructible objects, etc.
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
    bool IsAlive { get; }
}
