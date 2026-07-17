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
    [Header("Clips (one is picked at random)")]
    [SerializeField] AudioClip[] clips;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] Vector2 pitchRange = new(0.95f, 1.05f);
    [SerializeField] bool loop = false;
    [Tooltip("0 = 2D (UI sounds), 1 = fully 3D positional.")]
    [SerializeField, Range(0f, 1f)] float spatialBlend = 1f;
    [SerializeField] AudioMixerGroup mixerGroup;

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

        var clip = clips[Random.Range(0, clips.Length)];
        float pitch = Random.Range(pitchRange.x, pitchRange.y);

        return AudioPlayer.Instance.Play(clip, position, follow, volume, pitch, loop, spatialBlend, mixerGroup);
    }
}
