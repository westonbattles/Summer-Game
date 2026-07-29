using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

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
/// Settings for the voice that do NOT get changed after domain reloads.
/// </summary>
public readonly struct VoiceSettings
{
    public readonly float Volume;
    public readonly float SpatialBlend;
    public readonly float DopplerLevel;
    public readonly AudioMixerGroup MixerGroup;
    public readonly float MinDistance;
    public readonly float MaxDistance;
    public readonly AudioRolloffMode Rolloff;
    public readonly AnimationCurve CustomRolloff;

    public VoiceSettings(float volume, float spatialBlend, float dopplerLevel,
                         AudioMixerGroup mixerGroup, float minDistance, float maxDistance,
                         AudioRolloffMode rolloff, AnimationCurve customRolloff)
    {
        Volume = volume;
        SpatialBlend = spatialBlend;
        DopplerLevel = dopplerLevel;
        MixerGroup = mixerGroup;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
        Rolloff = rolloff;
        CustomRolloff = rolloff == AudioRolloffMode.Custom ? customRolloff : null;
    }

    /// <summary>
    /// Applies every audible property to a source. This is mostly to cleanup AudioPlayer.Play().
    /// </summary>
    /// <param name="src"></param>
    public void ApplyTo(AudioSource src)
    {
        src.volume = Volume;
        src.spatialBlend = SpatialBlend;
        src.dopplerLevel = DopplerLevel;
        src.outputAudioMixerGroup = MixerGroup;
        src.minDistance = MinDistance;
        src.maxDistance = MaxDistance;
        if (Rolloff == AudioRolloffMode.Custom && CustomRolloff != null)
        {
            // SetCustomCurve flips rolloffMode to Custom itself; setting the mode
            // AFTER this call would discard the curve. Order matters.
            src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CustomRolloff);
        }
        else
        {
            src.rolloffMode = Rolloff;
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

    /// <summary>Editor-only voice traffic logging. Flip on when hunting audio bugs.</summary>
    [SerializeField] bool logVoices = false;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogVoice(string message)
    {
        if (logVoices) Debug.Log($"[AudioPlayer] {message}", this);
    }

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
    /// <param name="pitch">Configure the pitch with your own function if you want it to be wonky.</param>
    /// <param name="loop">Whether or not the clip will loop.</param>
    /// <param name="s">VoiceSettings is baked at runtime.</param>
    /// <returns></returns>
    internal SoundHandle Play(AudioClip clip, Vector3 position, Transform follow,
                              float pitch, bool loop, in VoiceSettings s)
    {
        int index;
        if (free.Count > 0)
        {
            index = free.Pop();
        }
        else if (voices.Count < maxVoices)
        {
            index = CreateVoice();
            LogVoice($"Pool grew to {voices.Count}/{maxVoices}");
        }
        else
        {
            Debug.LogWarning($"AudioPlayer: out of voices ({maxVoices}), dropping '{clip.name}'.", this);
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
        src.pitch = pitch;
        src.loop = loop;
        s.ApplyTo(src);

        src.Play();

        LogVoice($"'{clip.name}' voice {index} v{v.Version} " +
                 $"(pitch {pitch:F2}, loop {loop}, free {free.Count}/{voices.Count})");

        return new SoundHandle(index, v.Version);
    }

    internal SoundHandle PlayBurst(SoundEvent evt, Vector3 position, Transform follow,
                               Vector2 pitchRange, int burstCount, float staggerDelay,
                               in VoiceSettings settings)
    {
        if (burstCount <= 1 || staggerDelay <= 0f)
        {
            var clip = evt.GetRandomClip();
            float pitch = Random.Range(pitchRange.x, pitchRange.y);
            return Play(clip, position, follow, pitch, false, settings);
        }

        StartCoroutine(PlayBurstRoutine(evt, position, follow, pitchRange,
                                        burstCount, staggerDelay, settings));
        return SoundHandle.Invalid;
    }

    private IEnumerator PlayBurstRoutine(SoundEvent evt, Vector3 position, Transform follow,
                                     Vector2 pitchRange, int burstCount, float staggerDelay,
                                     VoiceSettings settings)
    {
        var wait = new WaitForSeconds(staggerDelay);
        for (int i = 0; i < burstCount; i++)
        {
            if (follow != null)
            {
                position = follow.position;
            }
            else if (follow is not null)
            {
                follow = null;      // target is destroyed
            }

            var clip = evt.GetRandomClip();
            if (clip != null)
            {
                float pitch = Random.Range(pitchRange.x, pitchRange.y);
                Play(clip, position, follow, pitch, false, settings);
            }

            if (i < burstCount - 1) yield return wait;
        }
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
            if (v.FadePerSecond <= 0f) Release(h.Index);
        }
    }

    /// <summary>
    /// Removes the audioclip from the pool and releases it from the stack.
    /// </summary>
    /// <param name="index"></param>
    private void Release(int index)
    {
        var v = voices[index];
        LogVoice($"voice {index} v{v.Version} released ('{(v.Source.clip ? v.Source.clip.name : "null")}', free {free.Count + 1}/{voices.Count})");
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
            if (!v.Looping && !v.Source.isPlaying && !AudioListener.pause)
            {
                Release(i);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;      // had a case where I had domain reload off and it broke since its a singleton.
}
