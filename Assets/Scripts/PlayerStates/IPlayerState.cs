using UnityEngine;

public interface IPlayerState
{
    void Enter(Player player);
    void Tick(Player player);
    void Exit(Player player);
}
