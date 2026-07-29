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
    [Tooltip("Runtime burst overrides are clamped to this. Keeps mass pickups from flooding voices.")]
    [SerializeField, Min(1)] int maxBurstCount = 6;

    [Header("Throttling")]
    [SerializeField] float minInterval = 0.05f;
    float lastPlayedTime = float.NegativeInfinity;

    [Header("Distance Attenuation")]
    [Tooltip("Full volume inside this radius. Ignored when rolloff is Custom.")]
    [SerializeField, Min(0.01f)] float minDistance = 1f;
    [Tooltip("Logarithmic: attenuation stops here (sound does NOT reach zero). Linear: volume hits zero here. Custom: curve x=1 maps to this distance.")]
    [SerializeField, Min(0.02f)] float maxDistance = 50f;
    [SerializeField] AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    [Tooltip("Custom mode only. X: 0 = at source, 1 = maxDistance. Y: volume multiplier.")]
    [SerializeField] AnimationCurve customRolloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public bool Loop => loop;

    private void OnValidate()
    {
        if (maxDistance <= minDistance)
        {
            maxDistance = minDistance + 0.01f;
        }
        if (pitchRange.x > pitchRange.y)
        {
            pitchRange.y = pitchRange.x;
        }
        burstCount = Mathf.Min(burstCount, maxBurstCount);
    }

    /// <summary>Play with a runtime burst count (e.g. "collected N orbs"), overriding the authored burstCount.</summary>
    public SoundHandle Play(Vector3 position, int burstOverride) => PlayInternal(position, null, burstOverride);

    public SoundHandle Play(Vector3 position) => PlayInternal(position, null, -1);

    /// <summary>Play following a transform (charge hums, engine loops, anything on a moving thing).</summary>
    public SoundHandle PlayAttached(Transform follow) => PlayInternal(follow.position, follow, -1);

    private SoundHandle PlayInternal(Vector3 position, Transform follow, int burstOverride)
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

        int count = Mathf.Min(burstOverride > 0 ? burstOverride : burstCount, maxBurstCount);

        var settings = BuildSettings();

        if (count > 1 && !loop)
        {
            return AudioPlayer.Instance.PlayBurst(this, position, follow, pitchRange,
                                                  count, staggerDelay, settings);
        }

        AudioClip clip = GetRandomClip();
        float pitch = Random.Range(pitchRange.x, pitchRange.y);
        return AudioPlayer.Instance.Play(clip, position, follow, pitch, loop, settings);
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

    private VoiceSettings BuildSettings() =>
        new(volume, spatialBlend, dopplerLevel, mixerGroup,
            minDistance, maxDistance, rolloffMode, customRolloff);
}
