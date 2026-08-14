using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;


class ItemGrouping
{
    readonly List<ItemDefinition> _items = new();
    public IReadOnlyList<ItemDefinition> Items => _items;
    public float TotalWeight { get; private set; }
        
    public void Add(ItemDefinition item)
    {
        if (item == null) return;
        
        _items.Add(item);
        TotalWeight += item.DropWeight;
    }

    /// weighted with item weights
    public ItemDefinition GetRandom()
    {
        if (TotalWeight <= 0) return null;

        float weight = Random.Range(0, TotalWeight);
        foreach (ItemDefinition item in _items)
        {
            weight -= item.DropWeight;
            if (weight <= 0) return item;
        }

        return null; // probably shouldn't ever happen i think
    }
}

///
/// UpdateItemCatalogFromAssets runs when the editor button is pressed, and it grabs every itemdefintition in our files, and populates the
/// List<ItemDefinition> definitions; variable. Then, when this script is loaded with OnEnable, it populates three runtime dictionaries
///
/// Dictionary<string, ItemDefinition> _definitionsById;    :
/// Dictionary<ItemRarity, ItemGrouping> _regularItems;     :
/// Dictionary<ItemRarity, ItemGrouping> _duoItems;         :

[CreateAssetMenu(fileName = "Item Catalog", menuName = "Items/Item Catalogue/New", order=1)]
public class ItemCatalog : ScriptableObject
{
    [SerializeField] List<ItemDefinition> definitions = new();
    public IReadOnlyList<ItemDefinition> AllItems => definitions;

    
    Dictionary<string, ItemDefinition> _definitionsById;
    Dictionary<ItemRarity, ItemGrouping> _regularItems;
    Dictionary<ItemRarity, ItemGrouping> _duoItems;
    
    /*
     regularitems / duoitemes look like this:
     
        regularItems:
            - Common    : [Item, Item, Item, ...]
            - Uncommon  : [Item, Item, Item, ...]
            ...
    
        duoItems:
           - Common     : [Item, Item, Item, ...]
           - Uncommon   : [Item, Item, Item, ...]
           ...
     */
    
    
    void OnEnable()
    {
        BuildRuntimeDictionaries();
    }

    void BuildRuntimeDictionaries()
    {
        _definitionsById = new(definitions.Count);
        _regularItems = new();
        _duoItems = new();
        
        foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
        {
            _regularItems.Add(rarity, new ItemGrouping());
            _duoItems.Add(rarity, new ItemGrouping());
        }

        foreach (ItemDefinition definition in definitions)
        {
            if (definition == null)
            {
                Debug.LogWarning($"[Item Catalog - BuildRuntimeDictionaries]: Null entry in item catalog, an asset was probably deleted. Refresh the catalog.", this);
                continue;
            }

            if (string.IsNullOrEmpty(definition.Id))
            {
                Debug.LogError($"[Item Catalog - BuildRuntimeDictionaries]: Item '{definition.name}' has no id set. Skipping Id.", definition);
                continue;
            }

            // NOTE: When you Ctrl/Cmd + D, be careful of duplicate Id!
            if (!_definitionsById.TryAdd(definition.Id, definition))
            {
                Debug.LogError($"[Item Catalog - BuildRuntimeDictionaries]: Duplicate item Id '{definition.Id}' on '{definition.name}' " +
                               $"(already used by '{_definitionsById[definition.Id].name}'). Skipping Id.", definition);
                continue;
            }

            if (!definition.CanDropRandomly) continue; // skip items that can't be dropped randomly

            // populate definitions by id dict
            //_definitionsById.Add(definition.Id, definition);

            // populate duo/regular item dictionaries
            if (definition.IsDuoItem) _duoItems.GetValueOrDefault(definition.Rarity)?.Add(definition);
            else _regularItems.GetValueOrDefault(definition.Rarity)?.Add(definition);
        }
    }

    public ItemDefinition GetItemFromId(string id)
    {
        if (_definitionsById.TryGetValue(id, out ItemDefinition item)) return item;
        throw new ArgumentException($"no item definition found with id: {id}");
    }

