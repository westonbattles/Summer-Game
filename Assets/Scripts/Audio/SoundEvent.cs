using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Authorable sound asset: clips, volume, pitch variation, loop flag, mixer routing.
/// Attack code holds a SoundEvent reference and calls Play/PlayAttached; all tuning
/// lives here in the asset, not on prefabs or in code.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Sound Event")]
public class SoundEvent : ScriptableObject
{
    [Header("Clips")]
    [SerializeField] AudioClip[] clips;
    [Tooltip("Prevents picking the exact same clip back-to-back if multiple clips are assigned.")]
    [SerializeField] bool preventBackToBackRepeat = true;
    [System.NonSerialized] int lastClipIndex = -1;      // stores the last picked clip index at runtime.

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] Vector2 pitchRange = new(0.95f, 1.05f);
    [SerializeField] bool loop = false;
    [SerializeField] AudioMixerGroup mixerGroup;

    [Header("Spatial Properties")]
    [Tooltip("0 = 2D (UI sounds), 1 = fully 3D positional.")]
    [SerializeField, Range(0f, 1f)] float spatialBlend = 1f;
    [Tooltip("0 = No pitch shift from relative motion (XP). 1 = Real-world physics pitch shift (Vehicles).")]
    [SerializeField, Range(0f, 5f)] float dopplerLevel = 0f;

    [Header("Burst")]
    [SerializeField, Min(1)] int burstCount = 1;
    [SerializeField, Min(0f)] float staggerDelay = 0.06f;

    [Header("Throttling")]
    [SerializeField] float minInterval = 0.05f;
    float lastPlayedTime = float.NegativeInfinity;   

    public bool Loop => loop;

    /// <summary>Play at a fixed world position. For one-shots you can ignore the returned handle.</summary>
    public SoundHandle Play(Vector3 position) => PlayInternal(position, null);

    /// <summary>Play following a transform (charge hums, engine loops, anything on a moving thing).</summary>
    public SoundHandle PlayAttached(Transform follow) => PlayInternal(follow.position, follow);

    private SoundHandle PlayInternal(Vector3 position, Transform follow)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"SoundEvent '{name}' has no clips assigned.", this);
            return SoundHandle.Invalid;
        }


        // prevents massive amounts of sounds (e.g. aoe explosion) from sending you to an audiologist.
        if (lastPlayedTime > Time.unscaledTime) lastPlayedTime = float.NegativeInfinity;

        if (Time.unscaledTime - lastPlayedTime < minInterval)
        {
            return SoundHandle.Invalid;
        }
        lastPlayedTime = Time.unscaledTime;

        if (burstCount > 1 && !loop)
        {
            return AudioPlayer.Instance.PlayBurst(this, position, follow, volume,
                                              pitchRange, burstCount, staggerDelay,
                                              spatialBlend, dopplerLevel, mixerGroup);
        }

        AudioClip clip = GetRandomClip();
        float pitch = Random.Range(pitchRange.x, pitchRange.y);
        return AudioPlayer.Instance.Play(clip, position, follow, volume, pitch, loop, spatialBlend, dopplerLevel, mixerGroup);
    }

    /// <summary>
    /// Selects a random clip index, guaranteed not to repeat the previous clip if possible.
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length <= 0) return null;
        if (clips.Length == 1) return clips[0];

        int index;
        if (preventBackToBackRepeat && lastClipIndex >= 0)
        {
            index = Random.Range(0, clips.Length - 1);
            if (index >= lastClipIndex) index++;
        }
        else
        {
            index = Random.Range(0, clips.Length);
            
        }

        lastClipIndex = index;
        return clips[index];
    }
}
