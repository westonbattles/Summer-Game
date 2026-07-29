using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract(GameObject interactor);        // can be used for objects to refuse interaction such as a locked chest or door.
    bool TryInteract(GameObject interactor);        // returns true if interaction is consumed
    void SetFocused(bool focused);
}
