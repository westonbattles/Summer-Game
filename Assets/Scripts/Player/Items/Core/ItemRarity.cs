using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary,
}

public class ItemRarityWeightDistribution
{
    public float Common;
    public float Uncommon;
    public float Rare;
    public float Legendary;

    public float WeightSum => Common + Uncommon + Rare + Legendary;

    public ItemRarityWeightDistribution(float common, float uncommon, float rare, float legendary)
    {
        Common = common;
        Uncommon = uncommon;
        Rare = rare;
        Legendary = legendary;
    }
    
    /// gets a random itemrarity based on the distribution weights passed in via ItemRarityWeightDistribution
    public static ItemRarity GetRandomRarity(ItemRarityWeightDistribution weights)
    {
        float weight = Random.Range(0f, weights.WeightSum);
        if (weight < weights.Common) return ItemRarity.Common;
        if (weight < weights.Common + weights.Uncommon) return ItemRarity.Uncommon;
        if (weight < weights.Common + weights.Uncommon + weights.Rare) return ItemRarity.Rare;
        return ItemRarity.Legendary;
    }
    
    // static weight distribution variables (if we want different distributions for different ways of getting items, some enemies are more likely to drop more rare items etc)
    
    public static readonly ItemRarityWeightDistribution Default = new(
        common:     80,
        uncommon:   30,
        rare:       12,
        legendary:  5
    );
}
