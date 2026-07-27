using UnityEngine;


[CreateAssetMenu(fileName = "EnemySpawnCard", menuName = "Enemy/Spawn Card")]
public class EnemySpawnCard : ScriptableObject
{
        [SerializeField] Enemy prefab;
        
        // todo: orion use if you wanna steal the ror2 method (director uses creditcost to determine if it can spawn this enemy)
        // int creditCost = 1;
        public Enemy Prefab => prefab;
}