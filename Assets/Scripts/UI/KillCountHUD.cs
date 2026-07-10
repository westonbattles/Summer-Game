using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Event-driven kill readout. Subscribes to KillTracker.OnChanged rather than
/// polling, kill count changes discretely, unlike cooldowns.
/// Wire the StageKills scene object in the inspector; both live in the scene,
/// so no singleton timing gymnastica needed here.
/// </summary>
public class KillCountHUD : MonoBehaviour
{
    [SerializeField] Image fillImage;
    [SerializeField] StageKills stageKills;
    [SerializeField] float tweenDuration = 0.25f;

    [Tooltip("TEMP: placeholder boss threshold. Replace with the run director's threshold once it owns pacing.")]
    [SerializeField] float progressTarget = 50f;

    private Tween fillTween;

    private void OnEnable()
    {
        stageKills.Tracker.OnChanged += HandleChanged;
        // Refresh immediately so re-enabling the HUD doesn't show stale text. If it does, checky here.
        SnapFillAmount(stageKills.Tracker.Progress, progressTarget);
    }

    private void OnDisable()
    {
        stageKills.Tracker.OnChanged -= HandleChanged;
        fillTween?.Kill();
    }

    private void HandleChanged(int kills, float progress)
    {
        UpdateFillAmount(progress, progressTarget);
    }

    private void UpdateFillAmount(float current, float max)
    {
        if (max <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        float targetFill = Mathf.Clamp01(current / max);
        fillTween?.Kill();
        fillTween = fillImage.DOFillAmount(targetFill, tweenDuration)
                             .SetEase(Ease.OutQuad);
    }

    private void SnapFillAmount(float current, float max)
    {
        if (max <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }
        fillImage.fillAmount = Mathf.Clamp01(current / max);
    }
}
