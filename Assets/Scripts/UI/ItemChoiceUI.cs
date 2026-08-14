using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR // NOTE: we need to do this in all scripts that use NaughtyAttributes, builds would never need this
using NaughtyAttributes;
#endif

public class ItemChoiceUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform cardContainer;
    [SerializeField] ItemChoiceCardView cardPrefab;
    [SerializeField] ItemRarityPalette rarityPalette;

    readonly List<ItemChoiceCardView> spawnedCards = new(); // list of picked items

    public static ItemChoiceUI Instance { get; private set; }
    public bool IsOpen { get; private set; }

    // pause behavior
    CursorLockMode previousLockState;
    bool previousCursorVisible;
    float previousTimeScale;

    Action<ItemDefinition> pendingCallback;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panelRoot.SetActive(false);
    }

    public void Show(IReadOnlyList<ItemDefinition> choices, Action<ItemDefinition> onSelected)
    {
        Clear();

        foreach (ItemDefinition choice in choices)
        {
            ItemChoiceCardView card = Instantiate(cardPrefab, cardContainer);
            card.Populate(choice, rarityPalette, HandleCardChosen);
            spawnedCards.Add(card);
        }

        pendingCallback = onSelected;
        panelRoot.SetActive(true);
        IsOpen = true;

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPaused(true);
    }

    void HandleCardChosen(ItemDefinition chosen)
    {
        Action<ItemDefinition> callback = pendingCallback;
        Hide();
        callback?.Invoke(chosen);
    }

    public void Hide()
    {
        Clear();
        panelRoot.SetActive(false);
        IsOpen = false;
        pendingCallback = null;

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        SetPaused(false);
    }

    void Clear()
    {
        foreach (ItemChoiceCardView card in spawnedCards) Destroy(card.gameObject);
        spawnedCards.Clear();
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }
    }

#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowIf(nameof(showDebugButtons))]
    [SerializeField] List<ItemDefinition> debugChoices = new();

    [ShowIf(nameof(showDebugButtons))]
    [Button("Show Debug Choices", EButtonEnableMode.Playmode)]
    void TestShow()
    {
        if (debugChoices == null || debugChoices.Count == 0)
        {
            Debug.LogWarning("[ItemChoiceUI - Debug]: No debug choices assigned.", this);
            return;
        }

        Show(debugChoices, chosen =>
            Debug.Log($"[ItemChoiceUI - Debug]: Chose {chosen.DisplayName}", this));
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Hide", EButtonEnableMode.Playmode)]
    void TestHide() => Hide();

    #endregion
#endif

}
