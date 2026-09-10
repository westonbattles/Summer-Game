using System;
using KinematicCharacterController;
using UnityEngine;

[Serializable]
public class RampSlideSettings
{
    public float minimumApproachSpeed = 12f; // you dont get ramp slide if you go slower than this
    public float minimumRampAngle = 10f; // min angle that defines a ramp
    public float maximumApproachAngle = 60f; // max angle (to the slope normal) you can hit it while getting rampslide functionality
    [Min(0f)] public float airborneUpSpeed = 5f;
    [Min(0f)] public float minimumLaunchSpeed = 32f; // ramp boosts take in your incoming speed but if it is less than this, it will set it to this so you get a speed boost if you go in a bit slower
    [Min(0f)] public float maximumSpeedGain = 8f; // max speed you can gain from a boost is incoming speed + maximum speed gain
}

public struct MovementParameters
{
    public float GroundAcceleration, GroundSpeed, AirAcceleration, AirSpeed;
    public float JumpSpeed, Gravity, Friction, StopSpeed, BhopCarry;
}

public class PlayerMovement
{
    enum DashMotion { None, Surface, Airborne }
    enum PendingImpulse { None, Dash, GroundPound } // so we dont need seperate _shouldDash variables

    readonly KinematicCharacterMotor _motor;
    readonly RampSlideSettings _ramps;
    DashMotion _dashMotion;
    PendingImpulse _pendingImpulse;
    Vector3 _requestedVelocity;
    float _surfaceTimeRemaining, _surfaceDuration, _surfaceFriction;
    bool _poundUnground, _impulseAppliedThisStep, _rampBoostAvailable;
    bool _isDashing;

    public bool IsDashing => _isDashing || _pendingImpulse != PendingImpulse.None;
    public bool HasDashMomentum => _dashMotion != DashMotion.None;
    public bool IsSurfaceGliding => _dashMotion == DashMotion.Surface;

    public PlayerMovement(KinematicCharacterMotor motor, RampSlideSettings ramps)
    {
        _motor = motor;
        _ramps = ramps;
    }

    public void QueueDash(Vector3 requestedVelocity, float surfaceDuration, float surfaceFriction)
    {
        _requestedVelocity = requestedVelocity;
        _surfaceDuration = Mathf.Max(0f, surfaceDuration);
        _surfaceFriction = Mathf.Max(0f, surfaceFriction);
        _pendingImpulse = PendingImpulse.Dash;
    }

    public void QueueGroundPound(Vector3 velocity, bool forceUnground)
    {
        _requestedVelocity = velocity;
        _poundUnground = forceUnground;
        _pendingImpulse = PendingImpulse.GroundPound;
    }

    public void BeforeCharacterUpdate()
    {
        _impulseAppliedThisStep = false;
        if (_pendingImpulse == PendingImpulse.None) return;
        _impulseAppliedThisStep = true;

        if (_pendingImpulse == PendingImpulse.GroundPound)
        {
            EndDashMomentum();
            _motor.BaseVelocity = _requestedVelocity;
            if (_poundUnground) _motor.ForceUnground(0.25f);
        }
        else
        {
            Vector3 direction = _requestedVelocity.normalized;
            bool alongGround = _motor.GroundingStatus.IsStableOnGround && !_motor.MustUnground() && Vector3.Dot(direction, _motor.CharacterUp) <= 0.15f;

            if (alongGround) direction = _motor.GetDirectionTangentToSurface(direction, _motor.GroundingStatus.GroundNormal);

            // the request's magnitude includes vertical dash damping also preserve existing perpendicular momentum
            _motor.BaseVelocity = Accelerate(_motor.BaseVelocity, direction, _requestedVelocity.magnitude, float.PositiveInfinity);
            _dashMotion = alongGround ? DashMotion.Surface : DashMotion.Airborne;
            _surfaceTimeRemaining = alongGround ? _surfaceDuration : 0f;
            _rampBoostAvailable = true;

            // skip the initial ground probe for an aerial launch
            if (!alongGround) _motor.ForceUnground(0f);
        }

        _isDashing = true;
        _pendingImpulse = PendingImpulse.None;
    }

