using UnityEngine;


// goes on the player, handles swapping 
public class AbilityController : MonoBehaviour
{

    [SerializeField] AbilityLoadout startingLoadout;
    public AbilityLoadout CurrentLoadout { get; private set; }

    void Awake()
    {
        SetLoadout(startingLoadout);
    }

    void Update()
    {
        CurrentLoadout?.TickAbilities(Time.deltaTime);
    }

    void SetLoadout(AbilityLoadout loadout)
    {
        CurrentLoadout?.CancelAbilities();
        CurrentLoadout = loadout;
    }
}
