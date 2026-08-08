using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] float interactionDistanceToPlayer = 4f;
    [SerializeField] float interactionRaycastDistance = 30f;
    [Tooltip("Layers for blocking sight to interactable.")]
    [SerializeField] LayerMask sightLayerMask;

    PlayerAim aim;
    PlayerInputReader playerInput;

    IInteractable focused;

    private void Awake()
    {
        aim = GetComponent<PlayerAim>();
        playerInput = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        IInteractable lookedAt = FindLookedAtInteractable();

        // re-check every frame: state can change while focused (inventory, etc)
        if (lookedAt != null && !lookedAt.CanInteract(gameObject))
        {
            lookedAt = null;
        }

        SetFocused(lookedAt);

        if (focused != null && playerInput.InteractPressed)
        {
            if (focused.TryInteract(gameObject))
            {
                SetFocused(null);
            }
        }
    }

    private IInteractable FindLookedAtInteractable()
    {
        if (!Physics.Raycast(
            aim.AimOrigin,
            aim.AimDirection,
            out RaycastHit info,
            interactionRaycastDistance,
            sightLayerMask,
            QueryTriggerInteraction.Collide))
        {
            return null;
        }

        // distance to the hti point, not the object origin. Works for big objects as well!
        if (Vector3.Distance(info.point, transform.position) > interactionDistanceToPlayer)
        {
            return null;
        }

        return info.collider.GetComponentInParent<IInteractable>();
    }

    private void SetFocused(IInteractable interactable)
    {
        if (focused == interactable) return;

        // interface refs bypass Unity's fake-null, so guard against a destroyed object
        if (focused is Component c && c != null)
        {
            focused.SetFocused(false);
        }

        focused = interactable;

        if (focused != null)
        {
            focused.SetFocused(true);
            string displayName = playerInput.GetDisplayName(playerInput.InteractAction);
            InteractionPromptHUD.Instance.Show($"Press [{displayName}] to {focused.InteractionPrompt}");
        }
        else
        {
            InteractionPromptHUD.Instance.Hide();
        }
    }
}
