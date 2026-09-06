using UnityEngine;

public class EnemyAttackController : MonoBehaviour
{
    IEnemyAttack attack;
    GameObject activeIndicatorInstance; // only used if the attack has an indicator

    private void Awake()
    {
        attack = GetComponent<IEnemyAttack>();

        if (attack == null)
        {
            Debug.LogWarning($"{name}: EnemyAttackController found no IEnemyAttack in children.", this);
            return;
        }

        // binds the show/hide functions in this class to the attacks corresponding action (the attack can decide when show/hide happens)
        if (attack is IEnemyAttackWithIndicator)
        {
            IEnemyAttackWithIndicator indicatorAttack = (IEnemyAttackWithIndicator) attack;
            indicatorAttack.showIndicator += ShowIndicator;
            indicatorAttack.hideIndicator += HideIndicator;
        }
    }

    private void Update()
    {
        attack?.TryExecute();
    }
    
    void ShowIndicator(GameObject indicatorPrefab)
    {
        // TODO: give this info to a player component that handles showing body indicators
        // something like: activeIndicatorInstance = Player.Instance.IndicatorManager.Show(indicatorPrefab)
    }
    
    void HideIndicator()
    {
        // TODO: give this info to a player component that handles hiding body indicators
        // something like: Player.Instance.IndicatorManager.Hide(activeIndicatorInstance)
    }
    
    
}
