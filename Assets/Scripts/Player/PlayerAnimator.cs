using System;
using UnityEngine;

public class PlayerAnimator
{
    static readonly int MoveX = Animator.StringToHash("MoveX");
    static readonly int MoveY = Animator.StringToHash("MoveY");
    static readonly int Velocity = Animator.StringToHash("Velocity");
    static readonly int HasInput = Animator.StringToHash("HasInput");
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

    public void HandleMovementAnimation(Vector2 moveInput, Vector2 localVelocity, bool isGrounded)
    {
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f; // floating point imprecision
        float velocity = localVelocity.magnitude;

        Vector2 blendDirection = Vector2.zero;
        if (hasMoveInput)
        {
            blendDirection = localVelocity.sqrMagnitude > 0.01f
                ? localVelocity.normalized
                : moveInput.normalized;
        }

        Vector2 currentBlendDirection = new Vector2(
            CurrentAnimator.GetFloat(MoveX),
            CurrentAnimator.GetFloat(MoveY));

        bool startingMovement =
            currentBlendDirection.sqrMagnitude <= 0.01f &&
            blendDirection.sqrMagnitude > 0.01f;

        // start moving fast, slower smooth changes between movement directions.
        const float idleBlendTime = 0.01f;
        const float movementBlendTime = 0.12f;
        float blendTime = startingMovement ? idleBlendTime : movementBlendTime;
        
        CurrentAnimator.SetFloat(
            MoveX,
            blendDirection.x,
            blendTime,
            Time.deltaTime);
        CurrentAnimator.SetFloat(
            MoveY,
            blendDirection.y,
            blendTime,
            Time.deltaTime);
        
        
        CurrentAnimator.SetFloat(Velocity, velocity);
        CurrentAnimator.SetBool(HasInput, hasMoveInput);
        CurrentAnimator.SetBool(Moving, hasMoveInput || velocity > 1f);
        CurrentAnimator.SetBool(Grounded, isGrounded);
    }

    public void PlayJumpAnimation() { CurrentAnimator.SetTrigger(Jump); }
    public void PlayPrimaryAbility() { CurrentAnimator.SetTrigger(PrimaryAbility); }
    public void PlaySecondaryAbility() { CurrentAnimator.SetTrigger(SecondaryAbility); }
    public void PlayMovementAbility() { CurrentAnimator.SetTrigger(MovementAbility); }
    public void PlaySpecialAbility() { CurrentAnimator.SetTrigger(SpecialAbility); }
}
