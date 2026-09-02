using System;
using UnityEngine;
using NaughtyAttributes;

public enum PlayerCharacter
{
    Hand,
    Eye
}

public class PlayerCharacterSwitcher : MonoBehaviour
{
    [SerializeField] GameObject handModel;
    [SerializeField] GameObject eyeModel;

    [SerializeField] Animator handAnimator;
    [SerializeField] Animator eyeAnimator;

    Player _player;
    AbilityController AbilityController;

    public PlayerCharacter CurrentCharacter { get; private set; }
    
    public Animator CurrentAnimator { get; private set; }

    public bool switchFlag = false;

    void Awake()
    {
        _player = GetComponent<Player>();
        AbilityController = GetComponent<AbilityController>();
    }

    void Start()
    {
        SetCharacter(PlayerCharacter.Eye); // default character
    }

    void SetCharacter(PlayerCharacter character)
    {
        CurrentCharacter = character;
        
        handModel.SetActive(character == PlayerCharacter.Hand);
        eyeModel.SetActive(character == PlayerCharacter.Eye);

        // swap animators
        CurrentAnimator = character == PlayerCharacter.Hand ? handAnimator : eyeAnimator;

        // swap loadout
        AbilityLoadout loadout = character == PlayerCharacter.Hand ? _player.HandLoadout : _player.EyeLoadout;
        AbilityController.SetLoadout(loadout);
    }
    
    public void Swap()
    {
        PlayerCharacter next = CurrentCharacter == PlayerCharacter.Eye ? PlayerCharacter.Hand : PlayerCharacter.Eye;
        SetCharacter(next);

        // set flag for Newbie Gains / other swap items
        switchFlag = true;
    }


#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowNativeProperty] private PlayerCharacter DebugCurrentCharacter => CurrentCharacter;

    [ShowIf(nameof(showDebugButtons))]
    [Button("Force Swap Character", EButtonEnableMode.Playmode)]
    private void TestSwap()
    {
        Swap();
    }
    #endregion
#endif
}