    public void PostGroundingUpdate(bool jumpRequested)
    {
        if (!_motor.GroundingStatus.IsStableOnGround)
        {
            if (IsSurfaceGliding) _dashMotion = DashMotion.Airborne;
            _surfaceTimeRemaining = 0f;
        }
        else if (!_motor.LastGroundingStatus.IsStableOnGround || _dashMotion == DashMotion.Airborne)
        {
            _isDashing = false;
            // a buffered jump can carry earned momentum
            if (!jumpRequested) EndDashMomentum();
        }
        else if (IsSurfaceGliding && _surfaceTimeRemaining <= 0f)
        {
            EndDashMomentum();
        }
    }

    public bool UpdateVelocity(ref Vector3 velocity, Vector3 input, bool jumpRequested,
        MovementParameters settings, float deltaTime)
    {
        float inputAmount = Mathf.Clamp01(input.magnitude);
        Vector3 wishDirection = input.normalized;

        if (_motor.GroundingStatus.IsStableOnGround && !_motor.MustUnground())
        {
            Vector3 normal = _motor.GroundingStatus.GroundNormal;
            velocity = _motor.GetDirectionTangentToSurface(velocity, normal) * velocity.magnitude;

            if (jumpRequested)
            {
                Vector3 planar = Vector3.ProjectOnPlane(velocity, _motor.CharacterUp);
                float speed = planar.magnitude;
                float carry = HasDashMomentum ? 1f : settings.BhopCarry;
                if (speed > settings.GroundSpeed) planar *= (settings.GroundSpeed + (speed - settings.GroundSpeed) * carry) / speed;

                velocity = planar + _motor.CharacterUp * settings.JumpSpeed;
                _motor.ForceUnground(0f);
                if (HasDashMomentum) _dashMotion = DashMotion.Airborne;
                _surfaceTimeRemaining = 0f;
                ApplyAir(ref velocity, wishDirection, inputAmount, settings, deltaTime, false);
                return true;
            }

            Vector3 groundWish = _motor.GetDirectionTangentToSurface(wishDirection, normal);
            if (IsSurfaceGliding && inputAmount > 0f && Vector3.Dot(groundWish, velocity.normalized) < -0.25f) EndDashMomentum();

            float friction = settings.Friction;
            if (IsSurfaceGliding)
            {
                float fadeTime = Mathf.Min(0.08f, _surfaceDuration);
                float fade = fadeTime > 0f ? 1f - Mathf.Clamp01(_surfaceTimeRemaining / fadeTime) : 1f;
                friction = Mathf.Lerp(_surfaceFriction, friction, fade);
            }

            if (!_impulseAppliedThisStep) velocity = ApplyFriction(velocity, friction, settings.StopSpeed, deltaTime);
            velocity = Accelerate(velocity, groundWish, settings.GroundSpeed * inputAmount, settings.GroundAcceleration * inputAmount * deltaTime);
        }
        else
        {
            ApplyAir(ref velocity, wishDirection, inputAmount, settings, deltaTime, !_impulseAppliedThisStep);
        }

        return false;
    }

    public void AfterCharacterUpdate(float deltaTime, float groundSpeed)
    {
        if (!IsSurfaceGliding) return;
        _surfaceTimeRemaining = Mathf.Max(0f, _surfaceTimeRemaining - deltaTime);
        if (_motor.BaseVelocity.magnitude <= groundSpeed) _surfaceTimeRemaining = 0f;
    }

    public void ProcessHitStability(Vector3 normal, ref HitStabilityReport report)
    {
        if (!HasDashMomentum) return;

        bool keepAirborne = Vector3.Dot(_motor.BaseVelocity, _motor.CharacterUp) > _ramps.airborneUpSpeed || TryGetRampVelocity(_motor.BaseVelocity, normal, out _);

        if (keepAirborne)
        {
            report.IsStable = false;
            report.ValidStepDetected = false;
            // also avoid KCC flattening the obstruction normal when transitioning from grounded movement
            _motor.ForceUnground(0f);
        }
        else if (_dashMotion == DashMotion.Airborne && report.ValidStepDetected)
        {
            report.ValidStepDetected = false;
            report.IsStable = false;
        }
    }

