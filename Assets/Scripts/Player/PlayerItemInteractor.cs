using System;
using Unity;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public class PlayerItemInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] float interactionDistanceToPlayer = 4f;
    [SerializeField] float interactionRaycastDistance = 30f;
    [Tooltip("layers for blocking sight to item")]
    [SerializeField] LayerMask sightLayerMask;

    PlayerAim aim;
    PlayerInputReader playerInput;
    
    WorldItem focusedItem;

    void Awake()
    {
        aim = GetComponent<PlayerAim>();
        playerInput = GetComponent<PlayerInputReader>();
    }

    void Update()
    {
        WorldItem lookedAtItem = FindLookedAtItem();
        SetFocusedItem(lookedAtItem);

        if (focusedItem != null && playerInput.InteractPressed)
        {
            if (focusedItem.TryCollect(Player.Instance.Inventory)) SetFocusedItem(null);
        }
    }

    WorldItem FindLookedAtItem()
    {
        // return null if raycast fails
        if (!Physics.Raycast(aim.AimOrigin, aim.AimDirection, out RaycastHit info, interactionRaycastDistance, sightLayerMask, QueryTriggerInteraction.Collide)) return null;

        WorldItem item = info.collider.GetComponentInParent<WorldItem>();
        if (item == null || item.State != WorldItemState.Available || Vector3.Distance(item.transform.position, transform.position) > interactionDistanceToPlayer) return null; // only use items that are available

        return item;
    }
    
    
    void SetFocusedItem(WorldItem item)
    {
        if (focusedItem == item) return;
        
        if (focusedItem != null) focusedItem.SetFocused(false);
        focusedItem = item;

        if (focusedItem != null)
        {
            focusedItem.SetFocused(true);
            InteractionPromptHUD.Instance.Show("Press E to pick up");
        }
        else
        {
            InteractionPromptHUD.Instance.Hide();
        }
    }
}