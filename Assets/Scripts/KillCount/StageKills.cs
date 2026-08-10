using System;
using UnityEngine;

// BIG NOTE! that this tracks raw kill count and weighted progression separately so "how many died" and "how close is the boss" can diverge when we make director.
public class KillTracker
{
    public int Kills {  get; private set; }
    public float Progress { get; private set; }

    public event Action<int, float> OnChanged;

    /// <summary>
    /// Fired on any change. (kills, progress).
    /// </summary>
    /// <param name="weight"></param>
    public void ReportKill(float weight)    // This is for HUD and when we make the directory.
    {
        Kills++;
        Progress += Mathf.Max(weight, 0f);
        OnChanged?.Invoke(Kills, Progress);
    }

    /// <summary>
    /// On Stage transition. Fires OnChanged so HUDs snap back to zero.
    /// </summary>
    public void ResetProgress()
    {
        Kills = 0;
        Progress = 0f;
        OnChanged?.Invoke(Kills, Progress);
    }
}

/// <summary>
/// Scene-level host for the KillTracker. Singleton because enemy prefabs can't serialize a reference to a scene object.
/// Enemies report into this from their death path and never from Health.OnDeath and never from OnDespawn.
/// This is so pooling leftover enemies at boss spawn won't inflate the count.
/// </summary>
[DisallowMultipleComponent]
public class StageKills : MonoBehaviour
{
    public static StageKills Instance {  get; private set; }
    public KillTracker Tracker { get; } = new KillTracker();
    public OverfillGate Gate { get; private set; }

    [SerializeField] float[] stageTargets;
    [SerializeField] float overfillDuration;
    [SerializeField] float expiryDecay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Stage Kills]: Duplicate StageKills on {name}; destroying.", this);
            Destroy(this);
            return;
        }
        Instance = this;
        Gate = new OverfillGate(Tracker, stageTargets, overfillDuration, expiryDecay);
    }

    private void Update()
    {
        Gate.Tick();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Gate.Dispose();
        }
    }

    public void ResetStage()
    {
        Tracker.ResetProgress();
        Gate.ResetState();
    }
}
