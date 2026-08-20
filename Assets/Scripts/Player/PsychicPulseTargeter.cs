using UnityEngine;

public class PsychicPulseTargeter : MonoBehaviour
{
    [Header("VFX Config")]
    [SerializeField] private PooledVfx highlightVFXPrefab;

    [Header("Targeting Config")]
    [SerializeField] private float targetRadius = 10f;
    [SerializeField] private LayerMask targetMask;

    private Targetable currentTarget;
    private PooledVfx activeVFX;

    private void LateUpdate()
    {
        Targetable newTarget = ScanForTarget();

        if (newTarget != currentTarget)
        {
            ChangeTarget(newTarget);
        }

        if (activeVFX != null && currentTarget != null)
        {
            activeVFX.transform.position = currentTarget.TargetPosition;
        }
    }

    private Targetable ScanForTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetRadius, targetMask);

        Targetable bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Targetable targetable))
            {
                float dist = Vector3.Distance(transform.position, targetable.TargetPosition);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestTarget = targetable;
                }
            }
        }

        return bestTarget;
    }

    private void ChangeTarget(Targetable newTarget)
    {
        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(false);

            if (activeVFX != null)
            {
                VFXPoolManager.Instance.Release(activeVFX);
                activeVFX = null;
            }
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(true);
            activeVFX = VFXPoolManager.Instance.Play(
                highlightVFXPrefab,
                currentTarget.TargetPosition,
                Quaternion.identity
            );
        }
    }
}
