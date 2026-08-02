using UnityEngine;


[CreateAssetMenu(fileName = "EnemySpawnCard", menuName = "Enemy/Spawn Card")]
public class EnemySpawnCard : ScriptableObject
{
        [SerializeField] Enemy prefab;
        [SerializeField] int creditCost = 1;    
        
        // todo: enemy type enum
        
        public Enemy Prefab => prefab;
}