using System;
using UnityEngine;

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
    AbilityController _abilityController;

    public PlayerCharacter CurrentCharacter { get; private set; }
    
    public Animator CurrentAnimator { get; private set; }

    void Awake()
    {
        _player = GetComponent<Player>();
        _abilityController = GetComponent<AbilityController>();
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
        _abilityController.SetLoadout(loadout);
    }
    
    public void Swap()
    {
        PlayerCharacter next = CurrentCharacter == PlayerCharacter.Eye ? PlayerCharacter.Hand : PlayerCharacter.Eye;
        SetCharacter(next);
    }
    
}
