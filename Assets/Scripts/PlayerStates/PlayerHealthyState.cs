using UnityEngine;

public sealed class PlayerHealthyState : IPlayerState
{
    public void Enter(Player player)
    {
        player.SetSpeedMultiplier(1f);
        player.SetInputEnabled(true);
        player.StopCriticalBlink();
    }

    public void Tick(Player player)
    {
        if (player.IsDead)
            player.SetState(player.DeadState);
        else if (player.IsCriticalHealth)
            player.SetState(player.CriticalState);
    }

    public void Exit(Player player)
    {
    }
}