    /// called within KCC's projection, so the boost affects the remaining sweep as well as next tick
    public bool ProjectVelocity(ref Vector3 velocity, Vector3 normal)
    {
        if (!HasDashMomentum) return false;

        if (TryGetRampVelocity(velocity, normal, out Vector3 clipped))
        {
            if (_rampBoostAvailable)
            {
                float launchFloor = Mathf.Min(_ramps.minimumLaunchSpeed, velocity.magnitude + _ramps.maximumSpeedGain);
                float speed = clipped.magnitude;
                if (speed > 0f && speed < launchFloor) clipped *= launchFloor / speed;
                _rampBoostAvailable = false;
            }

            velocity = clipped;
            _dashMotion = DashMotion.Airborne;
            _surfaceTimeRemaining = 0f;
            _motor.ForceUnground(0f);
            return true;
        }

        float slope = Vector3.Angle(_motor.CharacterUp, normal);
        bool rejectedGroundContact = Vector3.Dot(velocity, normal) < 0f
            && slope <= _motor.MaxStableSlopeAngle
            && (_dashMotion == DashMotion.Airborne || slope >= _ramps.minimumRampAngle);

        if (rejectedGroundContact)
        {
            // KCC's default landing response discards downward velocity before turning uphill.
            // clip the actual impact instea  and prevent a later tangent contact awarding the bonus
            velocity = clipped;
            _rampBoostAvailable = false;
            _dashMotion = DashMotion.Airborne;
            _surfaceTimeRemaining = 0f;
            return true;
        }

        if (Vector3.Dot(velocity, _motor.CharacterUp) > _ramps.airborneUpSpeed)
        {
            velocity = ClipVelocity(velocity, normal);
            return true;
        }

        return false;
    }

    bool TryGetRampVelocity(Vector3 velocity, Vector3 normal, out Vector3 clipped)
    {
        clipped = ClipVelocity(velocity, normal);
        float slope = Vector3.Angle(_motor.CharacterUp, normal);
        float intoSurface = -Vector3.Dot(velocity.normalized, normal);
        float maximumIntoSurface = Mathf.Sin(_ramps.maximumApproachAngle * Mathf.Deg2Rad);
        return slope >= _ramps.minimumRampAngle && slope <= _motor.MaxStableSlopeAngle //ugly but its fine
            && Vector3.Dot(velocity, normal) <= 0.01f
            && intoSurface <= maximumIntoSurface + 0.00001f
            && Vector3.ProjectOnPlane(velocity, _motor.CharacterUp).magnitude >= _ramps.minimumApproachSpeed
            && Vector3.Dot(clipped, _motor.CharacterUp) > _ramps.airborneUpSpeed;
    }

    void EndDashMomentum()
    {
        _dashMotion = DashMotion.None;
        _surfaceTimeRemaining = 0f;
        _rampBoostAvailable = false;
        _isDashing = false;
    }

    void ApplyAir(ref Vector3 velocity, Vector3 direction, float inputAmount,
        MovementParameters settings, float deltaTime, bool applyGravity)
    {
        velocity = Accelerate(velocity, direction, settings.AirSpeed * inputAmount, settings.AirAcceleration * inputAmount * deltaTime);
        // keep the existing full launch velocity on the tick a jump/dash is applied
        if (applyGravity) velocity -= _motor.CharacterUp * (settings.Gravity * deltaTime);
    }

    public static Vector3 ApplyFriction(Vector3 velocity, float friction, float stopSpeed, float deltaTime)
    {
        float speed = velocity.magnitude;
        if (speed <= 0f) return Vector3.zero;
        float drop = Mathf.Max(speed, stopSpeed) * friction * deltaTime;
        return velocity * (Mathf.Max(speed - drop, 0f) / speed);
    }

    public static Vector3 Accelerate(Vector3 velocity, Vector3 direction, float wishSpeed, float accelerationStep)
    {
        float available = wishSpeed - Vector3.Dot(velocity, direction);
        if (available <= 0f || direction.sqrMagnitude <= 0f) return velocity;
        return velocity + direction * Mathf.Min(available, accelerationStep);
    }

    public static Vector3 ClipVelocity(Vector3 velocity, Vector3 normal)
    {
        return velocity - normal * Mathf.Min(0f, Vector3.Dot(velocity, normal));
    }
}
