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
    [Tooltip("If the group spawn should randomly choose each enemy (allowing duplicates), or if it should sequentially pick from the groupings (meaning groups will be the same every time)")]
    public bool PickEnemiesRandomly = true;

    [SerializeField] List<Grouping> enemyPool;
    [SerializeField, Tooltip("How many enemies to spawn from the grouping of enemies ")] int count = 1;

    public void SpawnEnemies()
    {
        if (EnemySpawner.Instance == null || enemyPool == null || count <= 0) return;

        List<EnemySpawnCard> validCards = new();
        float largestRadius = 0.5f;

        foreach (Grouping grouping in enemyPool)
        {
            EnemySpawnCard card = grouping.SpawnCard;
            if (card == null || card.Prefab == null || card.creditCost <= 0 || (PickEnemiesRandomly && grouping.Weight <= 0f)) continue;

            validCards.Add(card);
            if (card.Prefab.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent)) largestRadius = Mathf.Max(largestRadius, agent.radius);
        }

        if (validCards.Count == 0) return;

        float spacing = largestRadius * 2f + 0.25f;
        float rotation = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        int ring = 0;
        int indexInRing = 0;
        int countInRing = 1;

        for (int i = 0; i < count; i++)
        {
            if (indexInRing >= countInRing)
            {
                ring++;
                indexInRing = 0;
                countInRing = Mathf.Min(ring * 6, count - i);
            }

            float angle = rotation + indexInRing * Mathf.PI * 2f / countInRing;
            Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * spacing);
            EnemySpawnCard card = PickEnemiesRandomly ? GetRandomEnemyWeighted() : validCards[i % validCards.Count];

            EnemySpawner.Instance.Spawn(card, position, countTowardSpawnCap: false);
            indexInRing++;
        }
    }
    
    
    
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
