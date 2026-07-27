using UnityEngine;

//[CreateAssetMenu(fileName = "Item Rarity Palette", menuName = "Items/Rarity Palette", order=2)] : commented out so it doesn't clutter the asset creation menu for items. we only need 1
public sealed class ItemRarityPalette : ScriptableObject
{
    [SerializeField] Color common = Color.white;
    [SerializeField] Color uncommon = Color.green;
    [SerializeField] Color rare = Color.blue;
    [SerializeField] Color legendary = Color.red;

    public Color GetColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Uncommon   => uncommon,
        ItemRarity.Rare       => rare,
        ItemRarity.Legendary  => legendary,
        _                     => common // base
    };
}