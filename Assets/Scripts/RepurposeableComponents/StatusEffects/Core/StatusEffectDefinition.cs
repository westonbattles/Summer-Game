using UnityEngine;
using System.Collections;
using NaughtyAttributes;


[CreateAssetMenu(menuName = "Status Effect")]
public class StatusEffectDefinition : ScriptableObject
{
    public string displayName;
    [SerializeField, TextArea] public string description;   // TODO: Change to a colorful formatting
    public Sprite icon;         
    public bool hidden;         // HUD skips this.

    [Tooltip("<= 0 = permanent")] public float duration = 5f;
    [Tooltip("<= 0 = no ticks")] public float tickInterval = 0f;

    
    [Tooltip("Determines the max stacks for the item. If this is set to 1, then it will not change when reapplied.")] 
    public int maxStacks = 1;

    [SerializeReference] 
    public StatusEffectBehavior behavior;


    [ContextMenu("Set Behavior / Debug Log")]
    void SetDebug() => behavior = new DebugLogBehavior();
}
