using System.Text;
using UnityEngine;

/// <summary>
/// Verifies:
///  - StageKills.Instance exists and Tracker is non-null
///  - ReportKill increments Kills by exactly 1 and adds max(weight, 0) to Progress
///  - Negative weights are clamped (kill still counts, progress unchanged)
///  - OnChanged fires exactly once per call, with args matching Tracker state
///  - ResetProgress zeroes both values and still fires OnChanged (HUD snap-back)
/// </summary>
[DisallowMultipleComponent]
public class StageKillsSmokeTest : MonoBehaviour
{
    private readonly StringBuilder _log = new StringBuilder();
    private Vector2 _scroll;

    // Captured from the most recent OnChanged invocation.
    private int _eventFireCount;
    private int _lastEventKills;
    private float _lastEventProgress;

    private KillTracker Tracker => StageKills.Instance != null ? StageKills.Instance.Tracker : null;

    private void OnEnable()
    {
        if (Tracker == null)
        {
            Log("FAIL: StageKills.Instance is null. Is a StageKills object in the scene?");
            return;
        }
        Tracker.OnChanged += HandleChanged;
        Log("Subscribed to OnChanged.");
    }

    private void OnDisable()
    {
        if (Tracker != null)
        {
            Tracker.OnChanged -= HandleChanged;
        }
    }

    private void HandleChanged(int kills, float progress)
    {
        _eventFireCount++;
        _lastEventKills = kills;
        _lastEventProgress = progress;
    }

    private void ReportAndCheck(float weight)
    {
        if (Tracker == null) { Log("FAIL: no Tracker."); return; }

        int killsBefore = Tracker.Kills;
        float progressBefore = Tracker.Progress;
        int firesBefore = _eventFireCount;

        Tracker.ReportKill(weight);

        float expectedProgress = progressBefore + Mathf.Max(weight, 0f);
        bool killsOk = Tracker.Kills == killsBefore + 1;
        bool progressOk = Mathf.Approximately(Tracker.Progress, expectedProgress);
        bool firedOnce = _eventFireCount == firesBefore + 1;
        bool argsMatch = _lastEventKills == Tracker.Kills
                       && Mathf.Approximately(_lastEventProgress, Tracker.Progress);

        Log($"ReportKill({weight}) -> Kills {Tracker.Kills}, Progress {Tracker.Progress:F2} | " +
            $"kills:{P(killsOk)} progress:{P(progressOk)} event x1:{P(firedOnce)} args:{P(argsMatch)}");
    }

    private void ResetAndCheck()
    {
        if (Tracker == null) { Log("FAIL: no Tracker."); return; }

        int firesBefore = _eventFireCount;
        Tracker.ResetProgress();

        bool zeroed = Tracker.Kills == 0 && Mathf.Approximately(Tracker.Progress, 0f);
        bool firedOnce = _eventFireCount == firesBefore + 1;
        bool argsMatch = _lastEventKills == 0 && Mathf.Approximately(_lastEventProgress, 0f);

        Log($"ResetProgress() | zeroed:{P(zeroed)} event x1:{P(firedOnce)} args:{P(argsMatch)}");
    }

    private void RunAll()
    {
        Log("--- Run All ---");
        ResetAndCheck();          // known baseline
        ReportAndCheck(1f);       // normal kill
        ReportAndCheck(2.5f);     // weighted (elite-style) kill
        ReportAndCheck(0f);       // zero-weight: kill counts, progress frozen
        ReportAndCheck(-5f);      // negative: must clamp to 0 progress gain
        ResetAndCheck();          // snap back
        Log("--- Done ---");
    }

    private static string P(bool ok) => ok ? "PASS" : "FAIL";

    private void Log(string msg)
    {
        _log.AppendLine(msg);
        Debug.Log($"[SmokeTest] {msg}", this);
    }

    private void OnGUI()
    {
        const float w = 420f;
        GUILayout.BeginArea(new Rect(10, 10, w, Screen.height - 20), GUI.skin.box);

        GUILayout.Label(Tracker != null
            ? $"Kills: {Tracker.Kills}   Progress: {Tracker.Progress:F2}   Events fired: {_eventFireCount}"
            : "StageKills.Instance is NULL");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kill (w=1)")) ReportAndCheck(1f);
        if (GUILayout.Button("Elite (w=2.5)")) ReportAndCheck(2.5f);
        if (GUILayout.Button("w=0")) ReportAndCheck(0f);
        if (GUILayout.Button("w=-5")) ReportAndCheck(-5f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset (stage transition)")) ResetAndCheck();
        if (GUILayout.Button("Run All")) RunAll();
        if (GUILayout.Button("Clear Log")) _log.Clear();
        GUILayout.EndHorizontal();

        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.Label(_log.ToString());
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }
}