using System;
using System.Collections.Generic;
using Unity;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "Item Catalog", menuName = "Items/Item Catalogue/New", order=1)]
public class ItemCatalog : ScriptableObject
{
    //[field: SerializeField] 

    [SerializeField] List<ItemDefinition> definitions;
    Dictionary<string, ItemDefinition> _definitionsById;
    public IReadOnlyList<ItemDefinition> AllItems => definitions;


    public ItemDefinition GetItemFromId(string id)
    {
        return _definitionsById.GetValueOrDefault(id);
    }

    // returns a random itemdefinition out of every itemdefinition in our list
    public ItemDefinition GetRandomItem()
    {
        if (definitions.Count == 0) return null;

        // TODO: BALANCING BY RARITY
        int randomIndex = Random.Range(0, definitions.Count);
        return definitions[randomIndex];
    }
    
    
    /// gets references to all ItemDefinition assets in Assets/#Game/Items (ignoring /Examples folder), and populates a list with them all
    public void UpdateItemCatalogFromAssets()
    {
        // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.LoadAssetByGUID.html
        const string ignoredNullItem = "Assets/#Game/Items/#Null/";
        GUID[] itemDefinitionGuids = AssetDatabase.FindAssetGUIDs("t:ItemDefinition", new[] { "Assets/#Game/Items/" });
        
        definitions = new(itemDefinitionGuids.Length);
        _definitionsById = new(itemDefinitionGuids.Length);
        
        foreach (GUID itemGuid in itemDefinitionGuids)
        {
            if (AssetDatabase.GUIDToAssetPath(itemGuid).StartsWith(ignoredNullItem,StringComparison.OrdinalIgnoreCase)) continue; // skip ignored null folder (ordinal ignore case only bc im p sure i read somewhere that some systems keep asset lowercased by convention for some reason)
            
            // get asset reference using guid and add it to our list
            ItemDefinition definition = AssetDatabase.LoadAssetByGUID<ItemDefinition>(itemGuid);
            definitions.Add(definition);
            if (definition.Id == "")
            {
                throw new InvalidOperationException($"Item {definition} does not have it's id set");
            }
            _definitionsById.Add(definition.Id, definition);
        }
        
        EditorUtility.SetDirty(this); // tell the catalog editor that it needs to save this asset since it's been changed
    }
    
}