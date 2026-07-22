using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemInventory : MonoBehaviour
{
    
    public const int MaxStackSize = 999; // max stack size for each item, can make instance var for per item if we want
    
    // for debugging
    [SerializeField] List<ItemDefinition> startingItems = new();
    
    // mapping of item defintions and item instances
    readonly Dictionary<ItemDefinition, ItemRuntime> items = new();

    Player _player;
    
    public float DifficultyPoints { get; private set; } // each item adds to this value

    /// <summary>Fired whenever an item's count changes. (definition, newCount)</summary>
    public event Action<ItemDefinition, int> OnItemCountChanged;


    void Awake()
    {
        _player = GetComponent<Player>();
        _player.Modifiers.ResetAll();
    }

    void Start()
    {
        // populate items dictionary with starting items
        foreach (ItemDefinition startingItem in startingItems) AddItem(startingItem);
    }

    void Update()
    {
        // tick every item in the inventory
        float deltaTime = Time.deltaTime;
        
        foreach (ItemRuntime itemRuntime in items.Values)
        {
            itemRuntime?.Tick(deltaTime);
        }
    }

    void OnDestroy()
    {
        foreach (ItemRuntime itemRuntime in items.Values) itemRuntime.Dispose(); // use idisposable to destroy all the runtime instances we might have floating around in memory
    }

    /// gets how many copies of the item the player has in their inventory right now
    public int GetCurrentCount(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null) return 0;
        if (!items.TryGetValue(itemDefinition, out ItemRuntime itemRuntime)) return 0;

        return itemRuntime.Count;
    }

    public bool HasItem(ItemDefinition itemDefinition)
    {
        return GetCurrentCount(itemDefinition) > 0;
    }
    

    public void AddItem(ItemDefinition itemDefinition, int amount = 1)
    {
        if (itemDefinition == null || amount <= 0) return;
        
        // if the item isn't alr in our inventory, we need to create it and add it
        if (!items.TryGetValue(itemDefinition, out ItemRuntime item))
        {
            item = itemDefinition.Behaviour?.CreateRuntime(_player) ?? new ItemRuntime();
            items.Add(itemDefinition, item);
        }
        
        // for new items, item.Count is initialized to 0 so we're just setting it to be 'amount'
        item.SetCount(Mathf.Min(item.Count + amount, MaxStackSize));
        
        RecalculateModifiers();

        OnItemCountChanged?.Invoke(itemDefinition, item.Count);
    }

    void RecalculateModifiers()
    {

        //float previousDifficulty = DifficultyPoints;
        
        _player.Modifiers.ResetAll();
        DifficultyPoints = 0f;

        foreach (KeyValuePair<ItemDefinition, ItemRuntime> pair in items)
        {
            ItemDefinition definition = pair.Key;
            ItemRuntime runtime = pair.Value;
            
            definition.Behaviour?.ApplyModifiers(_player.Modifiers, runtime.Count);
            DifficultyPoints += definition.DifficultyCost * runtime.Count;
        }
    }
}
