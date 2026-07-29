using UnityEngine;

public class HealthSmokeTest : MonoBehaviour
{
    private void Start()
    {
        var h = new GameObject("dummy").AddComponent<Health>();

        h.TakeDamage(new DamageInfo { Amount = 30f });
        Debug.Assert(h.CurrentHP == 70f, "damage should reduce HP");

        h.TakeDamage(new DamageInfo { Amount = -5f });
        Debug.Assert(h.CurrentHP == 70f, "negative damage should be ignored");

        h.Heal(999f);
        Debug.Assert(h.CurrentHP == h.MaxHP, "heal should clamp to max");

        int deaths = 0;
        h.OnDeath += _ => deaths++;
        h.TakeDamage(new DamageInfo { Amount = 9999f });
        h.TakeDamage(new DamageInfo { Amount = 10f });
        Debug.Assert(deaths == 1, "death should fire exactly once");

        h.OnSpawn();
        Debug.Assert(h.IsAlive && h.CurrentHP == h.MaxHP, "respawn should revive at full");

        Debug.Log("Health smoke test done");
    }
}