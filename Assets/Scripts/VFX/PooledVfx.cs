using System;
using UnityEngine;

public enum VfxReleaseType
{
    WhenFinished,
    Manual
}

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class PooledVfx : MonoBehaviour
{
    [SerializeField] VfxReleaseType releaseType = VfxReleaseType.WhenFinished; // particles default to get released when theyre done showing their fx
    
    [Header("Pool Limits")]
    [SerializeField, Min(0)] int startingCount = 8;           // default load this number of the particles into the game so we can access them instantly when we start shooting
    [SerializeField, Min(1)] int maxInactiveInstances = 24;   // max number of inactive vfx objs allowed in the pool @ once
    [SerializeField, Min(1)] int maxConcurrentInstances = 32; // max number of active vfx allowed to play concurrently

    ParticleSystem _root;
    Action<PooledVfx> _returnToPool;
    Vector3 _prefabScale; // prob don't need this
    bool _isRented;       // being rented from the pool

    public int StartingCount => Mathf.Min(startingCount, maxInactiveInstances); // don't initialize pool with more than it can hold
    public int MaxInactiveInstances => maxInactiveInstances;
    public int MaxConcurrentInstances => maxConcurrentInstances;

    public bool ReturnsAutomatically => releaseType == VfxReleaseType.WhenFinished;

    public bool IsFinished => !_root.IsAlive(true);

    // Action<PooledVfx>: https://learn.microsoft.com/en-us/dotnet/api/system.action-1?view=net-10.0
    public void Initialize(Action<PooledVfx> returnToPool)
    {
        _root = GetComponent<ParticleSystem>();
        _returnToPool = returnToPool ?? throw new ArgumentNullException(nameof(returnToPool)); // ensure its not null
        _prefabScale = transform.localScale;

        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            
            // maybe we can stop the loop? or just don't ever use looping particles lolll
            if (ReturnsAutomatically && main.loop)
            {
                throw new InvalidOperationException($"Can't stop VFX '{name}' automatically as it contains a looping ParticleSystem");
            }
        }
    }

    public void PlayWorld(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        _isRented = true;
        gameObject.SetActive(true);
        _root.Play(true);
    }

    public void PlayAttached(Transform parent)
    {
        transform.SetParent(parent, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        _isRented = true;
        gameObject.SetActive(true);
        _root.Play(true);
    }

    /// returns the vfx to the pool
    internal void Return()
    {
        if (!_isRented) return; //probably shouldn't happen

        _isRented = false;
        _returnToPool(this);
    }
    
    /// disables the vfx, and resets its transform + scale and things (but doesn't return it to the pool)
    internal void ResetForPool(Transform poolRoot)
    {
        _root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        transform.SetParent(poolRoot, false);
        transform.localScale = _prefabScale; // reset scale if effect changed it
        
        _isRented = false;
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = gameObject.AddComponent<ParticleSystem>();
        }

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        if (TryGetComponent<ParticleSystemRenderer>(out var r))
            r.enabled = false;
    }
#endif
}