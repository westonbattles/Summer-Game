using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VFXPoolManager : MonoBehaviour
{
    public static VFXPoolManager Instance { get; private set; }
    
    [Header("Budget")]
    [SerializeField, Min(1)] int globalMaxActive = 128;

    [SerializeField]
    [Tooltip("VFX prefabs we want to pre-load so we don't cause lag if we have to spawn a bunch of them and the pool is empty (only happens once after the game starts, but could still be a bitch to deal with)")]
    PooledVfx[] startingVFXPrefabs;
    
    Dictionary<PooledVfx, ObjectPool<PooledVfx>> _pools = new();
    List<PooledVfx> _active = new();
    
    public int DroppedEffects { get; private set; } // skipped effects due pool concurrency limits being reached
    
    void Awake()
    {
        // don't keep multiple instances
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // pre-load the starting vfxs
        foreach (PooledVfx prefab in startingVFXPrefabs)
        {
            if (prefab != null) Preload(prefab);
        }
    }

    // late update so physics can run first and its garunteed to run after
    void LateUpdate()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            PooledVfx vfx = _active[i];
            if (vfx == null)
            {
                Debug.LogError("A pooled VFX was destroyed while active instead of being released first");
                
                // don't remove from pool, just list of active vfx
                SwapToBackAndRemove(i);
                continue;
            }
            
            if (vfx.ReturnsAutomatically && vfx.IsFinished) ReleaseAt(i);
        }
    }

    // plays the given pooledvfx prefab, (automatically adds it to our pool if not added)
    public PooledVfx Play(PooledVfx prefab, Vector3 position, Quaternion rotation)
    {
        // try get pool from prefab
        if (!CanPlay(prefab, out ObjectPool<PooledVfx> pool)) return null;

        PooledVfx vfx = pool.Get();
        _active.Add(vfx);
        vfx.PlayWorld(position, rotation);

        return vfx;
    }

    // play vfx but have it follow a parent while playing
    public PooledVfx PlayAttatched(PooledVfx prefab, Transform parent)
    {
        // try get pool from prefab
        if (!CanPlay(prefab, out ObjectPool<PooledVfx> pool)) return null;

        PooledVfx vfx = pool.Get();
        _active.Add(vfx);
        vfx.PlayAttached(parent);

        return vfx;
    }
    
    public void Release(PooledVfx vfx)
    {
        if (vfx == null) return;

        int index = _active.IndexOf(vfx); // -1 if not in range
        if (index >= 0) ReleaseAt(index);
    }

    bool CanPlay(PooledVfx prefab, out ObjectPool<PooledVfx> pool)
    {
        pool = null;
        
        if (prefab == null) return false;
        pool = GetOrCreatePool(prefab);

        // make sure our limits are being respected
        if (_active.Count >= globalMaxActive || pool.CountActive >= prefab.MaxConcurrentInstances)
        {
            DroppedEffects += 1;
            return false;
        }

        return true;
    }

    private ObjectPool<PooledVfx> GetOrCreatePool(PooledVfx prefab)
    {
        if (_pools.TryGetValue(prefab, out ObjectPool<PooledVfx> outPool)) return outPool;
        
        // create pool
        ObjectPool<PooledVfx> pool = null;
        pool = new ObjectPool<PooledVfx>(
            () =>
            {
                PooledVfx instance = Instantiate(prefab, transform);
                instance.Initialize(pool.Release); // gets invoked by our vfx object, pool release null checking is handled in pooledvfx
                instance.ResetForPool(transform);  // vfx transforms get initialized to the pool manager transform
                return instance;
            },
            null,
            (PooledVfx vfx) => vfx.ResetForPool(transform),     // pool.Release() will call this before returning it to the pool
            (PooledVfx vfx) => Destroy(vfx.gameObject),
            true,                                                // collection checks only happen in the editor
            Mathf.Max(0, prefab.StartingCount),                  // starting stack size for the pool
            prefab.MaxInactiveInstances                               // max stack size for the pool
        );
        
        // add to dict of pools on creation
        _pools.Add(prefab, pool);
        return pool;
    }

    void Preload(PooledVfx prefab)
    {
        ObjectPool<PooledVfx> pool = GetOrCreatePool(prefab);
        PooledVfx[] instances = new PooledVfx[prefab.StartingCount];
        
        // create and then release StartingCount instances so they get stored in our pool
        for (int i = 0; i < prefab.StartingCount; i++) instances[i] = pool.Get();
        for (int i = 0; i < prefab.StartingCount; i++) pool.Release(instances[i]);
    }

    /// returns vfx at index <c>index</c> to the pool, resetting its scale/transform and everything first
    void ReleaseAt(int index)
    {
        PooledVfx vfx = _active[index];
        SwapToBackAndRemove(index);
        vfx.Return();                   // returns our vfx to the pool
    }

    /// swaps the vfx to the end of our active list before removing it, so they stay close together in memory.
    /// doesn't remove it from pool, list order is not perserved
    void SwapToBackAndRemove(int index)
    {
        int last = _active.Count - 1;
        _active[index] = _active[last];
        _active.RemoveAt(last);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    [ShowIf(nameof(showDebugButtons))]
    [SerializeField] private PooledVfx debugVfxPrefab;

    [ShowNativeProperty] private int DebugActiveCount => _active.Count;
    [ShowNativeProperty] private int DebugPoolCount => _pools.Count;
    [ShowNativeProperty] private int DebugDroppedEffects => DroppedEffects;

    [ShowIf(nameof(showDebugButtons))]
    [Button("Play Debug VFX", EButtonEnableMode.Playmode)]
    private void TestPlay()
    {
        if (debugVfxPrefab == null)
        {
            Debug.LogWarning("[VFXPoolManager - Debug]: Assign a debug vfx prefab first.", this);
            return;
        }
        Vector3 pos = transform.position + UnityEngine.Random.insideUnitSphere * 3f;
        Play(debugVfxPrefab, pos, Quaternion.identity);
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Flood to Global Max", EButtonEnableMode.Playmode)]
    private void TestFlood()
    {
        if (debugVfxPrefab == null) return;
        int before = DroppedEffects;
        for (int i = 0; i < globalMaxActive + 5; i++)
        {
            Vector3 pos = transform.position + UnityEngine.Random.insideUnitSphere * 5f;
            Play(debugVfxPrefab, pos, Quaternion.identity);
        }
        Debug.Log($"[VFXPoolManager - Debug]: Flood done. Active: {_active.Count}, dropped this flood: {DroppedEffects - before}", this);
    }

    [Button("Release All", EButtonEnableMode.Playmode)]
    private void ReleaseAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i] == null)
            {
                SwapToBackAndRemove(i);
                continue;
            }
            ReleaseAt(i);
        }
    }
    #endregion
#endif

}