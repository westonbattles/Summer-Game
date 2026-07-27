using UnityEngine;

public class EnemyAttackController : MonoBehaviour
{
    private IEnemyAttack attack;

    private void Awake()
    {
        attack = GetComponent<IEnemyAttack>();

        if (attack == null)
        {
            Debug.LogWarning($"{name}: EnemyAttackController found no IEnemyAttack in children.", this);
        }
    }

    private void Update()
    {
        attack?.TryExecute();
    }
}
