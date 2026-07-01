using UnityEngine;

// TEMP delete once the real decision layer exists.
public class TempAttackTester : MonoBehaviour
{
    [SerializeField] private EnemyMeleeAttack melee;

    private void Update()
    {
        melee.TryExecute();
    }
}