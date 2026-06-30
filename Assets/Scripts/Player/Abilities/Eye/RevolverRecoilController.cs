using System;
using UnityEngine;

public class RevolverRecoilController : MonoBehaviour
{
    bool isConfigured;
    
    PlayerAim _playerAim;
    [SerializeField] Transform leftHandAimPivot;
    [SerializeField] Transform leftHandTarget;
    [SerializeField] Transform leftElbowHint;

    [Header("Aim")]
    [Tooltip("Extra Euler rotation applied after aiming to account for the revolver and hand's local axes.")]
    [SerializeField] Vector3 aimRotationOffset;
    [Tooltip("How quickly the wrist turns toward the direction supplied by the shot.")]
    [SerializeField, Min(0f)] float aimRotationSharpness = 25f;
    [Tooltip("How long the hand keeps trying to point at the shot direction after firing.")]
    [SerializeField, Min(0f)] float aimHoldDuration = 0.12f;
    [Tooltip("How long the aimed rotation takes to blend back into the normal animated pose.")]
    [SerializeField, Min(0f)] float aimReturnDuration = 0.15f;

    [Header("Kick")]
    [SerializeField, Min(0f)] float positionKick = 0.12f;
    [SerializeField, Min(0f)] float kickImpulse = 16f;
    [SerializeField, Min(0f)] float maxRecoil = 1f;
    [SerializeField, Min(0f)] float maxRecoilVelocity = 24f;
    [Tooltip("Additive Euler rotation at full recoil. Negative X pitches the muzzle upward.")]
    [SerializeField] Vector3 wristKickRotation = new Vector3(-35f, 0f, 0f);

    [Header("Spring")]
    [SerializeField, Min(0f)] float springStrength = 225f;
    [SerializeField, Min(0f)] float damping = 24f;

    [Header("Visual Result")]
    [Tooltip("How far the hand moves forward during recoil. This keeps the elbow in front of the shoulder.")]
    [SerializeField, Min(0f)] float recoilDistance = 2f;
    [Tooltip("Small upward hand movement. Muzzle rise should come primarily from rotation.")]
    [SerializeField, Min(0f)] float recoilLiftDistance = 0.2f;
    [Tooltip("How far the revolver pitches upward before the spring returns it.")]
    [SerializeField, Range(0f, 90f)] float recoilPitchAngle = 35f;

    [Header("Elbow Shape")]
    [Tooltip("Keeps the elbow below the shoulder during the kick.")]
    [SerializeField, Min(0f)] float elbowDownDistance = 0.15f;
    [Tooltip("Keeps the left elbow outside the body during the kick.")]
    [SerializeField, Min(0f)] float elbowOutDistance = 0.08f;

    Vector3 restingPosition;
    Quaternion restingRotation;
    Vector3 restingElbowPosition;
    Quaternion gripRotationOffset;

    Vector3 currentAimDirection;
    Vector3 targetAimDirection;
    float aimInfluence;
    float aimHoldRemaining;
    
    float recoil;
    float recoilVelocity;
    
    void Awake()
    {
        _playerAim = GetComponentInParent<PlayerAim>();
        if (_playerAim == null || leftHandAimPivot == null || leftHandTarget == null)
        {
            Debug.LogError("Revolver recoil controller failed to load, remember to set LeftHandAimPivot and LeftHandTarget transforms on the WeaponRig gameobj in the Eye heirarchy"); // if this happens ill kms (jk)
            isConfigured = false;
            return;
        }

        restingPosition = leftHandTarget.localPosition;
        restingRotation = leftHandTarget.localRotation;
        if (leftElbowHint != null) restingElbowPosition = leftElbowHint.localPosition;

        currentAimDirection = _playerAim.AimDirection.normalized;
        targetAimDirection = currentAimDirection;

        Quaternion initialAimRotation = GetAimRotation(currentAimDirection, out _, out _);
        Quaternion restingWorldRotation = leftHandAimPivot.rotation * restingRotation;
        gripRotationOffset = Quaternion.Inverse(initialAimRotation) * restingWorldRotation;

        isConfigured = true;
    }


