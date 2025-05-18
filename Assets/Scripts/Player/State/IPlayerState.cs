using UnityEngine;

namespace Player.State
{
    public interface IPlayerState
    {
        public void Enter(PlayerController playerController);
        public void Execute();
        public void Exit();
    }
}