using System;
using UnityEngine;

[Serializable]
public class Dash : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] float dashSpeed = 17f;
    [SerializeField] float dashCooldownTime = 2.3f;
    [SerializeField] float dashLandMomentumDecayTime = 0.6f; // time to apply updated friction for
    [SerializeField] float verticalDashDampPercent = 0.7f; // good feeling horizontal dashes lead to too strong vertical dashes, this value squishes down the vertical component for vert dashes to account for that
    [SerializeField] GameObject dashVfxPrefab;

    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float DashSpeed => dashSpeed;
    public float DashCooldownTime => dashCooldownTime 
        - Player.Instance.Modifiers.CooldownDurationMultiplier.GetValue();
    public bool DashVerticallyEnabled => Player.Instance.Modifiers.DashVerticallyEnabled.GetValue();
    public float DashLandMomentumDecayTime => dashLandMomentumDecayTime;
    public float VerticalDashDampPercent => verticalDashDampPercent;
    
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
        if (dash.CooldownRemaining > 0f) return false;  // can't dash cuz cooldown still active

        if (DashVerticallyEnabled && (dash.Player.Input.Move.y > 0 || dash.Player.Input.Move == Vector2.zero)) 
            dash.Player.DashVertically(DashSpeed);
        else 
            dash.Player.DashAlongInputAxis(DashSpeed); //<- base dash

        if (dash.ActiveVfx != null)
            UnityEngine.Object.Destroy(dash.ActiveVfx);

        if (dashVfxPrefab != null)
        {
            dash.ActiveVfx = UnityEngine.Object.Instantiate(dashVfxPrefab, dash.Player.transform.position, dash.Player.transform.rotation);
            dash.ActiveVfx.transform.SetParent(dash.Player.transform);
        }

        dash.CooldownRemaining = DashCooldownTime; // update cooldown
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