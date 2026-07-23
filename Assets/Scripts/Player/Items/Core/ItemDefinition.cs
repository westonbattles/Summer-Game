using System;
using UnityEngine;
// this is the data asset. It's shared and note immutable at runtime!!!
// these abilities will be thus used to create mirrored runtime instances that will have custom functionality attached
[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("ID/Name/Icon")]
    [SerializeField] string id;
    [SerializeField] string displayName;
    [SerializeField] Sprite icon;
    
    [Header("Description")]
    [SerializeField, TextArea] string shortDescription;  // blurb that shows up in world, maybe should use maxlines? https://docs.unity3d.com/6000.5/Documentation/ScriptReference/TextAreaAttribute.html
    [SerializeField, TextArea] string longDescription;   // logbook description
    [SerializeField, TextArea] string notes;             // logbook notes
    
    [Header("Properties")]
    [SerializeField] ItemRarity rarity;
    [SerializeField] bool isDuoItem;
    [SerializeField] bool canDropRandomly = true;
    [Tooltip("Relative drop weight vs all other items of the same rarity. Base is one")]
    [SerializeField] float dropWeight = 1f;               // if one item has 2 drop weight and every other item of that rarity has 1, the first item will be twice as likely
    [SerializeField] float difficultyCost = 1f;           // scaling cost, if we ever want it to be different for a given item for whatever reason

    [Header("Model")]
    [Tooltip("the model for when it is dropped on the ground (will be shown in the logbook thing eventually too)")]
    [SerializeField] GameObject worldModelPrefab;
    
    [Header("Behaviour")]
    [SerializeReference, SerializeField] ItemBehaviour behaviour;
    
    public string Id                  => id;
    public string DisplayName           => displayName;
    public Sprite Icon                  => icon;

    public string ShortDescription      => shortDescription;
    public string LongDescription       => longDescription;
    public string Notes                 => notes;

    public ItemRarity Rarity            => rarity;
    public bool IsDuoItem               => isDuoItem;
    public float DifficultyCost         => difficultyCost;
    public GameObject WorldModelPrefab  => worldModelPrefab;
    public ItemBehaviour Behaviour      => behaviour;
}

[Serializable]
public abstract class ItemBehaviour
{
    /// for items that only need to change modifier values to work correctly
    public virtual void ApplyModifiers(PlayerModifiers modifiers, int itemCount) { }
    
    /// for items that need to dome some fancy custom code at runtime
    public virtual ItemRuntime CreateRuntime(Player player) { return new ItemRuntime(); }
}


public class ItemRuntime : IDisposable
{
    public int Count { get; private set; } // count of the item in our inventory

    public void SetCount(int count) { Count = count; } 
    
    public virtual void Tick(float deltaTime) { }
    
    public virtual void Dispose() { } // benefit of using idisopsable, see comment in https://github.com/dotnet/dotnet/blob/b0f34d51fccc69fd334253924abd8d6853fad7aa/src/runtime/src/libraries/System.Private.CoreLib/src/System/IDisposable.cs
}
