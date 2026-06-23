using UnityEngine;

/// <summary>
/// Implemented by any component that needs to reset its state when its GameObject
/// is reused from a pool. The spawner calls OnSpawn after Get and OnDespawn before
/// Release, on every IPoolable on the object. Each component resets only its own state.
/// </summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
