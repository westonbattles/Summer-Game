using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Event-driven kill readout. Subscribes to KillTracker.OnChanged rather than
/// polling, kill count changes discretely, unlike cooldowns.
/// Wire the StageKills scene object in the inspector; both live in the scene,
/// so no singleton timing gymnastica needed here.
/// </summary>
public class KillCountHUD : MonoBehaviour
{
    [SerializeField] Image[] stageImages; // one per stage; each fills as its own stage, then stays full
    [SerializeField] StageKills stageKills;
    [SerializeField] float tweenDuration = 0.25f;

    private Tween fillTween;

    private void OnEnable()
    {
        stageKills.Gate.OnStageChanged += HandleStageChanged; // subscribe to events
        stageKills.Gate.OnWindowExpired += HandleWindowExpired;
        SnapToCurrentState();
    }

    private void OnDisable()
    {
        stageKills.Gate.OnStageChanged -= HandleStageChanged;
        stageKills.Gate.OnWindowExpired -= HandleWindowExpired;
        fillTween?.Kill();
    }

    private void HandleStageChanged(int stage, float stageProgress, float stageTarget)
    {
        fillTween?.Kill();

        for (int i = 0; i < stageImages.Length; i++)
        {
            if (i < stage)
            {
                stageImages[i].gameObject.SetActive(true);
                stageImages[i].fillAmount = 1f;
            }
            else if (i > stage)
            {
                stageImages[i].gameObject.SetActive(false);
                stageImages[i].fillAmount = 0f;
            }
        }

        if (stage < stageImages.Length) // swap in current image for filling
        {
            var current = stageImages[stage];
            current.gameObject.SetActive(true);
            float target = Mathf.Clamp01(stageTarget > 0f ? stageProgress / stageTarget : 0f);
            fillTween = current.DOFillAmount(target, tweenDuration).SetEase(Ease.OutQuad);
        }
    }

    private void HandleWindowExpired(int timesFilled)
    {
        fillTween?.Kill();
        foreach (var img in stageImages)
        {
            img.gameObject.SetActive(false);
            img.fillAmount = 0f;
        }
    }

    private void SnapToCurrentState()
    {
        var gate = stageKills.Gate;
        for (int i = 0; i < stageImages.Length; i++)
        {
            if (i < gate.CurrentStage)
            {
                stageImages[i].gameObject.SetActive(true);
                stageImages[i].fillAmount = 1f;
            }
            else if (i == gate.CurrentStage)
            {
                float target = gate.CurrentStageTarget;
                stageImages[i].gameObject.SetActive(true);
                stageImages[i].fillAmount = target > 0f ? Mathf.Clamp01(gate.StageProgress / target) : 0f;
            }
            else
            {
                stageImages[i].gameObject.SetActive(false);
                stageImages[i].fillAmount = 0f;
            }
        }
    }
}