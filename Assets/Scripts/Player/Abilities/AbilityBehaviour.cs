using UnityEngine;

public abstract class AbilityBehaviour : MonoBehaviour
{
    
    [field: SerializeField] public AbilityDefinition Definition { get; private set; }
    
    public virtual void Initialize() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Cancel() { }                // useful for when you switch characters mid-ability and need to stop the ability
}
