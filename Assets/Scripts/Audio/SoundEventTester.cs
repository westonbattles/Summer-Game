using UnityEngine;
using UnityEngine.InputSystem;

public class SoundEventTester : MonoBehaviour
{
    [SerializeField] SoundEvent sound;
    SoundHandle handle;

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
            handle = sound.Play(transform.position);

        if (Keyboard.current.yKey.wasPressedThisFrame)
            handle.Stop(0.25f);   // for testing loops + fade
    }
}