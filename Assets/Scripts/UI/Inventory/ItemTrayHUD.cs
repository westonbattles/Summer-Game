using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ItemTrayHUD : MonoBehaviour
{
    [SerializeField] ItemInventory inventory;
    [SerializeField] ItemSlot slotPrefab;
    [SerializeField] RectTransform slotContainer;

    [Header("Rarity Colors")]
    [SerializeField] Color commonColor      = Color.white;
    [SerializeField] Color uncommonColor    = Color.green;
    [SerializeField] Color rareColor        = Color.blue;
    [SerializeField] Color legendaryColor   = Color.red;
    // TODO: promote to a shared RarityPalette ScriptableObject once pickup VFX / droplets need the same colors

    private readonly Dictionary<ItemDefinition, ItemSlot> slots = new();

    private void OnEnable()
    {
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

    private Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Uncommon     => uncommonColor,
        ItemRarity.Rare         => rareColor,
        ItemRarity.Legendary    => legendaryColor,
        _                       => commonColor,
    };
}
