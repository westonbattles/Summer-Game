using UnityEngine;

public class AbilityLoadout : MonoBehaviour
{
    [SerializeField] AbilityBehaviour primary;
    [SerializeField] AbilityBehaviour secondary;
    [SerializeField] AbilityBehaviour movement;
    [SerializeField] AbilityBehaviour special;
    
    public void TickAbilities(float deltaTime)
    {
        primary.Tick(deltaTime);
        secondary.Tick(deltaTime);
        movement.Tick(deltaTime);
        special.Tick(deltaTime);
    }

    public void CancelAbilities()
    {
        primary.Cancel();
        secondary.Cancel();
        movement.Cancel();
        special.Cancel();
    }
}


