using UnityEngine;

public class BossDoorLight : MonoBehaviour
{
    [SerializeField] Renderer colorGameObject;
    [SerializeField] Material offMaterial;
    [SerializeField] Material onMaterial;

    public bool IsOn { get; private set; }

    public void TurnOn()
    {
        colorGameObject.material = onMaterial;
        IsOn = true;
    }
    
    public void TurnOff()
    {
        colorGameObject.material = offMaterial;
        IsOn = false;
    }
}
