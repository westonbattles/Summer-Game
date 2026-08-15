using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class ItemSpawner : MonoBehaviour
{

    [SerializeField] ItemCatalog itemCatalog;
    [SerializeField] float duoItemSpawnChance = 0.08f; // chance to spawn duo item when just randomly spawning an item
    
    [SerializeField] WorldItem worldItemPrefab;
    [SerializeField] GameObject nullItemModelPrefab;
    public static GameObject NullItemModelPrefab;

    [SerializeField] ChoiceBubble choiceBubblePrefab;
    [SerializeField] int defaultChoiceCount = 3;

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
        if (worldItemPrefab == null) throw new InvalidOperationException("World item prefab can't be null.");

        WorldItem item = Instantiate(worldItemPrefab, position, Quaternion.identity, transform);
        item.Initialize(definition, initialVelocity);
        return item;
    }

    // Added by Orion to spawn choice bubbles, feel free to refactor somewhere else
    public ChoiceBubble SpawnChoiceBubble(Vector3 position, int choiceCount = -1, ItemRarityWeightDistribution distribution = null)
    {
        if (choiceBubblePrefab == null) throw new InvalidOperationException("Choice bubble prefab can't be null");

        distribution ??= ItemRarityWeightDistribution.Default;
        int count = choiceCount > 0 ? choiceCount : defaultChoiceCount;

        List<ItemDefinition> choices = itemCatalog.GetRandomUniqueItems(count, distribution);

        ChoiceBubble bubble = Instantiate(choiceBubblePrefab, position, Quaternion.identity, transform);
        bubble.Initialize(choices);
        return bubble;
    }

#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowIf(nameof(showDebugButtons))]
    [SerializeField] private ItemDefinition debugItemDefinition;

    [ShowIf(nameof(showDebugButtons))]
    [SerializeField] private float debugSpawnDistance = 5f;

    [ShowIf(nameof(showDebugButtons))]
    [Button("Spawn Debug Item", EButtonEnableMode.Playmode)]
    private void TestSpawn()
    {
        if (debugItemDefinition == null)
        {
            Debug.LogWarning("[ItemSpawner - Debug]: Assign a debug item definition first.", this);
            return;
        }

        Vector3 pos = Player.Instance != null
            ? Player.Instance.transform.position + Player.Instance.transform.forward * debugSpawnDistance
            : transform.position;

        Spawn(debugItemDefinition, pos, Vector3.zero);
    }
    #endregion
#endif
}