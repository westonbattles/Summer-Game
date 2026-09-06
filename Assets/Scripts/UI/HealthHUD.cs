using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class HealthHUD : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private float tweenDuration = 0.25f;

    private Tween fillTween;
    private Tween textTween;
    private float displayedHP;

    private void Start()
    {
        if (health == null)
        {
            Debug.LogError("HealthHUD: Health reference is not set on HealthHUD.");
            return;
        }

        health.OnHealthChanged += HandleHealthChanged;

        displayedHP = health.CurrentHP;
        SnapFillAmount(health.CurrentHP, health.MaxHP);
        SetHealthTextImmediate(health.CurrentHP, health.MaxHP);
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
        }

        fillTween?.Kill();
        textTween?.Kill();
    }

    private void HandleHealthChanged(float current, float max)
    {
        UpdateFillAmount(current, max);
        UpdateHealthTextAnimated(current, max);
    }

    #region Fillbar
    private void UpdateFillAmount(float current, float max)
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
    #endregion

    #region Text
    private void UpdateHealthTextAnimated(float targetHP, float max)
    {
        textTween?.Kill();

        // Smoothly tweens a temporary float variable, updating the text string every frame
        textTween = DOTween.To(() => displayedHP, x => displayedHP = x, targetHP, tweenDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                // prevents ugly decimals showing up in your UI
                healthText.text = $"{Mathf.RoundToInt(displayedHP)}/{max}";
            });
    }

    private void SetHealthTextImmediate(float current, float max)
    {
        healthText.text = $"{Mathf.RoundToInt(current)}/{max}";
    }
    #endregion
}