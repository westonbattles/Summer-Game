using UnityEngine;
using TMPro;

/// <summary>
/// Event-driven kill readout. Subscribes to KillTracker.OnChanged rather than
/// polling, kill count changes discretely, unlike cooldowns.
/// Wire the StageKills scene object in the inspector; both live in the scene,
/// so no singleton timing gymnastica needed here.
/// </summary>
public class KillCountHUD : MonoBehaviour
{
    [SerializeField] StageKills stageKills;
    [SerializeField] TMP_Text killLabel;

    private void OnEnable()
    {
        stageKills.Tracker.OnChanged += HandleChanged;
        // Refresh immediately so re-enabling the HUD doesn't show stale text. If it does, checky here.
        HandleChanged(stageKills.Tracker.Kills, stageKills.Tracker.Progress);
    }

    private void OnDisable()
    {
        stageKills.Tracker.OnChanged -= HandleChanged;
    }

    private void HandleChanged(int kills, float progress)
    {
        killLabel.text = kills.ToString();
        // Later: drive a boss progress bar off 'progress / threshold'
        // once the run directior owns the threshold.
    }
}
