using System;
using System.Data;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{

    [SerializeField] ItemCatalog itemCatalog;
    [SerializeField] float duoItemSpawnChance = 0.08f; // chance to spawn duo item when just randomly spawning an item
    
    [SerializeField] WorldItem worldItemPrefab;
    [SerializeField] GameObject nullItemModelPrefab;
    public static GameObject NullItemModelPrefab;

    public static ItemSpawner Instance { get; private set; }
    public ItemCatalog ItemCatalog => itemCatalog;
    public float DuoItemSpawnChance => duoItemSpawnChance;
    
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

        WorldItem item = Instantiate(worldItemPrefab, position, Quaternion.identity, transform);
        item.Initialize(definition, initialVelocity);
        return item;
    }
    
}