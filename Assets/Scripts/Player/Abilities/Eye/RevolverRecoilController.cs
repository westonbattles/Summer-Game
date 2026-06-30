using System;
using UnityEngine;

public class RevolverRecoilController : MonoBehaviour
{
    bool enabled;
    
    PlayerAim _playerAim;
    [SerializeField] Transform leftHandAimPivot;
    [SerializeField] Transform leftHandTarget;

    [Header("Kick")]
    [SerializeField, Min(0f)] float positionKick = 0.12f;
    [SerializeField, Min(0f)] float kickImpulse = 16f;
    [SerializeField, Min(0f)] float maxRecoil = 1f;
    [SerializeField, Min(0f)] float maxRecoilVelocity = 24f;

    [Header("Spring")]
    [SerializeField, Min(0f)] float springStrength = 225f;
    [SerializeField, Min(0f)] float damping = 24f;

    [Header("Visual Result")]
    [SerializeField, Min(0f)] float recoilDistance = 0.06f;
    [SerializeField, Range(0f, 45f)] float recoilAngle = 12f;

    Vector3 restingPosition;
    Quaternion restingRotation;
    
    float recoil;
    float recoilVelocity;
    
    void Awake()
    {
        _playerAim = GetComponentInParent<PlayerAim>();
        if (_playerAim == null || leftHandAimPivot == null || leftHandTarget == null)
        {
            Debug.LogError("Revolver recoil controller failed to load, remember to set LeftHandAimPivot and LeftHandTarget transforms on the WeaponRig gameobj in the Eye heirarchy"); // if this happens ill kms (jk)
            enabled = false;
            return;
        }

        restingPosition = leftHandTarget.localPosition;
        restingRotation = leftHandTarget.localRotation;
        enabled = true;
    }


    void Update()
    {
        if (!enabled) return;

        float dt = Time.deltaTime;
        
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
            if (recoilVelocity < 0f) recoilVelocity = 0f;
        }
        
        ApplyRecoil();
    }

    ///<summary>
    /// <c>Kick</c> handles IK animation stuff to make the revolver kick up in the air a bit when firing to look cool
    ///</summary>
    public bool Kick()
    {
        if (!enabled) return false;
        
        // small immediate movement so the shot feels responsive (doesn't start slow)
        recoil = Mathf.Min(recoil + positionKick, maxRecoil);
        
        // add backward velocity to the spring
        recoilVelocity = Mathf.Min(recoilVelocity + kickImpulse, maxRecoilVelocity);
        return true;
    }
    
    void ApplyRecoil()
    {
        Vector3 restingWorldPosition = leftHandAimPivot.TransformPoint(restingPosition);
        Quaternion restingWorldRotation = leftHandAimPivot.rotation * restingRotation;

        leftHandTarget.position = restingWorldPosition - leftHandAimPivot.forward * (recoil * recoilDistance);
        leftHandTarget.rotation = Quaternion.AngleAxis(-recoil * recoilAngle, leftHandAimPivot.right) * restingWorldRotation;
    }
}
