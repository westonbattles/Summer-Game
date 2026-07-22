using System;
using System.Data;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] public ItemDefinition testItem; // DEBUGING
    
    [SerializeField] WorldItem worldItemPrefab;
    [SerializeField] Transform spawnedItemsRoot;
    
    [SerializeField] GameObject nullItemModelPrefab;
    public static GameObject NullItemModelPrefab;

    public static ItemSpawner Instance { get; private set; }
    
    void Awake()
    {
        // don't keep multiple instances
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        NullItemModelPrefab = nullItemModelPrefab;
    }

    public WorldItem Spawn(ItemDefinition definition, Vector3 position, Vector3 initialVelocity)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (worldItemPrefab == null) throw new NoNullAllowedException("world item prefab can't be null");

        WorldItem item = Instantiate(worldItemPrefab, position, Quaternion.identity, spawnedItemsRoot);
        item.Initialize(definition, initialVelocity);
        return item;
    }
    
}