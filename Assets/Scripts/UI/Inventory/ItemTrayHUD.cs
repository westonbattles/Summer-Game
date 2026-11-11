using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ItemTrayHUD : MonoBehaviour
{
    [SerializeField] ItemInventory inventory;
    [SerializeField] ItemSlot slotPrefab;
    [SerializeField] RectTransform slotContainer;

    [Header("Rarity Palette")]
    [SerializeField] ItemRarityPalette rarityPalette;

    private readonly Dictionary<ItemDefinition, ItemSlot> slots = new();

    private void OnEnable()
    {
        if (slotPrefab != null && slotPrefab.gameObject.scene.IsValid())
        {
            slotPrefab.gameObject.SetActive(false);
        }

        if (inventory == null)
        {
            Debug.LogWarning($"{name}: no ItemInventory assigned.", this);
            return;
        }
        inventory.OnItemCountChanged += HandleItemCountChanged;
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnItemCountChanged -= HandleItemCountChanged;
        }
    }

    private void HandleItemCountChanged(ItemDefinition itemDefinition, int newCount)
    {
        if (!slots.TryGetValue(itemDefinition, out ItemSlot slot))
        {
            // case 1: first pickup of item. Layout group appends to end so pickup order falls out of event order automatically.
            slot = Instantiate(slotPrefab, slotContainer);
            slot.Bind(itemDefinition, GetRarityColor(itemDefinition.Rarity));
            slot.gameObject.SetActive(true);
            slots.Add(itemDefinition, slot);
        }

        if (newCount <= 0)
        {
            // for removeitem
            slots.Remove(itemDefinition);
            Destroy(slot.gameObject);
            return;
        }

        slot.SetCount(newCount);
    }

    Color GetRarityColor(ItemRarity rarity) => rarityPalette.GetColor(rarity);
}
