using UnityEngine;

/// <summary>
/// THROWAWAY. Put on a cube to act as the "player" target. Has Health, logs hits.
/// Sit it inside the enemy's proximityToAttack range and watch the console.
/// </summary>
[RequireComponent(typeof(Health))]
public class TestTarget : MonoBehaviour
{
    private Health health;

    private void Awake() => health = GetComponent<Health>();

    private void OnEnable()
    {
        health.OnHealthChanged += (cur, max) => Debug.Log($"[Target] took a hit -> {cur:0.#}/{max:0.#}");
        health.OnDeath += _ => Debug.Log("[Target] DIED");
    }
}