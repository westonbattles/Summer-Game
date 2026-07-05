using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// XP bar widget. Structurally mirrors HealthHUD with two differences.
/// level wraps around.
/// XP text updates instantly.
/// </summary>
public class XPHUD : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerExperience experience;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("Tweens")]
    [SerializeField] private float fillTweenDuration = 0.25f;
    [Tooltip("Duration of the fill-to-full leg on level up. Snappier than the normal tween feels better.")]
    [SerializeField] private float levelUpFillDuration = 0.15f;

    private Tween fillTween;
    private int pendingLevelUps;
    private int latestLevel;

    private void OnEnable()
    {
        if (experience == null)
        {
            Debug.LogError("XPHUD: PlayerExperience reference is not set on XPHUD.");
            enabled = false;
            return;
        }

        experience.OnXPChanged += HandleXPChanged;
        experience.OnLevelUp += HandleLevelUp;

        // snap to current state so a re-enabled HUD doesn't sit stale until next orb
        latestLevel = experience.Level;
        pendingLevelUps = 0;
        SnapTo(experience.CurrentXP, experience.RequiredXP, experience.Level);
    }

    private void OnDisable()
    {
        if (experience != null)
        {
            experience.OnXPChanged -= HandleXPChanged;
            experience.OnLevelUp -= HandleLevelUp;
        }

        fillTween?.Kill();
        pendingLevelUps = 0;
    }

    private void HandleLevelUp(int newLevel)
    {
        // animation is built when the paired OnXpChange lands.
        pendingLevelUps++;
        latestLevel = newLevel;
    }

    private void HandleXPChanged(float current, float required)
    {
        float targetFill = required > 0f ? Mathf.Clamp01(current / required) : 0f;
        fillTween?.Kill();
        
        if (pendingLevelUps == 0)
        {
            // normal grant within the same level. Identical to health bar path.
            fillTween = fillImage.DOFillAmount(targetFill, fillTweenDuration)
                                 .SetEase(Ease.OutQuad);
        }
        else
        {
            // wrap-around: one fill-to-full + snap-to-zero leg per level gained.
            Sequence seq = DOTween.Sequence();
            int levelsGained = pendingLevelUps;
            int startingLevel = latestLevel - levelsGained;

            for (int i = 1; i <= levelsGained; i++)
            {
                int levelAfterThisFill = startingLevel + i;
                seq.Append(fillImage.DOFillAmount(1f, levelUpFillDuration).SetEase(Ease.OutQuad));
                seq.AppendCallback(() =>
                {
                    fillImage.fillAmount = 0f;
                    SetLevelText(levelAfterThisFill);
                    // juice hook: level-up maybe can get a flash/punch? add sound and stuff.
                    // maybe use levelText.transform.DOPunchScale(...)
                });
            }

            seq.Append(fillImage.DOFillAmount(targetFill, fillTweenDuration).SetEase(Ease.OutQuad));
            fillTween = seq;
            pendingLevelUps = 0;
        }

        SetXPText(current, required);
    }

    private void SnapTo(float current, float required, int level)
    {
        fillImage.fillAmount = required > 0f ? Mathf.Clamp01(current / required) : 0f;
        SetLevelText(level);
        SetXPText(current, required);
    }

    private void SetLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = level.ToString();
        }
    }

    private void SetXPText(float current, float required)
    {
        if (xpText != null)
        {
            xpText.text = $"{Mathf.FloorToInt(current)}/{Mathf.CeilToInt(required)}";
        }
    }
}
