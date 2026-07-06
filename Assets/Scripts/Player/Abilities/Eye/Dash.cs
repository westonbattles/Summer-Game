using System;
using UnityEngine;

[Serializable]
public class Dash : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] public float dashSpeed = 17f;
    [SerializeField] public float dashCooldownTime = 2.3f;
    [SerializeField] float dashLandMomentumDecayTime = 0.6f; // time to apply updated friction for
    [SerializeField] float verticalDashDampPercent = 0.7f; // good feeling horizontal dashes lead to too strong vertical dashes, this value squishes down the vertical component for vert dashes to account for that

    public float DashLandMomentumDecayTime => dashLandMomentumDecayTime;
    public float VerticalDashDampPercent => verticalDashDampPercent;
    
    class DashState : AbilityState
    {
        public Player Player;

        public bool IsDashing => Player.IsDashing;
        public float CooldownRemaining;         // how much time left on the shot cooldown

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
    }

    public override bool TryExecute(AbilityState state)
    {
        DashState dash = state as DashState;
        if (dash == null) return false;                 // cant execute rip
        if (dash.CooldownRemaining > 0f) return false;  // can't dash cuz cooldown still active

        if (dash.Player.Modifiers.DashVerticallyEnabled && (dash.Player.Input.Move.y > 0 || dash.Player.Input.Move == Vector2.zero)) dash.Player.DashVertically(dashSpeed);
        else dash.Player.DashAlongInputAxis(dashSpeed); //<- base dash
        
        dash.CooldownRemaining = dashCooldownTime; // update cooldown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        if (state is not DashState d) return 0f;
        if (dashCooldownTime <= 0f) return 0f;
        return Mathf.Clamp01(d.CooldownRemaining / dashCooldownTime);
    }
    // no cancel since carrying momentum after character swap is kinda cool
    /*public override void Cancel(AbilityState state)
    {
    }*/
}