using System;
using UnityEngine;

public class PlayerAnimator
{
    static readonly int Speed = Animator.StringToHash("Speed");
    static readonly int Moving = Animator.StringToHash("isMoving");
    static readonly int Grounded = Animator.StringToHash("isGrounded");
    static readonly int Jump = Animator.StringToHash("JumpTrigger");
    static readonly int PrimaryAbility = Animator.StringToHash("PrimaryAbility");
    static readonly int SecondaryAbility = Animator.StringToHash("SecondaryAbility");
    static readonly int MovementAbility = Animator.StringToHash("MovementAbility");
    static readonly int SpecialAbility = Animator.StringToHash("SpecialAbility");

    readonly PlayerCharacterSwitcher _characterSwitcher;
    Animator CurrentAnimator => _characterSwitcher.CurrentAnimator;

    public PlayerAnimator(PlayerCharacterSwitcher characterSwitcher)
    {
        _characterSwitcher = characterSwitcher;
    }

    public void HandleMovementAnimation(float movementSpeed, bool isGrounded, bool hasMoveInput)
    {
        CurrentAnimator.SetFloat(Speed, movementSpeed);
        CurrentAnimator.SetBool(Moving, hasMoveInput || movementSpeed > 1f);
        CurrentAnimator.SetBool(Grounded, isGrounded);
    }

    public void PlayJumpAnimation() { CurrentAnimator.SetTrigger(Jump); }
    public void PlayPrimaryAbility() { CurrentAnimator.SetTrigger(PrimaryAbility); }
    public void PlaySecondaryAbility() { CurrentAnimator.SetTrigger(SecondaryAbility); }
    public void PlayMovementAbility() { CurrentAnimator.SetTrigger(MovementAbility); }
    public void PlaySpecialAbility() { CurrentAnimator.SetTrigger(SpecialAbility); }
}
