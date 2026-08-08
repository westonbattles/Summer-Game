using NaughtyAttributes;
using System;
using UnityEngine;

public enum WorldItemState
{
    Airborne,
    Available,
    Collected
}

public class WorldItem : MonoBehaviour, IInteractable
{
    [Header("Flight")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Collider flightCollider;
    [SerializeField] GameObject flightDot;
    [SerializeField] LayerMask landingLayers;
    
    [Header("Available (Ground) Item")]
    [SerializeField] Collider interactionCollider;  // different colliders since it will be flying thru the air as a dot like in ror2 (also fortnite) and won't be pickupable then
    [SerializeField] Transform modelSocket;         // parent transform for entire model
    [SerializeField] Vector3 spawnOffset;
    [SerializeField] float spinDegreesPerSecond = 75f;

    [Header("Outline")]
    [SerializeField] Material outlineMaterial;
    [SerializeField] ItemRarityPalette rarityPalette;
    
    public ItemDefinition Definition { get; private set; }
    public WorldItemState State { get; private set; }

    GameObject worldModel;
    Vector3 modelRestPosition;
    Quaternion modelRestRotation;
    bool isFocused;

    public string InteractionPrompt => "pick up";

    public bool CanInteract(GameObject interactor)
    {
        if (State != WorldItemState.Available) return false;

        // if its a full stack, it won't show the prompt.
        ItemInventory inventory = interactor.GetComponent<Player>().Inventory;
        return inventory.GetCurrentCount(Definition) < ItemInventory.MaxStackSize;
    }

    public bool TryInteract(GameObject interactor)
    {
        if (!CanInteract(interactor)) return false;

        State = WorldItemState.Collected;

        ItemInventory inventory = interactor.GetComponent<Player>().Inventory;
        inventory.AddItem(Definition);
        Destroy(gameObject);
        return true;
    }

    public void Initialize(ItemDefinition definition, Vector3 velocity)
    {
        Definition = definition;
        
        CreateWorldModel();
        EnterAirborneState();
        StartPhysics(velocity);
    }

    void Update()
    {
        if (State != WorldItemState.Available) return;
        
        // rotation
        modelSocket.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
    }

    void Land()
    {
        EnterAvailableState();
        StopPhysics();
    }

    public void SetFocused(bool focused)
    {
        if (State != WorldItemState.Available) return;
        if (isFocused == focused) return;

        isFocused = focused;
    }
    
    void CreateWorldModel()
    {
        if (Definition.WorldModelPrefab == null)
        {
            Debug.LogWarning( $"Item '{Definition.DisplayName}' has no world model prefab.", Definition);
            worldModel = Instantiate(ItemSpawner.NullItemModelPrefab, modelSocket,false);
        }
        else worldModel = Instantiate(Definition.WorldModelPrefab, modelSocket,false);
        
        worldModel.transform.position += spawnOffset;
        
        WorldItemOutline.AddTo(worldModel, outlineMaterial, rarityPalette.GetColor(Definition.Rarity));

        modelRestPosition = modelSocket.localPosition;
        modelRestRotation = modelSocket.localRotation;

        worldModel.SetActive(false);
    }
    

    void EnterAirborneState()
    {
        State = WorldItemState.Airborne;
        isFocused = false;
        
        // disable the item model
        worldModel.SetActive(false);
        interactionCollider.enabled = false;
        
        // enabled the dot model
        flightDot.SetActive(true);
        flightCollider.enabled = true;
    }

    void EnterAvailableState()
    {
        State = WorldItemState.Available;
        isFocused = false; // just in case
        
        // disable the dot model
        flightDot.SetActive(false);
        flightCollider.enabled = false;
        
        // enable the ground model
        worldModel.SetActive(true);
        interactionCollider.enabled = true;
    }

    void StartPhysics(Vector3 velocity)
    {
        rb.isKinematic = false;
        rb.useGravity = false;      // if its true, the AddForce would have another order of magnitude
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
    }

    void StopPhysics()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // ignores gravity
        rb.Sleep();
    }
    
    // has to be ground since its our only collision layer
    void OnCollisionEnter(Collision collision)
    {
        if (State != WorldItemState.Airborne) return;

        // gamobj.layer is the layer number not the actual bitmask so we can just shift 1 over by this value and & it with our given layer mask to make sure the collision was on the correct layer
        if ((landingLayers.value & 1 << collision.gameObject.layer) == 0) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contactPoint = collision.GetContact(i);
            float maxNormal = Mathf.Cos(Player.Instance.Motor.MaxStableSlopeAngle * Mathf.Deg2Rad);     // just using the players grounded max angle
            if (Vector3.Dot(contactPoint.normal, Vector3.up) >= maxNormal)                        // if item is grounded
            {
                Land();
                return;
            }
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(Physics.gravity * 3f, ForceMode.Acceleration); // triple gravity (note it was 3x before since rb.gravity is weird and adds one).
    }


#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowNativeProperty] WorldItemState CurrentState => State;
    [ShowNativeProperty] string ItemName => Definition != null ? Definition.DisplayName : "(uninitialized)";

    [ShowIf(nameof(showDebugButtons))]
    [Button("Force Land")]
    void TestForceLand()
    {
        if (State == WorldItemState.Airborne)
        {
            Land();
        }
    }
    #endregion
#endif
}
