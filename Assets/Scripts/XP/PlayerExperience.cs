using UnityEngine;
using System;

/// <summary>
/// The XP sink. Lives on the player, owns current XP / level / the requirement curve,
/// and fires events the XP bar subscribes to - same event shape as Health, so the
/// bar widget is structurally identical to a health bar.
/// Exposed as a static Instance so orbs can find their target without scene lookups
/// (same pattern as ProjectilePoolManager).
/// </summary>
[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    public static PlayerExperience Instance { get; private set; }

    [Header("Leveling Curve")]
    [Tooltip("XP required to go from level 1 to level 2.")]
    [SerializeField] private float baseRequirement = 100f;
    [Tooltip("Each level's requirement is the previous requirement times this.")]
    [SerializeField] private float growthFactor = 1.5f;

    [Header("Collection")]
    [Tooltip("Where orbs fly to and are collected. Usually chest height; falls back to the root transform.")]
    [SerializeField] private Transform collectPoint;

    public int Level { get; private set; } = 1;
    public float CurrentXP { get; private set; }
    public float RequiredXP { get; private set; }

    public Transform CollectPoint => collectPoint != null ? collectPoint : transform;

    public event Action<float, float> OnXPChanged; // current, required
    public event Action<int> OnLevelUp;

    private void Awake()
    {
        Instance = this;
        RequiredXP = RequirementForLevel(Level);
    }

    private void Start()
    {
        // Initial psuh so the bar starts correct without waiting for the first orb.
        OnXPChanged?.Invoke(CurrentXP, RequiredXP);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddXP(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentXP += amount;

        while (CurrentXP >= RequiredXP)
        {
            CurrentXP -= RequiredXP;
            Level++;
            RequiredXP = RequirementForLevel(Level);
            OnLevelUp?.Invoke(Level);
        }

        OnXPChanged?.Invoke(CurrentXP, RequiredXP);
    }

    private float RequirementForLevel(int level) => baseRequirement * Mathf.Pow(growthFactor, level - 1);
}
