using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// THROWAWAY test script. Put it on the same GameObject as a Health component.
/// Press keys in Play mode to poke the damage system and watch the Console.
/// Delete this once you trust the system.
/// </summary>
[RequireComponent(typeof(Health))]
public class HealthTester : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        // Subscribe so we can see the events actually fire.
        health.OnHealthChanged += LogHealthChanged;
        health.OnDeath += LogDeath;
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= LogHealthChanged;
        health.OnDeath -= LogDeath;
    }

    private void Update()
    {
        // Get a reference to the keyboard device
        var keyboard = Keyboard.current;
        if (keyboard == null) return; // No keyboard connected

        // [1] Normal hit
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            Hit(30f, "normal hit");
        }

        // [2] Crit hit
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            health.TakeDamage(new DamageInfo(45f, gameObject, isCrit: true));
            Debug.Log("[Tester] dealt 45 CRIT");
        }

        // [3] Heal
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            health.Heal(20f);
            Debug.Log("[Tester] healed 20");
        }

        // [4] DOUBLE overkill
        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("[Tester] firing TWO lethal hits this frame...");
            Hit(9999f, "lethal A");
            Hit(9999f, "lethal B");
        }

        // [5] Affix-style max HP bump
        if (keyboard.digit5Key.wasPressedThisFrame)
        {
            health.SetMaxHP(health.MaxHP * 2.5f);
            Debug.Log("[Tester] elite-ified: MaxHP x2.5 and refilled");
        }
    }

    private void Hit(float amount, string label)
    {
        health.TakeDamage(new DamageInfo(amount, gameObject));
        Debug.Log($"[Tester] dealt {amount} ({label})");
    }

    private void LogHealthChanged(float current, float max)
    {
        Debug.Log($"[Health] {current:0.#} / {max:0.#}");
    }

    private void LogDeath(DamageInfo killingBlow)
    {
        string crit = killingBlow.IsCrit ? " (CRIT)" : "";
        Debug.Log($"[Health] DIED — killing blow {killingBlow.Amount:0.#}{crit}");
    }
}