using System.Collections.Generic;
using UnityEngine;

public class ChoiceBubble : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject bubbleVisual; // floating orb model

    List<ItemDefinition> choices;
    bool resolved;
    bool isFocused;

    public string InteractionPrompt => "choose item";

    public bool CanInteract(GameObject interactor)
    {
        if (resolved) return false;
        if (ItemChoiceUI.Instance != null && ItemChoiceUI.Instance.IsOpen) return false;
        return choices != null && choices.Count > 0;
    }

    public bool TryInteract(GameObject interactor)
    {
        if (!CanInteract(interactor)) return false;

        ItemChoiceUI.Instance.Show(choices, HandleChoiceMade);
        return true;
    }

    public void SetFocused(bool focused)
    {
        if (resolved) return;
        if (isFocused == focused) return;

        isFocused = focused;
        // TODO: add visual effects on focused
    }

    public void Initialize(List<ItemDefinition> itemChoices)
    {
        choices = itemChoices;
    }

    void HandleChoiceMade(ItemDefinition chosen)
    {
        if (resolved) return;
        resolved = true;

        ItemSpawner.Instance.Spawn(chosen, transform.position + Vector3.up, Vector3.up * 6f);

        if (bubbleVisual != null) bubbleVisual.SetActive(false);
        Destroy(gameObject);
    }
}
