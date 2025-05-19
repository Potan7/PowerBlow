using UnityEngine;

namespace Player.State
{
    public interface IPlayerState
    {
        public void Enter();
        public void Execute();
        public void Exit();
    }
}