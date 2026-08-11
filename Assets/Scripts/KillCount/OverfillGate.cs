using System;
using UnityEngine;

public class OverfillGate
{
    private readonly KillTracker tracker;
    private readonly float[] stageTargets;   // how much you need per overfill, stageTargets[0] would be the first bar count
    private readonly float overfillDuration; // how long you get to overfill at a base
    private readonly float expiryDecay;      // multiplier for each, gets applied to each extension (<1 value)

    public int CurrentStage { get; private set; }
    public float StageProgress { get; private set; }
    public int TimesFilled { get; private set; }
    public bool WindowOpen { get; private set; }
    public float CurrentStageTarget => TargetFor(CurrentStage);
    public int StageTargetCount => stageTargets?.Length ?? 0;
    public int maxFills = 3;

    private float lastSeenProgress;
    private float windowExpiry;
    private float currentExtension;

    public event Action<int, float, float> OnStageChanged; // stage, stageProgress, stageTarget
    public event Action<int> OnWindowExpired;              // timesFilled, right before reset

    public OverfillGate(KillTracker tracker, float[] stageTargets, float overfillDuration, float expiryDecay)
    {
        this.tracker = tracker;
        this.stageTargets = stageTargets;
        this.overfillDuration = overfillDuration;
        this.expiryDecay = expiryDecay;
        lastSeenProgress = tracker.Progress;
        tracker.OnChanged += HandleTrackerChanged;
    }

    public void Dispose() => tracker.OnChanged -= HandleTrackerChanged;

    // called every frame by StageKills
    public void Tick()
    {
        if (WindowOpen && Time.time >= windowExpiry)
            CloseWindow();
    }

    private void HandleTrackerChanged(int kills, float progress)
    {
        float delta = progress - lastSeenProgress;
        lastSeenProgress = progress;
        if (delta <= 0f) return;
        if (CurrentStage > 0 && !WindowOpen) return; // window already closed, next 0 bar will handle kills

        StageProgress += delta;

        float target = TargetFor(CurrentStage);
        while (target > 0f && StageProgress >= target)
        {
            float overflow = StageProgress - target;
            CompleteStage();
            StageProgress = overflow; // carry remainder
            target = TargetFor(CurrentStage);
        }

        OnStageChanged?.Invoke(CurrentStage, StageProgress, TargetFor(CurrentStage));
    }

    private float TargetFor(int stage)
    {
        if (stageTargets == null || stageTargets.Length == 0) return 0f;
        return stageTargets[Mathf.Min(stage, stageTargets.Length - 1)];
    }

    private void CompleteStage()
    {
        TimesFilled++;
        CurrentStage++;
        if (TimesFilled >= maxFills)
        {
            CloseWindow();
            return;
        }

        if (!WindowOpen)
        {
            WindowOpen = true;
            currentExtension = overfillDuration;
            windowExpiry = Time.time + overfillDuration;
        }
        else
        {
            currentExtension *= expiryDecay; // shrinking bonus on each chained fill
            windowExpiry += currentExtension;
        }
    }

    private void CloseWindow()
    {
        WindowOpen = false;
        OnWindowExpired?.Invoke(TimesFilled);

        // spawn item
        for (int i = 0; i < TimesFilled; i++)
        {
            ItemDefinition randomItem = ItemSpawner.Instance.ItemCatalog.GetRandomItem(ItemRarityWeightDistribution.Default);
            ItemSpawner.Instance.Spawn(randomItem, Player.Instance.transform.position + Vector3.up, Vector3.up * 13);
        }

        CurrentStage = 0;
        StageProgress = 0f;
        TimesFilled = 0;
    }

    public void ResetState()
    {
        CurrentStage = 0;
        StageProgress = 0f;
        TimesFilled = 0;
        WindowOpen = false;
        lastSeenProgress = tracker.Progress;
    }
}