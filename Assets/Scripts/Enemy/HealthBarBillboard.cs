using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class HealthBarBillboard : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image healthFillImage;   // only value that needs to be set in editor
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private bool hideWhenDead = true;

    private Transform target;
    private Transform camTransform;

    private void Awake()
    {
        target = transform.parent;

        if (health == null)
        {
            health = target.GetComponentInParent<Health>(); // should always be attached to a canvas parented to the health script owner
        }

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(health.CurrentHP, health.MaxHP);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + worldOffset;

        if (camTransform != null)
        {
            transform.forward = camTransform.forward;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        float pct = max > 0f ? current / max : 0f;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = pct;
        }

        bool deadHide = hideWhenDead && current <= 0f;
        bool fullHide = hideWhenFull && pct >= 1f;

        gameObject.SetActive(!(deadHide || fullHide));
    }
}