using UnityEngine;

/// Trigger cube for testing. Anything with an IStatusEffectTarget that
/// enters gets the effect. Same call a projectile or the director would make.
public sealed class StatusApplyVolume : MonoBehaviour
{
    [SerializeField] private StatusEffectDefinition effect;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<IStatusEffectTarget>(out var target)) return;

        target.Apply(effect, other.gameObject);
        Debug.Log($"{name} -> {other.name}: {target.Apply(effect, other.gameObject)}");
    }
}