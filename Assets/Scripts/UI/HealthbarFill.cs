using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthbarFill : MonoBehaviour
{
    private Image fillImage;
    [SerializeField] private Health health;
    [SerializeField] private float tweenDuration = 0.25f;

    private Tween fillTween; // Reference to kill active tweens

    private void Awake()
    {
        fillImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (health == null)
        {
            Debug.LogError("Health reference is not set on HealthbarFill.");
            return;
        }
        health.OnHealthChanged += SetFillAmount;

        SnapFillAmount(health.CurrentHP, health.MaxHP);
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= SetFillAmount;
        }

        fillTween?.Kill();
    }

    public void SetFillAmount(float current, float max)
    {
        if (max <= 0)
        {
            fillImage.fillAmount = 0;
            return;
        }

        float targetFill = Mathf.Clamp01(current / max);

        fillTween?.Kill();

        fillTween = fillImage.DOFillAmount(targetFill, tweenDuration)
                             .SetEase(Ease.OutQuad);
    }

    private void SnapFillAmount(float current, float max)
    {
        if (max <= 0) return;
        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }
}