    // returns a random ItemDefinition out of every ItemDefinition in the /Items folder
    public ItemDefinition GetRandomItem(ItemRarityWeightDistribution distribution)
    {
        if (definitions.Count == 0) return null;
        
        float duoItemChance = ItemSpawner.Instance.DuoItemSpawnChance;
        bool shouldSpawnDuoItem = Random.value < duoItemChance;
        Dictionary<ItemRarity, ItemGrouping> groupings = shouldSpawnDuoItem ? _duoItems : _regularItems;
        
        return GetRandomFrom(groupings, distribution);
    }

    // returns a random duoitem ItemDefinition out of every ItemDefinition in the /Items folder
    public ItemDefinition GetRandomDuoItem(ItemRarityWeightDistribution distribution)
    {
        if (definitions.Count == 0) return null;
        return GetRandomFrom(_duoItems, distribution);
    }
    
    ItemDefinition GetRandomFrom(Dictionary<ItemRarity, ItemGrouping> groupings, ItemRarityWeightDistribution distribution)
    {
        // modify distribution values for if we have no available items for a specific rarity for whatever reason (shouldn't happen)
        ItemRarityWeightDistribution newDistribution = new(
            groupings[ItemRarity.Common].TotalWeight > 0f ? distribution.Common : 0f,
            groupings[ItemRarity.Uncommon].TotalWeight > 0f ? distribution.Uncommon : 0f,
            groupings[ItemRarity.Rare].TotalWeight > 0f ? distribution.Rare : 0f,
            groupings[ItemRarity.Legendary].TotalWeight > 0f ? distribution.Legendary : 0f);

        // no items found in entire pool
        if (newDistribution.WeightSum <= 0f) return null;
        
        ItemRarity rarity = ItemRarityWeightDistribution.GetRandomRarity(newDistribution);
        return groupings[rarity].GetRandom();
    }

    // Orion addition for multiple random unique choices (choice bubbles)
    public List<ItemDefinition> GetRandomUniqueItems(int count, ItemRarityWeightDistribution distribution, int maxAttemptsPerItem = 20)
    {
        var result = new List<ItemDefinition>(count);
        var seen = new HashSet<ItemDefinition>();

        for (int i = 0; i < count; i++)
        {
            ItemDefinition picked = null;
            for (int attempt = 0; attempt < maxAttemptsPerItem; attempt++)
            {
                ItemDefinition candidate = GetRandomItem(distribution);
                if (candidate == null) break;
                if (seen.Contains(candidate)) continue;
                picked = candidate;
                break;
            }

            if (picked == null) continue; // pool exhausted, give fewer choices
            seen.Add(picked);
            result.Add(picked);
        }

        return result;
    }

#if UNITY_EDITOR        // prevents build runtime issues cause this is meant for editor to compile with AssetDatabase
    /// gets references to all ItemDefinition assets in Assets/#Game/Items (ignoring /Examples folder), and populates a list with them all
    public void UpdateItemCatalogFromAssets()
    {
        // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.LoadAssetByGUID.html
        const string ignoredNullItem = "Assets/#Game/Items/#NullItem/";
        GUID[] itemDefinitionGuids = AssetDatabase.FindAssetGUIDs("t:ItemDefinition", new[] { "Assets/#Game/Items/" });
        
        // properly initialize
        definitions = new(itemDefinitionGuids.Length);
        
        foreach (GUID itemGuid in itemDefinitionGuids)
        {
            if (AssetDatabase.GUIDToAssetPath(itemGuid).StartsWith(ignoredNullItem,StringComparison.OrdinalIgnoreCase)) continue; // skip ignored null folder (ordinal ignore case only bc im p sure i read somewhere that some systems keep asset lowercased by convention for some reason)
            
            // get asset reference using guid and add it to our list
            ItemDefinition definition = AssetDatabase.LoadAssetByGUID<ItemDefinition>(itemGuid);
            if (definition.Id == "") throw new InvalidOperationException($"Item {definition} does not have it's id set"); // make sure u set ids for items guys
            
            // add defintiion to global definition list
            definitions.Add(definition);
        }
        
        BuildRuntimeDictionaries();
        
        EditorUtility.SetDirty(this); // tell the catalog editor that it needs to save this asset since it's been changed
    }
#endif
}