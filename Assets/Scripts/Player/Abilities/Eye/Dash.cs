using System;
using UnityEngine;

[Serializable]
public class Dash : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] public float dashSpeed = 17f;
    [SerializeField] public float dashCooldownTime = 2.3f;

    class DashState : AbilityState
    {
        public Player Player;
        public PlayerAim PlayerAim;

        public bool IsDashing => Player.IsDashing;
        public float CooldownRemaining;         // how much time left on the shot cooldown

        public DashState(Player player)
        {
            Player = player;
            PlayerAim = player.Aim;
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

        dash.Player.Dash(dash.PlayerAim.AimDirection, dashSpeed);

        dash.CooldownRemaining = dashCooldownTime; // update cooldown
        return true;
    }

    // no cancel since carrying momentum after character swap is kinda cool
    /*public override void Cancel(AbilityState state)
    {
    }*/
}