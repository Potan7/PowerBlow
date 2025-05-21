using UnityEngine;
using Player;

namespace Player.State
{
    public abstract class PlayerStateEntity
    {
        protected PlayerController _player;
        public PlayerStateEntity(PlayerController player)
        {
            _player = player;
        }
        
        public abstract void Enter();
        public abstract void Execute();
        public abstract void Exit();
    }
}