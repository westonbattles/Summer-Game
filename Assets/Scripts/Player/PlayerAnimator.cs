using System;
using UnityEngine;

public class PlayerAnimator
{
    static readonly int MoveX = Animator.StringToHash("MoveX");
    static readonly int MoveY = Animator.StringToHash("MoveY");
    static readonly int Velocity = Animator.StringToHash("Velocity");
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

    public void HandleMovementAnimation(Vector2 moveInput, float velocity, bool isGrounded)
    {
        
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        bool hasMoveInput = moveInput.sqrMagnitude > 0.01; // floating point imprecision

        CurrentAnimator.SetFloat(MoveX, moveInput.x);
        CurrentAnimator.SetFloat(MoveY, moveInput.y);
        CurrentAnimator.SetFloat(Velocity, velocity);
        
        CurrentAnimator.SetBool(Moving, hasMoveInput || velocity > 1f);
        CurrentAnimator.SetBool(Grounded, isGrounded);
    }

    public void PlayJumpAnimation() { CurrentAnimator.SetTrigger(Jump); }
    public void PlayPrimaryAbility() { CurrentAnimator.SetTrigger(PrimaryAbility); }
    public void PlaySecondaryAbility() { CurrentAnimator.SetTrigger(SecondaryAbility); }
    public void PlayMovementAbility() { CurrentAnimator.SetTrigger(MovementAbility); }
    public void PlaySpecialAbility() { CurrentAnimator.SetTrigger(SpecialAbility); }
}
