using System;
using System.Collections.Generic;
using UnityEngine;

// entries for the grouping
[Serializable]
public class EnemySpawnEntry
{
    [SerializeField] EnemySpawnCard card;
    [SerializeField, Min(0f)] float weight = 1f;
    // use these?
    //[SerializeField, Min(0f)] float minSpawnDistance = 1f;
    //[SerializeField, Min(0f)] float maxSpawnDistance = 1f;

    public EnemySpawnCard Card => card;
    public float Weight => weight;
}

[CreateAssetMenu(fileName = "EnemySpawnGrouping", menuName = "Enemy/Spawn Grouping", order = 1)]
public class EnemySpawnGrouping : ScriptableObject
{
    [SerializeField] List<EnemySpawnEntry> _entries = new();

    public IReadOnlyList<EnemySpawnEntry> Entries => _entries; // ireadonlylist to ensure we can't modify inner values
}