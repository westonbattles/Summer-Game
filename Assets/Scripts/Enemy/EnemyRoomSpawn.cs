using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
struct Grouping
{
    public EnemySpawnCard SpawnCard;
    [Min(0f)] public float Weight;
}

[DisallowMultipleComponent]
public class EnemyRoomSpawn : MonoBehaviour
{
    [SerializeField] List<Grouping> enemyPool;
    [SerializeField, Tooltip("How many enemies to spawn from the grouping of enemies ")] int count = 1;
    
    
    
    
    
    
    
    // just returns a random enemy by weight from our grouping - returns null if enemies list is empty, or all weights are 0
    EnemySpawnCard GetRandomEnemyWeighted()
    {
        EnemySpawnCard selected = null;
        float totalWeight = 0f;

        foreach (Grouping grouping in enemyPool)
        {
            if (grouping.Weight <= 0f) continue;

            EnemySpawnCard card = grouping.SpawnCard;
            if (card == null || card.Prefab == null || card.creditCost <= 0) continue;
            
            // weighted reservoir sampling
            totalWeight += grouping.Weight;
            if (selected == null || UnityEngine.Random.value < grouping.Weight / totalWeight) selected = card;
        }

        return selected;
    }
}
