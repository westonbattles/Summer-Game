using UnityEngine;


public class PsychicPulseHighlighter : MonoBehaviour
{
    GameObject owner;
    Enemy enemyOwner;
    PooledVfx activeVFX;
    Transform cameraTransform;
    bool isHighlighted;


    void Awake()
    {
        owner = gameObject;
        TryGetComponent(out enemyOwner);

        if (Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!isHighlighted || activeVFX == null) return;

        // non enemy can be highlighted as well
        activeVFX.transform.position = enemyOwner != null ? enemyOwner.GetCenterPoint() : owner.transform.position;

        if (cameraTransform != null) activeVFX.transform.forward = cameraTransform.forward; // rotation of vfx aligns with camera
    }

    public void Highlight(PooledVfx highlightVFXPrefab)
    {
        if (isHighlighted) return;
        isHighlighted = true;

        Vector3 transformPosition = enemyOwner != null ? enemyOwner.GetCenterPoint() : owner.transform.position;
        activeVFX = VFXPoolManager.Instance.Play(highlightVFXPrefab, transformPosition, Quaternion.identity);
    }

    public void Unhighlight()
    {

        // TODO: FADE HIGHLIGHT SO IT DOESN'T DISSAPEAR?

        if (!isHighlighted) return;
        isHighlighted = false;

        if (activeVFX != null)
        {
            VFXPoolManager.Instance.Release(activeVFX);
            activeVFX = null;
        }

    }

}