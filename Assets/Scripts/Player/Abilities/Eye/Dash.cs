using System;
using UnityEngine;

[Serializable]
public class Dash : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] float dashSpeed = 25;
    [SerializeField] float dashCooldownTime = 2.3f;
    [SerializeField] float dashUseTime = 0f;
    [SerializeField] float dashUseCooldown = 0.2f;
    [SerializeField] float dashLandMomentumDecayTime = 0.6f; // time to apply updated friction for
    [SerializeField] float dashVerticalDampPercent = 0.7f; // good feeling horizontal dashes lead to too strong vertical dashes, this value squishes down the vertical component for vert dashes to account for that
    [SerializeField] GameObject dashVfxPrefab;

    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float DashSpeed => dashSpeed;
    public float DashCooldownTime => dashCooldownTime * Player.Instance.Modifiers.CooldownDurationMultiplier.GetValue();
    public float DashLandMomentumDecayTime => dashLandMomentumDecayTime;
    public float VerticalDashDampPercent => dashVerticalDampPercent * Player.Instance.Modifiers.DashVerticalMultiplier.GetValue();
    
    class DashState : AbilityState
    {
        public Player Player;

        public bool IsDashing => Player.IsDashing;
        public float CooldownRemaining;         // how much time left on the shot cooldown
        public GameObject ActiveVfx;

        public DashState(Player player)
        {
            Player = player;
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        DashState state = new DashState(player);
        return state;
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        DashState dash = state as DashState;
        if (dash == null) return; // make sure state variable is of type revolver state

        dash.CooldownRemaining = Mathf.Max(0f, dash.CooldownRemaining- deltaTime);
        dashUseTime = Mathf.Max(0f, dashUseTime - deltaTime);

        if (dash.ActiveVfx != null)
        {
            if (!dash.Player.IsDashing)
            {
                dash.ActiveVfx.SetActive(false);
                UnityEngine.Object.Destroy(dash.ActiveVfx);
                dash.ActiveVfx = null;
            }
        }
    }

    public override bool TryExecute(AbilityState state)
    {
        DashState dash = state as DashState;
        if (dash == null) return false;                 // cant execute rip
        if (dashUseTime > 0f) return false; 
        if (dash.CooldownRemaining > 0.1f) // cooldown eval (for QS bullet)
        {
            Debug.Log($"{dash.CooldownRemaining} seconds left of cooldown");
            Player Player = dash.Player;
            if (Player.Modifiers.CooldownDamageReduction.GetValue() != 0f)
            {
                Debug.Log("Quicksilver bullet used");
                // NOTE: This is on all abilities currently, refactoring for a broadcast done in the item runtime would be better but idc about that rn
                float playerHealth = Player.GetComponent<PlayerHealth>().MaxHP;
                DamageInfo selfDamage = new DamageInfo(playerHealth * dash.CooldownRemaining * .01f * Player.Modifiers.CooldownDamageReduction.GetValue(), DamageType.Physical);
                Player.GetComponent<PlayerHealth>().TakeDamage(selfDamage);
            }
            else
            {
                Debug.Log("No Quicksilver Bullet");
                return false;
            }
        }

        if (dash.Player.Input.Move.y > 0 || dash.Player.Input.Move == Vector2.zero) dash.Player.DashVertically(DashSpeed);
        else dash.Player.DashAlongInputAxis(DashSpeed); //<- base dash

        if (dash.ActiveVfx != null)
            UnityEngine.Object.Destroy(dash.ActiveVfx);

        if (dashVfxPrefab != null)
        {
            dash.ActiveVfx = UnityEngine.Object.Instantiate(dashVfxPrefab, dash.Player.transform.position, dash.Player.transform.rotation);
            dash.ActiveVfx.transform.SetParent(dash.Player.transform);
        }

        dash.CooldownRemaining = DashCooldownTime; // update cooldown
        dashUseTime = dashUseCooldown;
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        if (state is not DashState d) return 0f;
        if (DashCooldownTime <= 0f) return 0f;
        return Mathf.Clamp01(d.CooldownRemaining / DashCooldownTime);
    }
    // no cancel since carrying momentum after character swap is kinda cool
    /*public override void Cancel(AbilityState state)
    {
    }*/
}