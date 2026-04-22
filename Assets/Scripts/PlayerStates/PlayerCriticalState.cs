using UnityEngine;

public sealed class PlayerCriticalState : IPlayerState
{
    public void Enter(Player player)
    {
        player.SetSpeedMultiplier(player.CriticalSpeedMultiplier);
        player.SetInputEnabled(true);
        player.StartCriticalBlink();
        Debug.Log("Player entered Critical State");
    }

    public void Tick(Player player)
    {
        if (player.IsDead)
            player.SetState(player.DeadState);
        else if (!player.IsCriticalHealth)
            player.SetState(player.HealthyState);
    }

    public void Exit(Player player)
    {
        player.StopCriticalBlink();
    }
}