    void Update()
    {
        if (!isConfigured) return;

        float dt = Time.deltaTime;

        if (aimInfluence > 0f && aimRotationSharpness <= 0f)
        {
            currentAimDirection = targetAimDirection;
        }
        else if (aimInfluence > 0f)
        {
            float aimBlend = 1f - Mathf.Exp(-aimRotationSharpness * dt);
            currentAimDirection = Vector3.Slerp(currentAimDirection, targetAimDirection, aimBlend).normalized;
        }

        if (aimHoldRemaining > 0f)
        {
            aimHoldRemaining = Mathf.Max(0f, aimHoldRemaining - dt);
        }
        else if (aimInfluence > 0f)
        {
            aimInfluence = aimReturnDuration <= 0f
                ? 0f
                : Mathf.MoveTowards(aimInfluence, 0f, dt / aimReturnDuration);
        }
        
        // recoil is between 0-1 so -(springStrength * recoil) is calculating the spring force (force is stronger when spring / recoil is at the top)
        float acceleration = -(springStrength * recoil) - (damping * recoilVelocity);
        recoilVelocity += acceleration * dt;
        recoil += recoilVelocity * dt;

        if (recoil <= 0f)
        {
            recoil = 0f;
            if (recoilVelocity < 0f) recoilVelocity = 0f;
        }

        if (recoil >= maxRecoil)
        {
            recoil = maxRecoil;
            if (recoilVelocity > 0f) recoilVelocity = 0f;
        }
        
        ApplyRecoil();
    }

    ///<summary>
    /// <c>Kick</c> handles IK animation stuff to make the revolver kick up in the air a bit when firing to look cool
    ///</summary>
    public bool Kick(Vector3 aimDirection)
    {
        if (!isConfigured) return false;

        SetAimDirection(aimDirection);
        aimInfluence = 1f;
        aimHoldRemaining = aimHoldDuration;

        // small immediate movement so the shot feels responsive (doesn't start slow)
        recoil = Mathf.Min(recoil + positionKick, maxRecoil);
        
        // add backward velocity to the spring
        recoilVelocity = Mathf.Min(recoilVelocity + kickImpulse, maxRecoilVelocity);
        return true;
    }

    public void SetAimDirection(Vector3 aimDirection)
    {
        if (!isConfigured || aimDirection.sqrMagnitude < 0.0001f) return;

        targetAimDirection = aimDirection.normalized;
        if (aimInfluence <= 0f) currentAimDirection = targetAimDirection;
    }
    
    void ApplyRecoil()
    {
        Vector3 restingWorldPosition = leftHandAimPivot.TransformPoint(restingPosition);
        Quaternion restingWorldRotation = leftHandAimPivot.rotation * restingRotation;
        Quaternion aimRotation = GetAimRotation(currentAimDirection, out Vector3 right, out Vector3 up);
        Vector3 forward = currentAimDirection;
        Vector3 characterUp = _playerAim.transform.up;

        // move forward so the hand stays in front of the shoulder, then add only a small lift
        leftHandTarget.position = restingWorldPosition
                                  + forward * (recoil * recoilDistance)
                                  + up * (recoil * recoilLiftDistance);

        Quaternion aimOffset = Quaternion.Euler(aimRotationOffset);
        Quaternion muzzleRise = Quaternion.Euler(-recoilPitchAngle * recoil, 0f, 0f);
        Quaternion wristKick = Quaternion.Euler(wristKickRotation * recoil);
        Quaternion aimedRotation = aimRotation * aimOffset * gripRotationOffset;
        Quaternion rotationBeforeKick = Quaternion.Slerp(restingWorldRotation, aimedRotation, aimInfluence);
        Quaternion localKick = muzzleRise * wristKick;
        Quaternion worldKick = aimRotation * localKick * Quaternion.Inverse(aimRotation);

        // aiming only takes over after firing; wrist kick is additive and softens into the aimed pose
        leftHandTarget.rotation = worldKick * rotationBeforeKick;

        if (leftElbowHint != null)
        {
            Vector3 restingElbowWorldPosition = leftElbowHint.parent != null
                ? leftElbowHint.parent.TransformPoint(restingElbowPosition)
                : restingElbowPosition;

            // the left elbow moves down and to the character's left to preserve an L-shaped arm
            leftElbowHint.position = restingElbowWorldPosition
                                     - characterUp * (recoil * elbowDownDistance)
                                     - right * (recoil * elbowOutDistance);
        }
    }

    Quaternion GetAimRotation(Vector3 forward, out Vector3 right, out Vector3 up)
    {
        Vector3 characterUp = _playerAim.transform.up;
        right = Vector3.Cross(characterUp, forward).normalized;

        // fallback for the edge case where the player is aiming almost perfectly straight up or down
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.ProjectOnPlane(leftHandAimPivot.right, forward).normalized;
        }

        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.ProjectOnPlane(Vector3.right, forward).normalized;
        }

        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.ProjectOnPlane(Vector3.forward, forward).normalized;
        }

        up = Vector3.Cross(forward, right).normalized;
        return Quaternion.LookRotation(forward, up);
    }
}
