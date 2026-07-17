using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// Handle to a playing sound. Version-stamped so a stale handle (whose voice was
/// reclaimed and reused for another sound) safely no-ops instead of stopping the
/// wrong sound. Safe to store in fields, safe to call Stop() on twice, safe to
/// call on the default value.
/// </summary>
public readonly struct SoundHandle
{
    internal readonly int Index;
    internal readonly int Version;

    internal SoundHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public static SoundHandle Invalid => new(-1, -1);

    public bool IsPlaying => AudioPlayer.Instance != null && AudioPlayer.Instance.IsHandleActive(this);

    /// <summary>Stop this sound. Pass a fade time for loops so they don't cut off abruptly.</summary>
    public void Stop(float fadeSeconds = 0f)
    {
        if (AudioPlayer.Instance != null)
        {
            AudioPlayer.Instance.Stop(this, fadeSeconds);
        }
    }
}

/// <summary>
/// Pooled AudioSource host. SoundEvent assets call into this; gameplay code should
/// never touch it directly except via the handles it returns.
/// One-shots are reclaimed automatically when they finish; loops live until Stop().
/// </summary>
public class AudioPlayer : MonoBehaviour
{
    public static AudioPlayer Instance { get; private set; }

    [SerializeField] int prewarmCount = 16;
    [SerializeField] int maxVoices = 64;

    private class Voice
    {
        public AudioSource Source;
        public int Version;
        public Transform Follow;
        public bool Looping;
        public float FadePerSecond; // > 0 while fading out
        public bool InUse;
    }

    private readonly List<Voice> voices = new();
    private readonly Stack<int> free = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < prewarmCount; i++)
        {
            free.Push(CreateVoice());
        }
    }

    /// <summary>
    /// Used for creating a voice in the scene.
    /// </summary>
    /// <returns></returns>
    private int CreateVoice()
    {
        var go = new GameObject($"Voice {voices.Count}");
        go.transform.SetParent(transform, false);

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;

        voices.Add(new Voice { Source = src });
        return voices.Count - 1;
    }

    /// <summary>
    /// Plays a sound. Please read parameter descriptions if you need more info.
    /// </summary>
    /// <param name="clip">The audioclip that will get played.</param>
    /// <param name="position">The location where the audioclip will play from.</param>
    /// <param name="follow">The target the audioclip will follow. E.g. enemy.</param>
    /// <param name="volume">Volume, duh.</param>
    /// <param name="pitch">Configure the pitch with your own function if you want it to be wonky.</param>
    /// <param name="loop">Whether or not the clip will loop.</param>
    /// <param name="spatialBlend">Mixer stuff.</param>
    /// <param name="group">Check the mixer for more info.</param>
    /// <returns></returns>
    internal SoundHandle Play(AudioClip clip, Vector3 position, Transform follow,
                              float volume, float pitch, bool loop,
                              float spatialBlend, AudioMixerGroup group)
    {
        int index;
        if (free.Count > 0)
        {
            index = free.Pop();
        }
        else if (voices.Count < maxVoices)
        {
            index = CreateVoice();
        }
        else
        {
            Debug.LogWarning($"AudioPlayer: out of voices, dropping sound.");
            return SoundHandle.Invalid;
        }

        var v = voices[index];
        v.InUse = true;
        v.Follow = follow;
        v.Looping = loop;
        v.FadePerSecond = 0f;

        var src = v.Source;
        src.transform.position = position;
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = loop;
        src.spatialBlend = spatialBlend;
        src.outputAudioMixerGroup = group;
        src.Play();

        return new SoundHandle(index, v.Version);
    }

    /// <summary>
    /// Checks if the handle is active. This is likely only going to be internal.
    /// </summary>
    /// <param name="h"></param>
    /// <returns></returns>
    internal bool IsHandleActive(SoundHandle h)
    {
        return h.Index >= 0
            && h.Index < voices.Count
            && voices[h.Index].InUse
            && voices[h.Index].Version == h.Version;
    }

    /// <summary>
    /// Completely cuts the sound, unless there's a fadeSeconds passed in, in which it will fadeout.
    /// Note that for cases where the object despawns, you should call Stop() in OnDespawn() on the attack (or whatever) component.
    /// </summary>
    /// <param name="h"></param>
    /// <param name="fadeSeconds"></param>
    internal void Stop(SoundHandle h, float fadeSeconds)
    {
        if (!IsHandleActive(h))
        {
            return;     // no-op
        }

        var v = voices[h.Index];
        if (fadeSeconds <= 0f)
        {
            Release(h.Index);
        }
        else
        {
            v.FadePerSecond = v.Source.volume / fadeSeconds;
        }
    }

    /// <summary>
    /// Removes the audioclip from the pool and releases it from the stack.
    /// </summary>
    /// <param name="index"></param>
    private void Release(int index)
    {
        var v = voices[index];
        v.Source.Stop();
        v.Source.clip = null;
        v.Follow = null;
        v.InUse = false;
        v.Version++;
        free.Push(index);
    }

    private void Update()
    {
        for (int i = 0; i < voices.Count; i++)
        {
            var v = voices[i];
            if (!v.InUse)
            {
                continue;
            }

            // Follow a transform if we have one. The double check handles a destroyed
            // Transform: Unity's fake-null makes `!= null` false, while `is not null`
            // sees the dead managed reference so we can clear it. I found this out while debugging it.
            if (v.Follow != null)
            {
                v.Source.transform.position = v.Follow.position;
            }
            else if (v.Follow is not null)
            {
                v.Follow = null;
            }

            // do a fade-out in progress
            if (v.FadePerSecond > 0f)
            {
                v.Source.volume -= v.FadePerSecond * Time.unscaledDeltaTime;
                if (v.Source.volume <= 0f)
                {
                    Release(i);
                    continue;
                }
            }

            // One-shots reclaim themselves when its over
            if (!v.Looping && !v.Source.isPlaying)
            {
                Release(i);
            }
        }
    }
}
