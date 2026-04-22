using UnityEngine;

public sealed class PlayerDeadState : IPlayerState
{
    public void Enter(Player player)
    {
        player.SetInputEnabled(false);
        player.SetSpeedMultiplier(1f);
        player.StopMovement();
        player.StopCriticalBlink();
        player.StartDeathSequence();
        Debug.LogWarning("Player entered Dead State");
    }

    public void Tick(Player player)
    {
    }

    public void Exit(Player player)
    {
    }
}
